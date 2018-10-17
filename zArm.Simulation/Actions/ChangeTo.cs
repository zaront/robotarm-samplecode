using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Urho;
using Urho.Actions;

namespace zArm.Simulation.Actions
{
    public class ChangeTo<T> : FiniteTimeAction
    {
        public T EndPosition;
        public Func<BaseAction, Node, T> CurrentPosition;
        public Action<BaseAction, Node, T, T, float> Change;

        public ChangeTo(float duration, T endPosition, Func<BaseAction, Node, T> currentPosition, Action<BaseAction, Node, T,T,float> change) : base(duration)
        {
            EndPosition = endPosition;
            CurrentPosition = currentPosition;
            Change = change;
        }

        protected override ActionState StartAction(Node target)
        {
            return new ChangeToState<T>(this, target);
        }

        public override FiniteTimeAction Reverse()
        {
            return new ChangeTo<T>(Duration, EndPosition, CurrentPosition, Change);
        }
    }


    public class ChangeToState<T> : FiniteTimeActionState
    {
        protected new ChangeTo<T> Action;
        protected T StartPosition;
        float _totalTime;

        public ChangeToState(ChangeTo<T> action, Node target) : base(action, target)
		{
            Action = action;
            StartPosition = Action.CurrentPosition(Action, Target);
        }

        public override void Update(float time)
        {
            if (Target == null)
                return;

            Action.Change(Action, Target, StartPosition, Action.EndPosition, time);
        }
    }
}
