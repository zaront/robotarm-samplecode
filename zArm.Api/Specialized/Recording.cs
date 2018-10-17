using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zArm.Api;

namespace zArm.Api.Specialized
{
    public class Recording
    {
        public RecordingType RecordingType { get; set; }
        public RecordedMovement[] Movements { get; set; }
        public RecordingAdjustment Adjustments { get; set; }
    }

    public class RecordedMovement
    {
		public string Description { get; set; }
        public Pose Pose { get; set; }
        public TimeSpan? Delay { get; set; }
        public int? Speed { get; set; }
        public int? EaseIn { get; set; }
        public int? EaseOut { get; set; }
        public bool? Synchronized { get; set; }
        public RecordedMovementType Type { get; set; }
		public PickAndPlaceType? PickAndPlaceType { get; set; }
    }

    public enum RecordingType
    {
        RealTime,
        Position,
        AutoPosition,
        PickAndPlace
    }

	public enum PickAndPlaceType
	{
		Pick,
		Place,
		Safety
	}


	public enum RecordedMovementType
    {
        Position,
        Move
    }

    public class RecordingAdjustment
    {
        ServoAdjustment[] _servoAdjustment = new ServoAdjustment[7];

        public RecordingAdjustment()
        {
            //set fields
            for (int i = 0; i < 7; i++)
                _servoAdjustment[i] = new ServoAdjustment();
        }

        public ServoAdjustment this[int index]
        {
            get { return _servoAdjustment[index]; }
        }

        public ServoAdjustment GetServoAdjustment(int servoID)
        {
            return _servoAdjustment[servoID - 1];
        }

        public int? PlaybackSpeed { get; set; }

        public PoseAdjustment AdjustmentX { get; set; }
        public PoseAdjustment AdjustmentY { get; set; }
        public PoseAdjustment AdjustmentZ { get; set; }
    }

    public class ServoAdjustment
    {
        public int? StartIndex { get; set; }
        public int? EndIndex { get; set; }
        public bool Enabled { get; set; } = true;
        public PoseAdjustment Adjustment { get; set; }
    }

    public class PoseAdjustment
    {
        public float? Max { get; set; }
        public float? Min { get; set; }
        public float? Offset { get; set; }
    }

}
