using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zArm.Api.Commands
{
    public class MessageHandlerRegistry
    {
        ConcurrentDictionary<Type, Delegate> _registry = new ConcurrentDictionary<Type, Delegate>();

        public void Register<T>(Action<T> callback)
            where T : Message
        {
            _registry.AddOrUpdate(typeof(T), callback, (i, e) => callback);
        }

        public bool HandleMessage(Message message)
        {
            Delegate callback = null;
            if (_registry.TryGetValue(message.GetType(), out callback))
            {
                callback.DynamicInvoke(message);
                return true;
            }
            return false;
        }
    }
}
