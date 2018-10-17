using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zArm.Api.Commands;

namespace zArm.Api
{
    public class Button
    {
        CommandRunner _runner;
        Arm _arm;

        /// <summary>
        /// called when the button is pressed down
        /// </summary>
        public event EventHandler<DataEventArg<ButtonDownResponse>> Down;
        /// <summary>
        /// called when the button comes up from being pressed
        /// </summary>
        public event EventHandler<DataEventArg<ButtonUpResponse>> Up;
        public event EventHandler<DataEventArg<ButtonStatusResponse>> StatusReceived;

        internal Button(CommandRunner runner, Arm arm)
        {
            //set fields
            _runner = runner;
            _arm = arm;

            //register responses
            _runner.RegisterForResponse<ButtonDownResponse>(DownResponse);
            _runner.RegisterForResponse<ButtonUpResponse>(UpResponse);
            _runner.RegisterForResponse<ButtonStatusResponse>(StatusResponse);
        }

        public void ResetPressedCount()
        {
            _runner.Execute(new ButtonCountResetCommand());
        }

        /// <summary>
        /// request to receive the current status of the button.  the StatusReceived event will fire after this call
        /// </summary>
        public async Task<ButtonStatusResponse> GetStatusAsync()
        {
            return await _runner.ExecuteAsync(new AsyncCommandFirstResponse<ButtonStatusResponse>(new ButtonStatusCommand()));
        }

        void DownResponse(ButtonDownResponse status)
        {
            //fire the event
            Down?.Invoke(_arm, new DataEventArg<ButtonDownResponse>(status));
        }

        void UpResponse(ButtonUpResponse status)
        {
            //fire the event
            Up?.Invoke(_arm, new DataEventArg<ButtonUpResponse>(status));
        }

        void StatusResponse(ButtonStatusResponse status)
        {
            //fire the event
            StatusReceived?.Invoke(_arm, new DataEventArg<ButtonStatusResponse>(status));
        }

    }

}
