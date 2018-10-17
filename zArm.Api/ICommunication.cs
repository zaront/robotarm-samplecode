using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zArm.Api.Commands;

namespace zArm.Api
{
    /// <summary>
	/// Implimentation is required to be thread safe
	/// </summary>
	public interface ICommunication : IDisposable
    {
        string[] GetPorts();
        void Connect(string port = null);
        event EventHandler<ConnectionChangedEventArgs> ConnectionChanged;
        void Disconnect();
        bool IsConnected { get; }
        string ConnectedPort { get; }
        event EventHandler<ComDataEventArgs> Received;
        event EventHandler<ComDataEventArgs> Sent;
        void Send(string data);
        TextWriter Out { get; set; }
        CommType CommType {get;}
        void Send(params Command[] commands);
        event EventHandler<ComResponseEventArgs> ReceivedResponse;
        event EventHandler<ComResponseEventArgs> ReceivingResponse;
        event EventHandler<ComCommandsEventArgs> SentCommands;
        event EventHandler<ComCommandsEventArgs> SendingCommands;
        event EventHandler<ComErrorEventArgs> CommunicationError;
    }

    public enum CommType
    {
        Usb,
        Bluetooth,
        Simulation
    }


    public class ComDataEventArgs : EventArgs
    {
        public string Data { get; set; }
    }

    public class ComResponseEventArgs : EventArgs
    {
        public Response[] Responses { get; set; }
    }

    public class ComCommandsEventArgs : EventArgs
    {
        public Command[] Commands { get; set; }
    }

    public class ConnectionChangedEventArgs : EventArgs
    {
        public bool IsConnected { get; set; }
        public string ConnectedPort { get; set; }
    }
    
    public class ComErrorEventArgs : EventArgs
    {
        public CommunicationException Error { get; set; }
        public bool Handled { get; set; }
    }

    public class CommunicationException : Exception
    {
        public CommunicationException(string message) : base(message)
        {
        }
    }
}
