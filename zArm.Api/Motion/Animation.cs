using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace zArm.Api.Motion
{
	[Obsolete]
	public class Animation
    {
        ReaderWriterLockSlim _sync = new ReaderWriterLockSlim();
        List<MotionPose> _poses = new List<MotionPose>();
        List<PoseSpeedControl> _speedControls = new List<PoseSpeedControl>();
        double _totalTime;

        public PoseSpeed DefaultTopSpeed { get; set; } = PoseSpeed.Normal;
        public PoseSpeed DefaultAcceleration { get; set; } = PoseSpeed.Normal;

        public List<MotionPose> Poses
        {
            get { return _poses; }
        }

        public void UpdateAnimation()
        {
            _sync.EnterWriteLock();
            try
            {
                UpdateState();
            }
            finally
            {
                _sync.ExitWriteLock();
            }
        }

        public AnimationPose GetPosition(double time)
        {
            _sync.EnterReadLock();
            try
            {
                //find the speedControl pose
                PoseSpeedControl currentSpeedControl = null;
                var index = 0;
                var startTime = 0d;
                if (time < 0)
                {
                    currentSpeedControl = _speedControls.FirstOrDefault();
                }
                else if (time > _totalTime)
                {
                    currentSpeedControl = _speedControls.LastOrDefault();
                    if (currentSpeedControl != null)
                    {
                        startTime = _totalTime - currentSpeedControl.TotalTime;
                        index = _speedControls.Count - 1;
                    }
                }
                else
                {

                    foreach (var speedControl in _speedControls)
                    {
                        if (time >= startTime && time <= startTime + speedControl.TotalTime)
                        {
                            currentSpeedControl = speedControl;
                            break;
                        }
                        startTime += speedControl.TotalTime;
                        index++;
                    }
                }
                if (currentSpeedControl == null)
                    return null;

                //get positions
                var timeSlice = time - startTime;
                var pose = CreatePose(timeSlice, currentSpeedControl);
                var indexCompleted = GetCompleted(timeSlice, currentSpeedControl.TotalTime);
                var completed = GetCompleted(time, _totalTime);

                //return
                return new AnimationPose() { Index = index, Pose = pose, Completed = completed, IndexCompleted = indexCompleted, IsCompleted = completed == 1 };
            }
            finally
            {
                _sync.ExitReadLock();
            }
        }

        MotionPose CreatePose(double time, PoseSpeedControl poseSpeedControl)
        {
            var result = new MotionPose();
            for (int i = 0; i < poseSpeedControl.Count; i++)
            {
                var speedControl = poseSpeedControl[i];
                if (speedControl != null)
                    result[i] = speedControl.GetPosition(time);
            }
            return result;
        }

        public double GetCompleted(double time)
        {
            _sync.EnterReadLock();
            try
            {
                return GetCompleted(time, _totalTime);
            }
            finally
            {
                _sync.ExitReadLock();
            }
        }

        public TimeSpan GetTime(double completed)
        {
            _sync.EnterReadLock();
            try
            {
                if (completed <= 0)
                    return TimeSpan.Zero;
                if (completed >= 1)
                    return TimeSpan.FromSeconds(_totalTime);
                return TimeSpan.FromSeconds(_totalTime * completed);
            }
            finally
            {
                _sync.ExitReadLock();
            }
        }

        double GetCompleted(double time, double totalTime)
        {
            if (time <= 0)
                return 0;
            if (time >= totalTime)
                return 1;
            return time / totalTime;
        }

        public double TotalTime
        {
            get { return _totalTime; }
        }

        void UpdateState()
        {
            //create speed controls
            _speedControls.Clear();
            MotionPose startPose = null;
            foreach (var endPose in _poses)
            {
                //get start pose
                if (startPose == null)
                {
                    startPose = endPose;
                    continue;
                }
                //create SpeedControl
                _speedControls.Add(CreateSpeedControl(startPose, endPose));
                startPose = endPose;
            }

            //calculate totalTime
            _totalTime = _speedControls.Sum(i => i.TotalTime);
        }

        PoseSpeedControl CreateSpeedControl(MotionPose start, MotionPose end)
        {
            var result = new PoseSpeedControl();
            for (int i = 0; i < start.Count; i++)
            {
                var startPosition = start[i];
                var endPosition = end[i];
                if (startPosition != null && endPosition != null)
                    result[i] = new SpeedControl(startPosition.Value, endPosition.Value, ConvertSpeed(end.Acceleration, DefaultAcceleration), ConvertSpeed(end.TopSpeed, DefaultTopSpeed));
            }
            result.TotalTime = result.Servos.Max(i => i != null ? i.TotalTime : 0d);
            return result;
        }

        double ConvertSpeed(PoseSpeed? speed, PoseSpeed defaultSpeed)
        {
            if (speed == null)
                speed = defaultSpeed;
            switch (speed)
            {
                case PoseSpeed.Fastest: return 900;
                case PoseSpeed.Fast: return 400;
                case PoseSpeed.Slow: return 95;
                case PoseSpeed.Slowest: return 10;
                case PoseSpeed.Normal:
                default:
                    return 150;
            }
        }

		[Obsolete]
        class PoseSpeedControl : ServoIndexer<SpeedControl>
        {
            public double TotalTime { get; set; }
        }

    }



	[Obsolete]
	public class AnimationPose
    {
        public MotionPose Pose { get; set; }
        public int Index { get; set; }
        public double IndexCompleted { get; set; }
        public double Completed { get; set; }
        public bool IsCompleted { get; set; }
    }

}
