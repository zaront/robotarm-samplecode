using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace zArm.Api.Motion
{
	[Obsolete]
	public class AnimationRecorder
    {
        Arm _arm;
        int _servoCount;
        Animation _animation;
        Timer _idleTimeout;

        public event EventHandler<DataEventArg<MotionPose>> PositionRecorded;

        public AnimationRecorder(Arm arm)
        {
            //set fields
            _arm = arm;
            _servoCount = _arm.Servos.Count;
            _animation = new Animation();
        }

        public void StartAutoRecord(double idleTimeOut = 1000)
        {
            if (_idleTimeout != null)
                _idleTimeout.Stop();
            _idleTimeout = new Timer(idleTimeOut);
            _idleTimeout.Elapsed += Timeout;
            foreach (var servo in _arm.Servos)
                servo.PositionChanged += Servo_PositionChanged;
        }

        private async void Timeout(object sender, ElapsedEventArgs e)
        {
            //validate
            if (!_idleTimeout.Enabled)
                return;

            //record position
            _idleTimeout.Stop();
            await RecordPosition();
        }

        private void Servo_PositionChanged(object sender, DataEventArg<Commands.ServoPositionChangedResponse> e)
        {
            _idleTimeout.Start();
        }

        public void StopAutoRecord()
        {
            _idleTimeout.Stop();
            foreach (var servo in _arm.Servos)
                servo.PositionChanged -= Servo_PositionChanged;
        }

        public async Task RecordPosition()
        {
            //get the current pose
            var pose = await _arm.GetCurrentPoseAsync();

            //record the position
            _animation.Poses.Add(pose);

            //fire event
            PositionRecorded?.Invoke(this, new DataEventArg<MotionPose>(pose));
        }

        public void NewRecording()
        {
            _animation = new Animation();
        }

        public Animation GetRecording()
        {
            _animation?.UpdateAnimation();
            return _animation;
        }


    }
}
