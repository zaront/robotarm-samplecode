using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Urho;
using Urho.Actions;

namespace zArm.Simulation.Actions
{
    public class ChangeBy<T> : FiniteTimeAction
    {
        public T EndPosition;
        public Func<BaseAction, Node, T> CurrentPosition;
        public Func<T,T,T> AddPosition;
        public Action<BaseAction, Node, T, T, float> Change;

        public ChangeBy(float duration, T endPosition, Func<BaseAction, Node, T> currentPosition, Func<T, T, T> addPosition, Action<BaseAction, Node, T,T,float> change) : base(duration)
        {
            EndPosition = endPosition;
            CurrentPosition = currentPosition;
            AddPosition = addPosition;
            Change = change;
        }

        protected override ActionState StartAction(Node target)
        {
            return new ChangeByState<T>(this, target);
        }

        public override FiniteTimeAction Reverse()
        {
            return new ChangeBy<T>(Duration, EndPosition, CurrentPosition, AddPosition, Change);
        }
    }


    public class ChangeByState<T> : FiniteTimeActionState
    {
        protected new ChangeBy<T> Action;
        protected T StartPosition;
        protected T EndPosition;
        float _totalTime;

        public ChangeByState(ChangeBy<T> action, Node target) : base(action, target)
		{
            Action = action;
            StartPosition = Action.CurrentPosition(Action, Target);
            EndPosition = Action.AddPosition(StartPosition, Action.EndPosition);
        }

        public override void Update(float time)
        {
            if (Target == null)
                return;

            Action.Change(Action, Target, StartPosition, EndPosition, time);
        }
    }
}
