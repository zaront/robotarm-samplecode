using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zArm.Api.Commands;

namespace zArm.Api
{
    public class Led
    {
        CommandRunner _runner;
        Arm _arm;

        public event EventHandler<DataEventArg<LedStatusResponse>> StatusReceived;

        internal Led(CommandRunner runner, Arm arm)
        {
            //set fields
            _runner = runner;
            _arm = arm;

            //register responses
            _runner.RegisterForResponse<LedStatusResponse>(StatusResponse);
        }

        /// <summary>
        /// turn the Led on
        /// <param name="color">the color to display</param>
        /// </summary>
        public void On(LedColor? color = null)
        {
            _runner.Execute(new LedOnCommand() { Color = color });
        }

        /// <summary>
        /// turn the Led off
        /// </summary>
        public void Off()
        {
            _runner.Execute(new LedOffCommand());
        }

        /// <summary>
        /// turn on the Led at a specific brightness - 0 turns it off
        /// </summary>
        /// <param name="brightness">from 0 - 10, null is full brightness, 0 is off</param>
        /// <param name="color">the color to display</param>
        public void Fade(int? brightness = null, LedColor ? color = null)
        {
            _runner.Execute(new LedFadeCommand() { Brightness = brightness, Color = color });
        }

        /// <summary>
        /// blink the Led
        /// </summary>
        /// <param name="speed">from 1 - 10, how fast to blink</param>
        /// <param name="count">from 0 - 100, how many times to blink</param>
        /// <param name="color">the color to display</param>
        public void Blink(int? speed = null, int? count = null, LedColor? color = null)
        {
            _runner.Execute(new LedBlinkCommand() { Speed = speed, Count = count, Color = color });
        }

        /// <summary>
        /// pulse the Led
        /// </summary>
        /// <param name="speed">from 1 - 10, how fast to blink</param>
        /// <param name="count">from 0 - 100, how many times to blink</param>
        /// <param name="color">the color to display</param>
        public void Pulse(int? speed = null, int? count = null, LedColor? color = null)
        {
            _runner.Execute(new LedPulseCommand() { Speed = speed, Count = count, Color = color });
        }

        /// <summary>
        /// will turn the led on while the button is pressed
        /// </summary>
        /// <param name="brightness">from 0 - 10, null is full brightness, 0 is off</param>
        /// <param name="color">the color to display</param>
        public void SyncWithButton(int? brightness = null, LedColor? color = null)
        {
            _runner.Execute(new LedSyncButtonCommand() { Brightness = brightness, Color = color });
        }

        /// <summary>
        /// will not turn off the led while the button is pressed
        /// </summary>
        public void SyncWithButtonOff()
        {
            _runner.Execute(new LedSyncButtonOffCommand());
        }

        /// <summary>
        /// request to receive the current status of the Led.  the StatusReceived event will fire after this call
        /// </summary>
        public async Task<LedStatusResponse> GetStatusAsync()
        {
            return await _runner.ExecuteAsync(new AsyncCommandFirstResponse<LedStatusResponse>(new LedStatusCommand()));
        }

        void StatusResponse(LedStatusResponse status)
        {
            //fire the event
            StatusReceived?.Invoke(_arm, new DataEventArg<LedStatusResponse>(status));
        }

    }

}
