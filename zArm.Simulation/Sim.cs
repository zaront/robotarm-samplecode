using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Urho;

namespace zArm.Simulation
{
    public abstract class Sim
    {
        public App App { get; internal set; }
        public bool IsRunning { get; private set; }

        public event EventHandler HasStarted;
        public event EventHandler HasStopped;

        public virtual Type ApplicationType
        {
            get
            {
                //by default, gets a nested class by naming convention
                var type = GetType();
                var nestedTypeName = type.Name + "App";
                var appType = type.GetNestedType(nestedTypeName, System.Reflection.BindingFlags.NonPublic);
                if (appType == null)
                    throw new Exception($"Couldn't find nested type '{nestedTypeName}' within {type.FullName}");
                return appType;
            }
        }

        internal protected virtual void Stopped()
        {
            IsRunning = false;
            HasStopped?.Invoke(this, EventArgs.Empty);
        }

        internal void OnStarted()
        {
            IsRunning = true;
            Started();
            HasStarted?.Invoke(this, EventArgs.Empty);
        }

        internal protected virtual void Started()
        {
            
        }
    }


    public abstract class Sim<T> : Sim
        where T : App
    {
        public override Type ApplicationType
        {
            get { return typeof(T); }
        }

        new protected T App
        {
            get { return base.App as T; }
        }
    }
}
