using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zArm.Api.Specialized
{
	public abstract class PickPlaceMovements: ICloneable
	{
		IKinematics _ik;
		float _lastEntryDistance;

		public float ShoulderOffset { get; private set; }
		public RecordedMovement[] Movements { get; private set; }
		public bool WasGenerated { get; }

		public PickPlaceMovements(IKinematics ik, RecordedMovement target, RecordedMovement entry = null) : this(ik, new RecordedMovement[] { target, entry })
		{
		}

		public PickPlaceMovements(IKinematics ik, IEnumerable<RecordedMovement> movements)
		{
			//set fields
			_ik = ik;
			Movements = movements.Where(i => i != null).ToArray();

			//rebuild if entities aren't right
			if (Movements.Length != 5)
			{
				Rebuild();
				WasGenerated = true;
			}
			else
				ShoulderOffset = GetShoulderOffset();
		}

		void Rebuild()
		{
			//create new movements
			if (Movements.Length != MovementCount)
			{
				//get targets
				var target = Movements[0].Pose;
				Pose entryTarget = null;
				if (Movements.Length >= 2)
					entryTarget = Movements[1].Pose;
				else
				{
					//calculate secondary target by IK
					entryTarget = GetPoseAbove(target, 3); //raise by 3 cm
				}
				//set default grippers if wrong
				if (EntryIndex == OpenGripperIndex)
				{
					if (target[4] == null || target[4] >= entryTarget[4].GetValueOrDefault(40))
						target[4] = 0;
					if (entryTarget[4] == null || entryTarget[4] <= target[4])
						entryTarget[4] = 40;
				}
				else
				{
					if (target[4] == null || target[4] <= entryTarget[4].GetValueOrDefault(0))
						target[4] = 40;
					if (entryTarget[4] == null || entryTarget[4] >= target[4])
						entryTarget[4] = 0;
				}
				var description = Movements[0].Description;
				var speed = Movements[0].Speed.GetValueOrDefault(50);
				var syncronized = Movements[0].Synchronized.GetValueOrDefault();

				//create new movements
				Movements = CreateMovements();
				foreach (var entity in Movements)
				{
					entity.Type = RecordedMovementType.Move;
					entity.PickAndPlaceType = PickAndPlaceType;
				}
				Target.Pose = target;
				Entry.Pose = entryTarget;
				Movements[0].Synchronized = syncronized;
				Movements[0].Description = description;
				SetSpeed(speed);
			}

			//update movement positions
			ShoulderOffset = GetShoulderOffset();
			UpdatePoses();
		}

		Pose GetPoseAbove(Pose pose, float distance)
		{
			var ikTarget = _ik.GetTarget(pose);
			ikTarget.Result.Y += distance; //raise by distance
			var ikPose = _ik.GetPose(ikTarget.Result, pose);
			if (!ikPose.OnTarget)
				ikPose = _ik.GetPose(ikTarget.Result); //try again without the initial pose
			return ikPose.Result;
		}

		Pose GetPoseLiftUpperArm(Pose pose, float distance)
		{
			var ikTarget = _ik.GetTarget(pose);
			var baseLoc = _ik.BaseLocation;
			var length = GetDistance(baseLoc.Y, baseLoc.Z, ikTarget.Result.Y, ikTarget.Result.Z);

			//calculate rize in degrees (SSS triangle)
			var upperArmOffset = (float)(Math.Acos((Math.Pow(length, 2) + Math.Pow(length, 2) - Math.Pow(distance, 2)) / (2 * length * length)) * (180 / Math.PI));

			var newPose = pose.Copy();
			newPose[1] -= upperArmOffset;
			return newPose;
		}

		public abstract int TargetIndex { get; }

		public abstract int EntryIndex { get; }

		public abstract int OpenGripperIndex { get; }

		public abstract PickAndPlaceType PickAndPlaceType { get; }

		public abstract void SetSpeed(int speed);

		protected abstract void UpdatePoses();

		protected abstract RecordedMovement[] CreateMovements();

		protected abstract int MovementCount { get; }

		public RecordedMovement Target { get => Movements[TargetIndex]; }

		public RecordedMovement Entry { get => Movements[EntryIndex]; }

		public void SetTargetPose(Pose pose)
		{
			Target.Pose = pose;
			UpdatePoses();
		}

		public void SetEntryPose(Pose pose)
		{
			Entry.Pose = pose;
			UpdatePoses();
			_lastEntryDistance = GetDistance(Target.Pose, Entry.Pose); //only update when entry is changed
		}

		public void ResetEntryPose(bool simple)
		{
			//validate
			if (_lastEntryDistance == 0)
				_lastEntryDistance = GetDistance(Target.Pose, Entry.Pose);

			//lift directly above target
			Pose newPose = null;
			if (simple)
				newPose = GetPoseLiftUpperArm(Target.Pose, _lastEntryDistance);
			//pull back upper arm
			else
				newPose = GetPoseAbove(Target.Pose, _lastEntryDistance);

			if (newPose != null)
			{
				var entryGripper = Entry.Pose[4];
				if (EntryIndex == OpenGripperIndex)
					newPose = AddShoulderOffset(newPose);
				else
					newPose = DetractShoulderOffset(newPose);
				newPose[4] = entryGripper;
				SetEntryPose(newPose);
			}
		}

		protected Pose RemoveGripper(Pose pose)
		{
			pose[4] = null;
			return pose;
		}

		protected Pose AddShoulderOffset(Pose pose)
		{
			pose[0] -= ShoulderOffset;
			return pose;
		}

		protected Pose DetractShoulderOffset(Pose pose)
		{
			pose[0] += ShoulderOffset;
			return pose;
		}

		float GetShoulderOffset()
		{
			Pose openPose = null;
			Pose closedPose = null;
			if (TargetIndex == OpenGripperIndex)
			{
				openPose = Target.Pose;
				closedPose = Entry.Pose;
			}
			else
			{
				openPose = Entry.Pose;
				closedPose = Target.Pose;
			}

			//validate
			if (closedPose[0] == null) //if no shoulder recording on closed pose
				return 0;
			var jawAngle = openPose[4].GetValueOrDefault() - closedPose[4].GetValueOrDefault();
			if (jawAngle <= 0) //verify open gripper pose is open
				return 0;

			//calulate gripper spread
			var jawLength = 6d; //cm
			var spread = Math.Sqrt(Math.Pow(jawLength, 2) + Math.Pow(jawLength, 2) - 2 * jawLength * jawLength * Math.Cos(jawAngle / (180 / Math.PI))); //law of cosines to solve side-angle-side (SAS triangle)

			//calculate 2D distance from shoulder
			var target = _ik.GetTarget(closedPose);
			var distance = GetDistance(0, 0, target.Result.X, target.Result.Z);

			//calculate shoulder offset angle (SSS triangle)
			var halfSpread = spread / 2d;
			var shoulderOffset = Math.Acos((Math.Pow(distance, 2) + Math.Pow(distance, 2) - Math.Pow(halfSpread, 2)) / (2 * distance * distance)) * (180 / Math.PI);

			return (float)shoulderOffset;
		}

		float GetDistance(float startX, float startY, float endX, float endY)
		{
			float deltaX = endX - startX;
			float deltaY = endY - startY;

			return (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
		}

		float GetDistance(Pose start, Pose end)
		{
			var ikStart = _ik.GetTarget(start);
			var ikEnd = _ik.GetTarget(end);
			return GetDistance(ikStart.Result.X, ikStart.Result.Y, ikStart.Result.Z, ikEnd.Result.X, ikEnd.Result.Y, ikEnd.Result.Z);
		}

		float GetDistance(float startX, float startY, float startZ, float endX, float endY, float endZ)
		{
			float deltaX = endX - startX;
			float deltaY = endY - startY;
			float deltaZ = endZ - startZ;

			return (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
		}

		public virtual object Clone()
		{
			var clone = this.MemberwiseClone() as PickPlaceMovements;
			clone.Movements = clone.Movements.Copy();
			return clone;
		}
	}

	public class PickMovements : PickPlaceMovements
	{
		// 0 = same as secondary target but without gripper - ease out
		// 1 = [secondary target] should have open gripper - sync
		// 2 = same as target but without gripper - ease out - sync - include shoulder offset - half speed
		// 3 = [target] should have closed gripper - ease out - sync
		// 4 = same as secondary target without gripper - ease in - sync -  include shoulder offset - half speed

		public PickMovements(IKinematics ik, IEnumerable<RecordedMovement> movements) : base(ik, movements)
		{
		}

		public PickMovements(IKinematics ik, RecordedMovement target, RecordedMovement entry = null) : base(ik, target, entry)
		{
		}

		protected override int MovementCount => 5;

		public override int TargetIndex => 3;

		public override int EntryIndex => 1;

		public override int OpenGripperIndex => EntryIndex;

		public override PickAndPlaceType PickAndPlaceType => PickAndPlaceType.Pick;

		protected override RecordedMovement[] CreateMovements()
		{
			return new RecordedMovement[]
			{
				new RecordedMovement() { EaseIn = 0, EaseOut = 50, Synchronized = false },
				new RecordedMovement() { EaseIn = 0, EaseOut = 0, Synchronized = false },//true },
				new RecordedMovement() { EaseIn = 0, EaseOut = 80, Synchronized = true },
				new RecordedMovement() { EaseIn = 0, EaseOut = 0, Synchronized = true },
				new RecordedMovement() { EaseIn = 0, EaseOut = 30, Synchronized = true },
			};
		}

		protected override void UpdatePoses()
		{
			Movements[0].Pose = RemoveGripper(Entry.Pose.Copy());
			Movements[2].Pose = RemoveGripper(AddShoulderOffset(Target.Pose.Copy()));
			Movements[4].Pose = RemoveGripper(DetractShoulderOffset(Entry.Pose.Copy()));
		}

		public override void SetSpeed(int speed)
		{
			//set speed on all movements
			Movements[0].Speed = speed;
			Movements[1].Speed = speed;
			Movements[2].Speed = speed / 2;
			Movements[3].Speed = speed;
			Movements[4].Speed = speed / 2;
		}
	}

	public class PlaceMovements : PickPlaceMovements
	{
		// 0 = [secondary target] should have closed gripper - ease out
		// 1 = same as target but without gripper - sync - include shoulder offset - half speed - ease out
		// 2 = [target] should have closed gripper - ease in - sync
		// 3 = same as target but without gripper - sync
		// 4 = [secondary target]

		public PlaceMovements(IKinematics ik, IEnumerable<RecordedMovement> movements) : base(ik, movements)
		{
		}

		public PlaceMovements(IKinematics ik, RecordedMovement target, RecordedMovement entry = null) : base(ik, target, entry)
		{
		}

		protected override int MovementCount => 5;

		public override int TargetIndex => 2;

		public override int EntryIndex => 0;

		public override int OpenGripperIndex => TargetIndex;

		public override PickAndPlaceType PickAndPlaceType => PickAndPlaceType.Place;

		protected override RecordedMovement[] CreateMovements()
		{
			return new RecordedMovement[]
			{
				new RecordedMovement() { EaseIn = 0, EaseOut = 50, Synchronized = false },
				new RecordedMovement() { EaseIn = 0, EaseOut = 80, Synchronized = true },
				new RecordedMovement() { EaseIn = 0, EaseOut = 0, Synchronized = true },
				new RecordedMovement() { EaseIn = 0, EaseOut = 30, Synchronized = true },
				new RecordedMovement() { EaseIn = 0, EaseOut = 0, Synchronized = false },
			};
		}

		protected override void UpdatePoses()
		{
			Movements[1].Pose = RemoveGripper(DetractShoulderOffset(Target.Pose.Copy()));
			Movements[3].Pose = RemoveGripper(AddShoulderOffset(Entry.Pose.Copy()));
			Movements[4].Pose = AddShoulderOffset(Entry.Pose.Copy());
		}

		public override void SetSpeed(int speed)
		{
			//set speed on all movements
			Movements[0].Speed = speed;
			Movements[1].Speed = speed / 2;
			Movements[2].Speed = speed;
			Movements[3].Speed = speed / 2;
			Movements[4].Speed = speed;
		}
	}
}
