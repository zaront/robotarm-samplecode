using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace zArm.Api.Motion
{
	[Obsolete]
	public class AnimationPlayer
    {
        Arm _arm;
        Timer _timer;
        bool _isPlaying;
        Animation _animation;
        Animation _transition;
        TimeSpan _startTimeOffset;
        DateTime _startTime;
        int _servoCount;
        MotionPose _prevPose;
        bool _isPlayingTransition;
        AnimationStatus _prevStatus;
        DateTime _prevStatusSent;
        TimeSpan _statusChangedFireDuration = TimeSpan.FromSeconds(.1);
        int _index;
        bool _isCompleted;
        double _completed;
        double _indexCompleted;
        bool _transitionOnly;

        public event EventHandler<DataEventArg<AnimationStatus>> StatusChanged;

        public bool ServosOffOnStop { get; set; } = true;

        public AnimationPlayer(Arm arm, double intervalMS = 10)
        {
            //set fields
            _arm = arm;
            _servoCount = _arm.Servos.Count;
            _timer = new Timer(intervalMS);
            _timer.Elapsed += Timer_Elapsed;
        }

        public bool IsPlaying
        {
            get { return _isPlaying; }
        }

        public async Task Play(Animation animation = null, double? startPercentage = null, bool transitionOnly = false, bool superImpose = false)
        {
            //validate
            if (animation == null && _animation == null)
                return;

            Stop();

            //assign the animation
            if (animation != null)
                _animation = animation;

            //validate animation length
            if (_animation.Poses.Count == 0)
                return;

            _isPlaying = true;
            _prevStatus = null;
            _index = 0;
            _isCompleted = false;
            _completed = 0;
            _indexCompleted = 0;
            _transitionOnly = transitionOnly;

            //set starting time offset
            if (startPercentage == null)
                startPercentage = 0;
            else if (startPercentage < 0)
                startPercentage = 0;
            else if (startPercentage > 1)
                startPercentage = 1;
            _startTimeOffset = animation.GetTime(startPercentage.Value);

            //Get current pose
            var pose = await _arm.GetCurrentPoseAsync();

            //calculate the transition
            _transition = new Animation() { DefaultAcceleration = _animation.DefaultAcceleration, DefaultTopSpeed = _animation.DefaultTopSpeed };
            _transition.Poses.Add(pose);
            var endPose = _animation.Poses.First();
            var animationPose = _animation.GetPosition(_startTimeOffset.TotalSeconds);
            if (animationPose != null)
                endPose = animationPose.Pose;
            _transition.Poses.Add(endPose);
            _transition.UpdateAnimation();

            //turn on servos at current position
            _prevPose = new MotionPose();
            MoveToPose(pose);
            _arm.Servos.On();

            //start the play timer
            _startTime = DateTime.Now;
            _timer.Start();
        }

        public void Stop()
        {
            if (_isPlaying)
            {
                _isPlaying = false;
                _isPlayingTransition = false;
                _index = 0;
                _isCompleted = false;
                _completed = 0;
                _indexCompleted = 0;
                _timer.Stop();

                if (ServosOffOnStop)
                    _arm.Servos.Off();

                FireStatusChanged();
            }
        }

        void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //validate
            if (!_isPlaying)
                return;

            bool fireStop = false;

            //get the time
            var time = (DateTime.Now - _startTime).TotalSeconds;

            //move through the transition
            var transition = _transition;
            if (transition != null)
            {
                _isPlayingTransition = true;

                //move servos
                var animationPose = transition.GetPosition(time);
                if (animationPose != null)
                    MoveToPose(animationPose.Pose);

                //finish with transition
                if (animationPose == null || animationPose.IsCompleted)
                {
                    _startTime = DateTime.Now - _startTimeOffset;
                    _isPlayingTransition = false;
                    _transition = null;

                    //stop if transition only
                    if (_transitionOnly)
                        fireStop = true;
                }
            }

            //move through the animation
            else
            {
                var animation = _animation;
                if (animation != null)
                {
                    //move servos
                    var animationPose = animation.GetPosition(time);
                    if (animationPose != null)
                    {
                        MoveToPose(animationPose.Pose);

                        //update status
                        _index = animationPose.Index;
                        _isCompleted = animationPose.IsCompleted;
                        _completed = animationPose.Completed;
                        _indexCompleted = animationPose.IndexCompleted;
                    }

                    //finish with animation
                    if (animationPose == null || animationPose.IsCompleted)
                    {
                        fireStop = true;
                    }
                }
                else
                    fireStop = true;
            }

            FireStatusChanged();

            //stop
            if (fireStop)
                Stop();
        }

        void FireStatusChanged()
        {
            //validate
            if (StatusChanged == null)
                return;

            //create new status
            var status = new AnimationStatus()
            {
                IsPlaying = _isPlaying,
                IsPlayingTransition = _isPlayingTransition,
                Completed = _completed,
                Index = _index,
                IndexCompleted = _indexCompleted,
                IsCompleted = _isCompleted
            };

            //if new status is diffrent then fire the event
            bool fireEvent = false;
            if (_prevStatus == null)
                fireEvent = true;
            else
            {
                var isDiffrent = status.AreDiffrent(_prevStatus);
                fireEvent = isDiffrent.GetValueOrDefault();
                if (isDiffrent == null)
                {
                    //only fire once every .5 seconds
                    fireEvent = (DateTime.Now - _prevStatusSent) >= _statusChangedFireDuration;
                }
            }

            //fire the event
            if (fireEvent)
            {
                _prevStatus = status;
                _prevStatusSent = DateTime.Now;
                StatusChanged(this, new DataEventArg<AnimationStatus>(status));
            }
        }

        void MoveToPose(MotionPose pose)
        {
            foreach (var servo in _arm.Servos)
            {
                //get positions
                var position = pose[servo.ServoID-1];
                var prevPosition = _prevPose[servo.ServoID-1];

                //validate
                if (position == null)
                    continue;

                //move servo - if changed degree
                var degree = (int)Math.Round(position.Value);
                int? prevDegree = null;
                if (prevPosition != null)
                    prevDegree = (int)Math.Round(prevPosition.Value);
                if (prevDegree == null || prevDegree.Value != degree)
                {
                    servo.SetPosition(degree);
                }   
            }
            _prevPose = pose;
        }

    }


	[Obsolete]
	public class AnimationStatus
    {
        public bool IsPlaying { get; set; }
        public bool IsPlayingTransition { get; set; }
        public int Index { get; set; }
        public bool IsCompleted { get; set; }

        public double Completed { get; set; }
        public double IndexCompleted { get; set; }

        /// <returns>null means only the complete is diffrent</returns>
        internal bool? AreDiffrent(AnimationStatus status)
        {
            //anything changed
            if (IsPlaying != status.IsPlaying) return true;
            if (IsPlayingTransition != status.IsPlayingTransition) return true;
            if (Index != status.Index) return true;
            if (IsCompleted != status.IsCompleted) return true;
            //only the completed amount has changed
            if (Completed != status.Completed) return null;
            if (IndexCompleted != status.IndexCompleted) return null;

            //nothing changed
            return false;
        }
    }
}
