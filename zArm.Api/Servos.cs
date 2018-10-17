using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using zArm.Api.Commands;

namespace zArm.Api
{
    public class Servos : List<Servo>
    {
        CommandRunner _runner;
        Arm _arm;
        int _servoCount;

        public event EventHandler<DataEventArg<ServoPositionChangedResponse>> PositionChanged;

        internal Servos(CommandRunner runner, Arm arm, int servoCount)
        {
            //set fields
            _runner = runner;
            _arm = arm;
            _servoCount = servoCount;

            //add servos
            for (int i = 1; i <= servoCount; i++)
                Add(new Servo(_runner, _arm, i));

            //register responses
            _runner.RegisterForResponse<ServoPositionChangedResponse>(PositionChangedResponse);
            _runner.RegisterForResponse<ServoStatusResponse>(StatusResponse);
            _runner.RegisterForResponse<MoveResponse>(MoveResponse);
            _runner.RegisterForResponse<ServoOnResponse>(OnResponse);
            _runner.RegisterForResponse<ServoOffResponse>(OffResponse);
        }

        public Servo GetServo(int servoID)
        {
            if (servoID > 0 && servoID <= this.Count)
                return this[servoID - 1];
            return null;
        }

        /// <summary>
        /// turn all servos on
        /// </summary>
        public void On()
        {
            _runner.Execute(new ServoOnCommand());
        }

        /// <summary>
        /// turn all servos off
        /// </summary>
        public void Off()
        {
            _runner.Execute(new ServoOffCommand());
        }

        /// <summary>
        /// enable sending PositionChanged events for all servos
        /// </summary>
        public void EnablePositionChanged()
        {
            _runner.Execute(new ServoPositionChangedCommand() { Enabled = true });
        }

        /// <summary>
        /// disable sending PositionChanged events for all servos
        /// </summary>
        public void DisablePositionChanged()
        {
            _runner.Execute(new ServoPositionChangedCommand() { Enabled = false });
        }

        /// <summary>
        /// Animate all the servos to a position using speed and easing.  Will turn on the servo if off.  The PositionDone event will fire for each individual servo after the position is reached, or stopped early
        /// </summary>
        /// <param name="speed">0-100</param>
        /// <param name="easeIn">0-100</param>
        /// <param name="easeOut">0-100</param>
        /// <param name="synchronized">all servos will reach their destination at the same time</param>
        /// <param name="percentageComplete">0-1 representing where to start within the movement</param>
        public void Move(Pose pose, int? speed = null, int? easeIn = null, int? easeOut = null, bool synchronized = false, float ? percentageComplete = null)
        {
            //validate
            if (pose == null)
                return;

            var cmd = (synchronized) ? new MoveAllSynchronizedCommand(_servoCount) : new MoveAllCommand(_servoCount);
            cmd.Servo1_Position = pose.Servo1_Position;
            cmd.Servo2_Position = pose.Servo2_Position;
            cmd.Servo3_Position = pose.Servo3_Position;
            cmd.Servo4_Position = pose.Servo4_Position;
            cmd.Servo5_Position = pose.Servo5_Position;
            cmd.Servo6_Position = pose.Servo6_Position;
            cmd.Servo7_Position = pose.Servo7_Position;
            cmd.Speed = speed;
            cmd.EaseIn = easeIn;
            cmd.EaseOut = easeOut;
            cmd.PercentageComplete = percentageComplete;

            _runner.Execute(cmd);
        }

        /// <summary>
        /// Animate all the servos to a position using speed and easing.  Will turn on the servo if off.  Returns when position reached or stopped early for all the servos.
        /// </summary>
        /// <param name="speed">0-100</param>
        /// <param name="easeIn">0-100</param>
        /// <param name="easeOut">0-100</param>
        /// <param name="synchronized">all servos will reach their destination at the same time</param>
        /// <param name="percentageComplete">0-1 representing where to start within the movement</param>
        public async Task<IEnumerable<MoveResponse>> MoveAsync(Pose pose, int? speed = null, int? easeIn = null, int? easeOut = null, bool synchronized = false, float? percentageComplete = null, CancellationToken? cancelationToken = null)
        {
            //validate
            if (pose == null)
                return null;

            var cmd = (synchronized) ? new MoveAllSynchronizedCommand(_servoCount) : new MoveAllCommand(_servoCount);
            cmd.Servo1_Position = pose.Servo1_Position;
            cmd.Servo2_Position = pose.Servo2_Position;
            cmd.Servo3_Position = pose.Servo3_Position;
            cmd.Servo4_Position = pose.Servo4_Position;
            cmd.Servo5_Position = pose.Servo5_Position;
            cmd.Servo6_Position = pose.Servo6_Position;
            cmd.Servo7_Position = pose.Servo7_Position;
            cmd.Speed = speed;
            cmd.EaseIn = easeIn;
            cmd.EaseOut = easeOut;
            cmd.PercentageComplete = percentageComplete;

            var result = new ConcurrentBag<MoveResponse>();
            var poseCount = pose.Servos.Count(e => e != null);
            return await _runner.ExecuteAsync(new AsyncCommand<IEnumerable<MoveResponse>>(cmd, i=> 
            {
                //wait until all move commands are received
                var response = i as MoveResponse;
                if (response != null)
                {
                    if (result.FirstOrDefault(e => e.ServoID == response.ServoID) == null && pose.Get(response.ServoID) != null)
                    {
                        //add to results
                        result.Add(response);

                        //return when all found
                        if (result.Count == poseCount)
                            return result.OrderBy(e => e.ServoID);
                    }
                }
                return null;
            }, cancelationToken, () => StopMove(),
            async origTask =>
            {
                //wait a bit for the MoveResponses to arrive
                var completedTask = await Task.WhenAny(origTask, Task.Delay(500)); //timeout in .5 sec
                if (completedTask == origTask)
                    return await origTask;
                return null;
            }, EstimateMoveTimeout(speed)));
        }

