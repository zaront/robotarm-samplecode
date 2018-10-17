using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zArm.Api
{
    /// <summary>
	/// Communicates using serial.
	/// </summary>
	public class Communication : ICommunication
    {
        SerialPort _serialPort;
        string _lastSuccessfulPort;
        readonly object _syncObject = new object();
        string _partialCommandBuffer;
        public event EventHandler<ComDataEventArgs> Received;
        public event EventHandler<ComDataEventArgs> Sent;
        public event EventHandler<ConnectionChangedEventArgs> ConnectionChanged;
        public TextWriter Out { get; set; }

        public Communication()
        {
        }

        public bool IsConnected
        {
            get { return _serialPort != null; }
        }

        public string ConnectedPort
        {
            get { return _serialPort?.PortName; }
        }

        public string[] GetPorts()
        {
            return SerialPort.GetPortNames();
        }

        public void Connect(string port = null)
        {
            bool connected = false;
            lock (_syncObject)
            {
                if (!IsConnected)
                {
                    //get the com port
                    if (port == null)
                    {
                        var ports = GetPorts();
                        if (ports != null && ports.Length != 0)
                            port = ports.Last();  //guess the last port
                        //choose last successful - if exists
                        if (_lastSuccessfulPort != null && ports.Contains(_lastSuccessfulPort))
                            port = _lastSuccessfulPort;
                    }

                    //validate
                    if (port == null)
                        throw new CommunicationException("Port couldn't be found and wasn't supplied");

                    //dump partial command buffer
                    _partialCommandBuffer = null;

                    try
                    {
                        //open com port
                        _serialPort = new SerialPort(port, 115200, Parity.None, 8, StopBits.One); //because its a usb virtual com port, baud rate and such doesn't matter
                        _serialPort.DataReceived += DataReceived;
                        _serialPort.Open();
                        connected = true;
                        _lastSuccessfulPort = port;
                    }
                    catch (Exception ex)
                    {
                        Disconnect(false);
                        throw new CommunicationException(ex.Message);
                    }
                }
            }

            //send event
            if (connected && ConnectionChanged != null)
                ConnectionChanged(this, new ConnectionChangedEventArgs() { IsConnected = true, ConnectedPort = ConnectedPort });
        }

        public void Disconnect()
        {
            Disconnect(true);
        }
        void Disconnect(bool allowEvent)
        {
            bool disconnected = false;
            lock (_syncObject)
            {
                if (IsConnected)
                {
                    //close the connection
                    _serialPort.Close();
                    _serialPort.DataReceived -= DataReceived;
                    _serialPort.Dispose();
                    _serialPort = null;
                    disconnected = true;
                }
            }

            //send event
            if (allowEvent && disconnected && ConnectionChanged != null)
                ConnectionChanged(this, new ConnectionChangedEventArgs() { IsConnected = false });
        }

        public void Send(string data)
        {
            //validate length
            if (data.Length >= 64) //data can't exceed 64.  max length of device buffer (see note on page CmdMessenger)
                throw new CommunicationException("data to send exceeds 64 character");

            lock (_syncObject)
            {
                //validate
                if (!IsConnected)
                    throw new CommunicationException("Connection must be connected before sending data");

                //send data
                try
                {
                    _serialPort.Write(data);
                }
                catch (Exception ex)
                {
                    throw new CommunicationException(ex.Message);
                }
            }

            //send event
            Sent?.Invoke(this, new ComDataEventArgs() { Data = data });

            //send out
            Out?.WriteLine("> " + data);
        }

        private void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            //get data
            var data = _serialPort.ReadExisting();

            //truncate partial commands
            if (_partialCommandBuffer != null)
            {
                data = _partialCommandBuffer + data;
                _partialCommandBuffer = null;
            }
            if (data.Last() != Commands.CommandBuilder.EndChar)
            {
                var lastCommandIndex = data.LastIndexOf(Commands.CommandBuilder.EndChar);
                if (lastCommandIndex == -1)
                {
                    _partialCommandBuffer = data;
                    return;
                }
                _partialCommandBuffer = data.Substring(lastCommandIndex + 1);
                data = data.Substring(0, lastCommandIndex + 1);
            }

            //send event
            Received?.Invoke(this, new ComDataEventArgs() { Data = data });

            //send out
            Out?.WriteLine("< " + data);
        }

        public void Dispose()
        {
            Disconnect(false);
        }
    }





    public class CommunicationException : Exception
    {
        public CommunicationException(string message) : base(message)
        {
        }
    }
}
