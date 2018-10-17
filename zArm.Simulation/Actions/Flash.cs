using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Urho;
using Urho.Actions;
using zArm.Simulation.Components;

namespace zArm.Simulation.Actions
{
    public class Flash : FiniteTimeAction
    {
        public uint Times { get; }
        Action<BaseAction,Node, bool> _flashAction;

        public Flash(float duration, uint numOfFlashes, Action<BaseAction, Node, bool> flashAction) : base(duration)
        {
            Times = numOfFlashes;
            _flashAction = flashAction;
        }

        protected override ActionState StartAction(Node target)
        {
            return new FlashState(this, target, _flashAction);
        }

        public override FiniteTimeAction Reverse()
        {
            return new Flash(Duration, Times, _flashAction);
        }
    }


	public class FlashState : FiniteTimeActionState
    {
        protected uint Times { get; set; }
        Action<BaseAction, Node, bool> _flashAction;

        public FlashState(Flash action, Node target, Action<BaseAction, Node, bool> flashAction) : base(action, target)
        {
            Times = action.Times;
            _flashAction = flashAction;
        }

        public override void Update(float time)
        {
            if (Target != null && !IsDone)
            {
                float num = 1f / this.Times;
                float num2 = time % num;
                _flashAction(Action, Target, (num2 > num / 2f));
            }
        }

        protected override void Stop()
        {
            _flashAction(Action, Target, false);

            base.Stop();
        }
    }
}
