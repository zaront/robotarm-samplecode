using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using zArm.Api.Commands;

namespace zArm.Api
{
    public class Servo
    {
        CommandRunner _runner;
        Arm _arm;
        int _servoID;
        bool _isOn;

        public event EventHandler<DataEventArg<ServoPositionChangedResponse>> PositionChanged;
        public event EventHandler<DataEventArg<ServoStatusResponse>> StatusReceived;
        public event EventHandler<DataEventArg<MoveResponse>> MoveDone;
        public event EventHandler<DataEventArg<ServoOnChanged>> OnChanged;

        internal Servo(CommandRunner runner, Arm arm, int servoID)
        {
            //set fields
            _runner = runner;
            _arm = arm;
            _servoID = servoID;
        }

        /// <summary>
        /// turn the servo on
        /// </summary>
        public void On()
        {
            _runner.Execute(new ServoOnCommand() { ServoID = _servoID });
        }

        /// <summary>
        /// turn the servo off
        /// </summary>
        public void Off()
        {
            _runner.Execute(new ServoOffCommand() { ServoID = _servoID });
        }

        public bool IsOn
        {
            get { return _isOn; }
        }

        /// <summary>
        /// enable sending PositionChanged events
        /// </summary>
        public void EnablePositionChanged()
        {
            _runner.Execute(new ServoPositionChangedCommand() { ServoID = _servoID, Enabled = true });
        }

        /// <summary>
        /// disable sending PositionChanged events
        /// </summary>
        public void DisablePositionChanged()
        {
            _runner.Execute(new ServoPositionChangedCommand() { ServoID = _servoID, Enabled = false });
        }

        /// <summary>
        /// moves the sevo instantly
        /// </summary>
        /// <param name="position">from -180 - 360</param>
        public void SetPosition(float position)
        {
            _runner.Execute(new ServoPositionCommand() { ServoID = _servoID, Position = position });
        }

        /// <summary>
        /// moves the sevo instantly.  used for calibrating a servo's max speed.  returns with the duration when the servo apears to have stoped moving.
        /// </summary>
        /// <param name="position">from -180 - 360</param>
        public async Task<ServoCalibrationMoveResponse> CalibrationMoveAsync(float position)
        {
            return await _runner.ExecuteAsync(new AsyncCommandFirstServoResponse<ServoCalibrationMoveResponse>(_servoID, new ServoCalibrationMoveCommand() { ServoID = _servoID, Position = position }, timeoutMS: 5000));
        }

        /// <summary>
        /// request to receive the current status of the Servo.  the StatusReceived event will fire after this call
        /// </summary>
        public async Task<ServoStatusResponse> GetStatusAsync()
        {
            return await _runner.ExecuteAsync(new AsyncCommandFirstServoResponse<ServoStatusResponse>(_servoID, new ServoStatusCommand() { ServoID = _servoID }));
        }

        /// <summary>
        /// request to receive the current status of the Servo.  the StatusReceived event will fire after this call
        /// </summary>
        public void GetStatus()
        {
            _runner.Execute(new ServoStatusCommand() { ServoID = _servoID });
        }

        /// <summary>
        /// Animate the servo to a position using speed and easing.  Will turn on the servo if off.  The PositionDone event will fire after the position is reached, or stopped early
        /// </summary>
        /// <param name="position">from -180 - 360</param>
        /// <param name="speed">0-100</param>
        /// <param name="easeIn">0-100</param>
        /// <param name="easeOut">0-100</param>
        public void Move(float position, int? speed = null, int? easeIn = null, int? easeOut = null)
        {
            _runner.Execute(new MoveCommand() { ServoID = _servoID, Position = position, Speed = speed, EaseIn = easeIn, EaseOut = easeOut });
        }

        /// <summary>
        /// Animate the servo to a position using speed and easing.  Will turn on the servo if off.  Returns when position reached or stopped early.
        /// </summary>
        /// <param name="position">from -180 - 360</param>
        /// <param name="speed">0-100</param>
        /// <param name="easeIn">0-100</param>
        /// <param name="easeOut">0-100</param>
        public async Task<MoveResponse> MoveAsync(float position, int? speed = null, int? easeIn = null, int? easeOut = null, CancellationToken? cancelationToken = null)
        {
            return await _runner.ExecuteAsync(new AsyncCommandFirstServoResponse<MoveResponse>(_servoID, new MoveCommand() { ServoID = _servoID, Position = position, Speed = speed, EaseIn = easeIn, EaseOut = easeOut }, cancelationToken, ()=> StopMove(), 
                async origTask =>
                {
                    //wait a bit for the MoveResponse to arrive
                    var completedTask = await Task.WhenAny(origTask, Task.Delay(200)); //timeout in .2 sec
                    if (completedTask == origTask)
                        return await origTask;
                    return null;
                }, Servos.EstimateMoveTimeout(speed)));
        }

        /// <summary>
        /// Stops a move command early, holding the servo's position in the current location.  if Positioning was in progress then the PositionDone event will fire
        /// </summary>
        public void StopMove()
        {
            _runner.Execute(new MoveStopCommand() { ServoID = _servoID });
        }

        internal void StatusResponse(ServoStatusResponse status)
        {
            //fire the event
            StatusReceived?.Invoke(_arm, new DataEventArg<ServoStatusResponse>(status));
        }

        internal void PositionChangedResponse(ServoPositionChangedResponse status)
        {
            //fire the event
            PositionChanged?.Invoke(_arm, new DataEventArg<ServoPositionChangedResponse>(status));
        }

        internal void MoveResponse(MoveResponse status)
        {
            //fire the event
            MoveDone?.Invoke(_arm, new DataEventArg<MoveResponse>(status));
        }

        internal void OnOffResponse(bool isOn)
        {
            _isOn = isOn;

            //fire the event
            OnChanged?.Invoke(_arm, new DataEventArg<ServoOnChanged>(new ServoOnChanged() { ServoID = ServoID, IsOn = isOn }));
        }

        public int ServoID
        {
            get { return _servoID; }
        }

    }



    public class ServoOnChanged : EventArgs
    {
        public int ServoID { get; set; }
        public bool IsOn { get; set; }
    }
}
