using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zArm.Api
{
    public interface IConnection : IDisposable
    {
        event EventHandler<ConnectionEventArgs> ConnectionAvailible;
        event EventHandler<ConnectionEventArgs> ConnectionUnavailible;
        bool Listen { get; set; }
    }


    public class ConnectionEventArgs : EventArgs
    {
        public ICommunication Communication { get; set; }
        public string Port { get; set; }
        public bool ExistingDevice { get; set; }
        public bool PossibleArm { get; set; }
    }
}
