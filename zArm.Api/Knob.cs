using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zArm.Api.Commands;

namespace zArm.Api
{
    public class Knob
    {
        CommandRunner _runner;
        Arm _arm;

        /// <summary>
        /// called when the knob has been twisted
        /// </summary>
        public event EventHandler<DataEventArg<KnobPositionChangedResponse>> PositionChanged;
        public event EventHandler<DataEventArg<KnobPositionResponse>> Position;
        public event EventHandler<DataEventArg<KnobRangeResponse>> Range;

        internal Knob(CommandRunner runner, Arm arm)
        {
            //set fields
            _runner = runner;
            _arm = arm;

            //register responses
            _runner.RegisterForResponse<KnobPositionChangedResponse>(PositionChangedResponse);
            _runner.RegisterForResponse<KnobPositionResponse>(PositionResponse);
            _runner.RegisterForResponse<KnobRangeResponse>(RangeResponse);
        }

        public void SetPosition(int position)
        {
            _runner.Execute(new KnobPositionSetCommand() { Position = position });
        }

        public void SetRange(int min, int max)
        {
            _runner.Execute(new KnobRangeSetCommand() { Min = min, Max = max });
        }

        /// <summary>
        /// request to receive the current position of the knob.  the Position event will fire after this call
        /// </summary>
        public async Task<KnobPositionResponse> GetPositionAsync()
        {
            return await _runner.ExecuteAsync(new AsyncCommandFirstResponse<KnobPositionResponse>(new KnobPositionCommand()));
        }

        /// <summary>
        /// request to receive the current knob range.  the Range event will fire after this call
        /// </summary>
        public async Task<KnobRangeResponse> GetRangeAsync()
        {
            return await _runner.ExecuteAsync(new AsyncCommandFirstResponse<KnobRangeResponse>(new KnobRangeCommand()));
        }

        void RangeResponse(KnobRangeResponse status)
        {
            //fire the event
            Range?.Invoke(_arm, new DataEventArg<KnobRangeResponse>(status));
        }

        void PositionResponse(KnobPositionResponse status)
        {
            //fire the event
            Position?.Invoke(_arm, new DataEventArg<KnobPositionResponse>(status));
        }

        void PositionChangedResponse(KnobPositionChangedResponse status)
        {
            //fire the event
            PositionChanged?.Invoke(_arm, new DataEventArg<KnobPositionChangedResponse>(status));
        }

    }

}
