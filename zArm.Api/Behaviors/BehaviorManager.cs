using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zArm.Api;

namespace zArm.Api.Behaviors
{
    public class BehaviorManager
    {
        Arm _arm;
        ConcurrentDictionary<Type, IBehavior> _behaviors = new ConcurrentDictionary<Type, IBehavior>();
        ConcurrentQueue<IWorkflow> _workflows = new ConcurrentQueue<IWorkflow>();
        IBehavior[] _suspended;

        public BehaviorManager(Arm arm)
        {
            //set fields
            _arm = arm;
        }

        public void Clear()
        {
            foreach (var value in _behaviors.Values.ToArray())
                Remove(value);
        }

        public bool SuspendAll
        {
            get { return _suspended != null; }
            set
            {
                if (value == SuspendAll)
                    return;

                //suspend all
                if (value)
                {
                    _suspended = _behaviors.Values.Where(i=>!(i is IWorkflow)).ToArray(); //don't suspend workflows
                    Clear();
                }

                //restore all
                else
                {
                    Clear();
                    foreach (var behavior in _suspended)
                        Add(behavior);
                }
            }
        }

        public IBehavior Add(IBehavior behavior)
        {
            //validate
            if (behavior == null)
                return behavior;

            //add or replace
            _behaviors.AddOrUpdate(behavior.GetType(), behavior, (i, e) => { RemoveDisable(e); return behavior; });

            //enable
            behavior.Enable(_arm);

            //add workflow
            AddWorkflow(behavior as IWorkflow);

            return behavior;
        }

        public T Add<T>()
            where T : class, IBehavior, new()
        {
            return (T)Add(new T());
        }

        public T GetOrAdd<T>()
            where T : class, IBehavior, new()
        {
            return Get<T>() ?? Add<T>();
        }

        public T GetOrAdd<T>(Func<T> factory)
            where T : class, IBehavior
        {
            var result = Get<T>();
            if (result == null)
            {
                result = factory();
                Add(result);
            }
            return result;
        }

        public IBehavior Remove(IBehavior behavior)
        {
            //validate
            if (behavior == null)
                return null;

            IBehavior storedBehavior = null;
            if (_behaviors.TryGetValue(behavior.GetType(), out storedBehavior))
            {
                //remove only the same instance
                if (storedBehavior != behavior)
                    return null;

                //remove
                _behaviors.TryRemove(behavior.GetType(), out storedBehavior);

                //disable
                RemoveDisable(behavior);
            }

            return storedBehavior;
        }

        public T Remove<T>()
            where T : class, IBehavior
        {
            return Remove(Get<T>()) as T;
        }

        void RemoveDisable(IBehavior behavior)
        {
            //disable
            behavior?.Disable();

            //stop workflow if its currently active
            IWorkflow current = null;
            if (!_workflows.TryPeek(out current))
                return;
            if (current == behavior)
                RemoveWorkflow();
        }

        public IBehavior Get(Type behaviorType)
        {
            IBehavior behavior = null;
            if (_behaviors.TryGetValue(behaviorType, out behavior))
                return behavior;
            return null;
        }

        public T Get<T>()
            where T : class, IBehavior
        {
            return Get(typeof(T)) as T;
        }

        void AddWorkflow(IWorkflow workflow)
        {
            //validate
            if (workflow == null)
                return;

            //start first workflow
            if (_workflows.IsEmpty)
                workflow.StartWorkflow(new WorkflowManager(() => Remove(workflow as IBehavior)));

            //add it to the queue
            _workflows.Enqueue(workflow);
        }

        void RemoveWorkflow()
        {
            //stop the current workflow
            IWorkflow current = null;
            if (!_workflows.TryDequeue(out current))
                return;
            current.StopWorkflow();

            //remove old workflows no longer active
            var pruning = true;
            while (pruning)
            {
                if (!_workflows.TryPeek(out current))
                    return;
                if (current != Get(current.GetType()))
                    _workflows.TryDequeue(out current);
                else
                    pruning = false;
            }

            //start the next workflow
            if (!_workflows.TryPeek(out current))
                return;
            current.StartWorkflow(new WorkflowManager(()=> Remove(current as IBehavior)));
        }

        public IWorkflow CurrentWorkflow
        {
            get
            {
                IWorkflow current = null;
                _workflows.TryPeek(out current);
                return current;
            }
        }
    }
}
