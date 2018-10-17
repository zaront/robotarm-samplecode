using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using zArm.Api;
using zArm.Api.Commands;

namespace Mimic.ViewModel
{
    class CommunicationVM : BaseVM
    {
        ICommunication _com;
        string _status;
        Brush _statusColor;
        string _connectText;
        string _sendText;
        bool _autoScroll;
        bool _isLogging;
        bool _suppressPosition;
        Port[] _ports;
        string _selectedPort;
        ObservableCollection<LogItem> _log;
        ConcurrentQueue<LogItem> _logQueue;
        Timer _logTimer;
        SentHistory _sentHistory = new SentHistory();
        public ICommand ToggleConnectCommand { get; }
        public ICommand SendCommand { get; }
        public ICommand ClearLogCommand { get; }
        public ICommand PrevSentCommand { get; }
        public ICommand NextSentCommand { get; }
        bool _filterReceived;

        public CommunicationVM(ICommunication entity)
        {
            //set fields
            _com = entity;
            _com.ConnectionChanged += Comm_ConnectionChanged;
            PrevSentCommand = new DelegateCommand<string>(PrevSent);
            NextSentCommand = new DelegateCommand<string>(NextSent);
            ToggleConnectCommand = new DelegateCommand(ToggleConnect);
            SendCommand = new DelegateCommand<string>(Send);
            ClearLogCommand = new DelegateCommand(ClearLog);
            Log = new ObservableCollection<LogItem>();
            _logQueue = new ConcurrentQueue<LogItem>();
            _logTimer = new Timer(500); //.5 sec
            _logTimer.Elapsed += LogTimer_Elapsed;

            IsLogging = true;
            AutoScroll = true;
            RefreshStatus();
            RefreshPorts();

            //unhandled exception
            System.Windows.Application.Current.DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            //show error in statusbar
            Status = "error: " + e.Exception.Message;
            StatusColor = Brushes.Red;
            e.Handled = true;
        }

        private void Comm_Sent(object sender, ComDataEventArgs e)
        {
            if (!_isLogging)
                return;

            AppendLog(e.Data, LogType.Send);
        }

        private void Comm_Received(object sender, ComDataEventArgs e)
        {
            if (!_isLogging || _filterReceived)
                return;

            AppendLog(e.Data, LogType.Receive);
        }

        private void Comm_ConnectionChanged(object sender, ConnectionChangedEventArgs e)
        {
            if (SwitchToMainThread(sender, e)) return;

            RefreshStatus();
        }

        public void RefreshPorts()
        {
            var portNames = _com.GetPorts();
            var ports = portNames.Select(i => new Port() { Name = i, Value = i }).ToList();
            ports.Insert(0, new Port() { Name = "<auto>" });
            var selectedPort = SelectedPort;
            Ports = ports.ToArray();
            if (portNames.Contains(selectedPort))
                SelectedPort = selectedPort;
            else
                SelectedPort = null;
        }

        public void RefreshStatus()
        {
            if (_com.IsConnected)
            {
                Status = "Connected";
                StatusColor = Brushes.Green;
                ConnectText = "Disconnect";
            }
            else
            {
                Status = "Disconnected";
                StatusColor = Brushes.Black;
                ConnectText = "Connect";
            }
            
        }

        void ToggleConnect()
        {
            if (_com.IsConnected)
                _com.Disconnect();
            else
            {
                try
                {
                    _com.Connect(SelectedPort);
                }
                catch (CommunicationException ex)
                {
                    ErrorConnection(ex);
                }
            }
        }

        void ErrorConnection(CommunicationException ex)
        {
            Status = "Connection error: " + ex.Message;
            StatusColor = Brushes.Red;
        }

        void Send(string text)
        {
            //ignore blank
            if (string.IsNullOrWhiteSpace(text))
                return;

            //send the command
            var command = text + CommandBuilder.EndChar;
            try
            {
                _com.Send(command);
            }
            catch (CommunicationException ex)
            {
                ErrorConnection(ex);
            }

            //clear the input
            SendText = string.Empty;

            //log in history
            _sentHistory.Add(text);
        }

        void PrevSent(string text)
        {
            _sentHistory.MovePrev();
            if (_sentHistory.Current != null)
                SendText = _sentHistory.Current;
        }

        void NextSent(string text)
        {
            _sentHistory.MoveNext();
            if (_sentHistory.Current != null)
                SendText = _sentHistory.Current;
        }

        void AppendLog(string text, LogType logType)
        {
            //skip if logging is off
            if (!IsLogging)
                return;

            var cmds = text.Split(CommandBuilder.EndChar);
            foreach (var cmd in cmds)
            {
                //validate
                if (string.IsNullOrWhiteSpace(cmd))
                    continue;

                var item = new LogItem() { Message = cmd, Type = logType };

                //set color
                switch (logType)
                {
                    case LogType.Receive:
                        item.Color = Brushes.Blue;
                        break;
                    case LogType.Error:
                        item.Color = Brushes.Red;
                        break;
                    default:
                        item.Color = Brushes.Black;
                        break;
                }

                //append to the queue
                _logQueue.Enqueue(item);
            }
        }

        private void LogTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!_logQueue.IsEmpty)
                DumpLogQueue();
        }

        void DumpLogQueue()
        {
            if (SwitchToMainThread()) return;

            //append the text
            LogItem logItem;
            while (_logQueue.TryDequeue(out logItem))
                Log.Add(logItem);

            ////remove an item if too long
            //var remove = Log.Count - 5000;
            //if (remove > 0)
            //    for (int i = 0; i < remove; i++)
            //        Log.RemoveAt(0);

        }

        void ClearLog()
        {
            Log.Clear();
        }

        public string Status
        {
            get { return _status; }
            set { _status = value; FirePropertyChanged(); }
        }

        public Brush StatusColor
        {
            get { return _statusColor; }
            set { _statusColor = value; FirePropertyChanged(); }
        }

        public string ConnectText
        {
            get { return _connectText; }
            set { _connectText = value; FirePropertyChanged(); }
        }

        public string SendText
        {
            get { return _sendText; }
            set { _sendText = value; FirePropertyChanged(); }
        }

        public Port[] Ports
        {
            get { return _ports; }
            set { _ports = value; FirePropertyChanged(); }
        }

        public string SelectedPort
        {
            get { return _selectedPort; }
            set { _selectedPort = value; FirePropertyChanged(); }
        }

        public bool AutoScroll
        {
            get { return _autoScroll; }
            set { _autoScroll = value; FirePropertyChanged(); }
        }

        public bool FilterReceived
        {
            get { return _filterReceived; }
            set { _filterReceived = value; FirePropertyChanged(); }
        }

        public bool IsLogging
        {
            get { return _isLogging; }
            set
            {
                if (_isLogging == value)
                    return;

                _isLogging = value;
                _logTimer.Enabled = _isLogging;
                if (_isLogging)
                {
                    _com.Received += Comm_Received;
                    _com.Sent += Comm_Sent;
                }
                else
                {
                    _com.Received -= Comm_Received;
                    _com.Sent -= Comm_Sent;
                }

                FirePropertyChanged();
            }
        }

        public bool SuppressPosition
        {
            get { return _suppressPosition; }
            set { _suppressPosition = value; FirePropertyChanged(); }
        }

        public ObservableCollection<LogItem> Log
        {
            get { return _log; }
            set { _log = value; FirePropertyChanged(); }
        }
    }



    class Port
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public enum LogType
    {
        Send,
        Receive,
        Error
    }

    public class LogItem
    {
        public LogType Type { get; set; }
        public string Message { get; set; }
        public Brush Color { get; set; }
    }
}
