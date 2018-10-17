using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Urho;
using Urho.Physics;

namespace zArm.Simulation.Components
{
    public class CameraOrbit : Component, IInputReceiver
    {
        const float _distanceMin = 10;
        const float _distanceMax = 120;
        Node _orbitTarget;
        Node _camera;
        bool _transition;
        Vector3 _targetPosition;
        float? _transitionPitch;
        float? _transitionYaw;
        float? _transitionDistance;
        Node _target;
		float _minPitch = -90f;

		public float MouseSensitivity { get; set; } = .75f;
        public float MoveSpeed { get; set; } = 10.0f;
        public float Distance { get; set; }
        public bool AutoCenter { get; set; } = true;
        public bool AutoDistance { get; set; } = true;
		public Node Floor { get; set; }

		void IInputReceiver.OnInputUpdate(float timeStep, IInputPermissions inputPermissions)
        {
            if (!inputPermissions.CanUseMouse)
            {
                _transition = true;
                return;
            }

            var input = Application.Input;

            //create helper nodes
            if (_orbitTarget == null)
            {
                _orbitTarget = Scene.CreateChild("orbitTarget");
                _camera = _orbitTarget.CreateChild("orbitCamera");

                //setup auto distance too
                var body = Node.GetOrCreateComponent<RigidBody>();
                var collide = Node.GetOrCreateComponent<CollisionShape>();
                collide.SetSphere(5, Vector3.Zero, Quaternion.Identity);
                body.Trigger = true;
                body.Mass = 1;
                body.Kinematic = true;
                Node.NodeCollisionStart += Camera_NodeCollisionStart;
            }

            //get target pos
            if (AutoCenter || _targetPosition == null || _targetPosition == Vector3.Zero)
                _targetPosition = GetTargetBoundingBox()?.Center ?? new Vector3();

            //set distance
            if (_transitionDistance == null)
                Distance = Vector3.Distance(_targetPosition, Node.Position);

			//change distance
			input.GetMouseWheelOrPinch(out var wheel);
            Distance -= MoveSpeed * wheel * 1f;
            Distance = MathHelper.Clamp(Distance, _distanceMin, _distanceMax);

            var cameraRotation = Node.GetOrCreateComponent<CameraRotation>();
            if (input.GetMouseDownOrTouching(out var mouseMove))
            {
                inputPermissions.RequestExclusiveMouse = true;
                _transition = false;

                //change rotation
                cameraRotation.Yaw += MouseSensitivity * mouseMove.X;
                cameraRotation.Pitch += MouseSensitivity * mouseMove.Y;
                cameraRotation.Pitch = MathHelper.Clamp(cameraRotation.Pitch, _minPitch, 90);
            }
            else
                inputPermissions.RequestExclusiveMouse = false;

            //set rotation
            Node.Rotation = new Quaternion(cameraRotation.Pitch, cameraRotation.Yaw, 0);

            //calculate position
            _orbitTarget.Position = _targetPosition;
            _orbitTarget.Rotation = new Quaternion(cameraRotation.Pitch, cameraRotation.Yaw, 0);
            _camera.Position = new Vector3(0, 0, -Distance);

            //set position
            if (_transition && AutoCenter)
            {
                //transition target
                var speed = MathHelper.Clamp(.1668f * timeStep, .1f, 1);
                Node.SetWorldPosition(Vector3.Lerp(Node.Position, _camera.WorldPosition, speed));
                if (_transitionDistance != null)
                    Distance = MathHelper.Lerp(Distance, _transitionDistance.Value, speed);
                if (_transitionPitch != null)
                    cameraRotation.Pitch = MathHelper.Lerp(cameraRotation.Pitch, _transitionPitch.Value, speed);
                if (_transitionYaw != null)
                    cameraRotation.Yaw = MathHelper.Lerp(cameraRotation.Yaw, _transitionYaw.Value, speed);

                //transition is complete
                if ((Node.Position - _camera.WorldPosition).Length < .05f)
                    _transition = false;
            }
            else
            {
                Node.SetWorldPosition(_camera.WorldPosition);
                if (_transitionDistance != null)
                {
                    Distance = _transitionDistance.Value;
                    _transitionDistance = null;
                }
                if (_transitionPitch != null)
                {
                    cameraRotation.Pitch = _transitionPitch.Value;
                    _transitionPitch = null;
                }
                if (_transitionYaw != null)
                {
                    cameraRotation.Yaw = _transitionYaw.Value;
                    _transitionYaw = null;
                }
            }

			//set min pitch
			if (Floor != null)
			{
				var angleToFloor = MathHelper.RadiansToDegrees((float)Math.Atan((_orbitTarget.WorldPosition.Y - Floor.WorldPosition.Y - 2f) / Distance));
				_minPitch = MathHelper.Clamp(-angleToFloor, -90, 0);
			}
			
		}

		void Camera_NodeCollisionStart(NodeCollisionStartEventArgs e)
        {
            if (!AutoDistance)
                return;

			//moveable object - increase the distance
			if (e.OtherBody.Mass > 0)
			{
				Distance = MathHelper.Clamp(Distance + 2, _distanceMin, _distanceMax);
				_transitionDistance = Distance;
			}
        }

        public void StartTransition()
        {
            _transition = true;
        }

        BoundingBox? GetTargetBoundingBox()
        {
            BoundingBox? result = null;
            if (Target != null)
            {
                var models = Target.Components.OfType<StaticModel>().Concat(Target.GetChildrenWithComponent<StaticModel>(true).SelectMany(i => i.Components.OfType<StaticModel>()));
                foreach (var model in models)
                    if (result == null)
                        result = model.WorldBoundingBox;
                    else
                    {
                        var bound = model.WorldBoundingBox;
                        result = new BoundingBox(Vector3.Min(result.Value.Min, bound.Min), Vector3.Max(result.Value.Max, bound.Max));
                    }
            }
            return result;
        }

        public Node Target
        {
            get { return _target; }
            set
            {
                if (_target != null)
                    _transition = true;
                _target = value;
            }
        }

        public void SetTarget(Node target, float? distance = null, float? pitch = null, float? yaw = null)
        {
            Target = target;
            _transitionDistance = distance;
            _transitionPitch = pitch;
            _transitionYaw = yaw;
        }

        public bool IsTransitioning
        {
            get { return _transition; }
        }

        void IInputReceiver.ReceivedInputControl(IInputPermissions inputPermissions)
        {
        }

        void IInputReceiver.RevokedInputControl(IInputPermissions inputPermissions)
        {
        }
    }
}