        /// <summary>
        /// Stops all move commands to all servos early, holding the servo's position in the current location.  if Positioning was in progress then the PositionDone event will fire on each servo stopped
        /// </summary>
        public void StopMove()
        {
            _runner.Execute(new MoveStopCommand());
        }

        void StatusResponse(ServoStatusResponse status)
        {
            //fire the event for the correct servo
            GetServo(status.ServoID)?.StatusResponse(status);
        }

        void PositionChangedResponse(ServoPositionChangedResponse status)
        {
            //fire the event
            PositionChanged?.Invoke(_arm, new DataEventArg<ServoPositionChangedResponse>(status));

            //fire the event for the correct servo
            GetServo(status.ServoID)?.PositionChangedResponse(status);
        }

        void MoveResponse(MoveResponse status)
        {
            //fire the event for the correct servo
            GetServo(status.ServoID)?.MoveResponse(status);
        }

        void OnResponse(ServoOnResponse status)
        {
            //fire the event for the correct servo
            GetServo(status.ServoID)?.OnOffResponse(true);
        }

        void OffResponse(ServoOffResponse status)
        {
            //fire the event for the correct servo
            GetServo(status.ServoID)?.OnOffResponse(false);
        }

        public void EnableCalibration()
        {
            _runner.Execute(new ServoSetCalibrationCommand() { Enable = true });
        }

        public void DisableCalibration()
        {
            _runner.Execute(new ServoSetCalibrationCommand() { Enable = false });
        }

        public async Task<bool> IsCalibrationEnabledAsync()
        {
            return (await _runner.ExecuteAsync(new AsyncCommandFirstResponse<ServoGetCalibrationResponse>(new ServoGetCalibrationCommand()))).Enabled;
        }

        /// <summary>
        /// request to receive the current status of all Servos.  the StatusReceived event will fire after this call for each servo
        /// </summary>
        public async Task<ServoStatusResponse[]> GetStatusAsync()
        {
            var result = new ServoStatusResponse[Count];
            return await _runner.ExecuteAsync(new AsyncCommand<ServoStatusResponse[]>(new ServoStatusCommand(), i =>
            {
                var response = i as ServoStatusResponse;
                if (response != null)
                {
                    //record response for servo
                    result[response.ServoID - 1] = response;

                    //return when all received
                    if (!result.Any(e => e == null))
                        return result;
                }
                return null;
            }));
        }

        /// <summary>
        /// request to receive the current status of all Servos.  the StatusReceived event will fire after this call for each servo
        /// </summary>
        public void GetStatus()
        {
            _runner.Execute(new ServoStatusCommand());
        }

		/// <summary>
		/// returns the current servo position
		/// </summary>
		/// <param name="useLastSetPosition">if a servo is on it uses the servos last set position, otherwise uses the feeback position</param>
		/// <returns></returns>
		public async Task<Pose> GetPoseAsync(bool useLastSetPosition = false)
        {
            var status = await GetStatusAsync();
			if (status != null)
			{
				if (useLastSetPosition)
					return new Pose(status.Select(i => i.IsOn ? i.LastSetPosition : i.Position));
				else
					return new Pose(status.Select(i => i.Position));
			}
			return null;
        }

        internal static int EstimateMoveTimeout(int? speed)
        {
            if (speed == null)
                speed = 50;
            if (speed > 100)
                speed = 100;
            if (speed < 1)
                speed = 1;
            return (int)Math.Round(57000 * (Math.Pow(speed.Value, -.76)));

            //data used for timout estimation
            //1   57000
            //5   12000
            //10  7000
            //20  4000
            //50  3000
            //100 2000

        }
    }

}
