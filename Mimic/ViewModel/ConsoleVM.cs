using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using zArm.Api;
using zArm.Api.Commands;

namespace Mimic.ViewModel
{
    class ConsoleVM : BaseVM, IModule
    {
        IDisposable _sendEvent;
        IDisposable _receivedEvent;
        SentHistory _sentHistory = new SentHistory();
        string _sendText;
        ObservableCollection<LogMessage> _log = new ObservableCollection<LogMessage>();
        ICommunication _comm;
        bool _isLogging;
        bool _autoScroll;

        public ICommand SendCommand { get; }
        public ICommand ClearLogCommand { get; }
        public ICommand PrevSentCommand { get; }
        public ICommand NextSentCommand { get; }

        public ConsoleVM()
        {
            //set fields
            _isLogging = true; //start logging by default
            _autoScroll = true; //autoscroll by default
            PrevSentCommand = new RelayCommand<string>(PrevSent);
            NextSentCommand = new RelayCommand<string>(NextSent);
            SendCommand = new RelayCommand<string>(Send);
            ClearLogCommand = new RelayCommand(ClearLog);
        }

        void IModule.ShowingModule()
        {
            if (IsLogging)
                StartLogging();
        }

        void IModule.HidingModule()
        {
            if (IsLogging)
                StopLogging();
        }

        void StartLogging()
        {
            //validate
            var arm = App.Instance.Arm;
            if (arm == null)
                return;

            //set fields
            _comm = arm.Communication;

            //connect to events
            _sendEvent = Observable.FromEventPattern<ComDataEventArgs>(arm.Communication, "Sent")
                .GroupByUntil(x => 1, x => Observable.Timer(TimeSpan.FromSeconds(.05)))
                .SelectMany(x => x.ToArray())
                .Subscribe(Communication_Sent);

            _receivedEvent = Observable.FromEventPattern<ComDataEventArgs>(arm.Communication, "Received")
                .GroupByUntil(x => 1, x => Observable.Timer(TimeSpan.FromSeconds(.2)))
                .SelectMany(x => x.ToArray())
                .Subscribe(Communication_Received);
        }

        void StopLogging()
        {
            //disconnect from events
            if (_sendEvent != null)
            {
                _sendEvent.Dispose();
                _sendEvent = null;
            }
            if (_receivedEvent != null)
            {
                _receivedEvent.Dispose();
                _receivedEvent = null;
            }
        }

        void Communication_Received(IList<EventPattern<ComDataEventArgs>> events)
        {
            var messages = CreateLogMessages(events, false);
            if (messages != null)
                AppendLog(messages);
        }

        void Communication_Sent(IList<EventPattern<ComDataEventArgs>> events)
        {
            var messages = CreateLogMessages(events, true);
            if (messages != null)
                AppendLog(messages);
        }

        IEnumerable<LogMessage> CreateLogMessages(IList<EventPattern<ComDataEventArgs>> events, bool wasSent)
        {
            //skip if logging is off
            if (!_isLogging)
                return null;

            return from e in events
                   from cmd in e.EventArgs.Data.Split(CommandBuilder.EndChar)
                   where !string.IsNullOrWhiteSpace(cmd)
                   select new LogMessage() { Message = cmd, WasSent = wasSent };
        }

        void AppendLog(IEnumerable<LogMessage> messages)
        {
            if (SwitchToMainThread(messages))
                return;

            foreach (var message in messages)
                Log.Add(message);
        }

        public string SendText
        {
            get { return _sendText; }
            set { _sendText = value; FirePropertyChanged(); }
        }

        public ObservableCollection<LogMessage> Log
        {
            get { return _log; }
            set { _log = value; FirePropertyChanged(); }
        }

        public bool AutoScroll
        {
            get { return _autoScroll; }
            set { _autoScroll = value; FirePropertyChanged(); }
        }

        public bool IsLogging
        {
            get { return _isLogging; }
            set
            {
                //validate
                if (_isLogging == value)
                    return;

                //connect to events
                _isLogging = value;
                if (_isLogging)
                    StartLogging();
                else
                    StopLogging();
                
                FirePropertyChanged();
            }
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

        void Send(string text)
        {
            //ignore blank
            if (string.IsNullOrWhiteSpace(text))
                return;

            //send the command
            var command = text + CommandBuilder.EndChar;
            _comm.Send(command);

            //clear the input
            SendText = string.Empty;

            //log in history
            _sentHistory.Add(text);
        }

        void ClearLog()
        {
            Log.Clear();
        }

    }




    public class LogMessage
    {
        public bool WasSent { get; set; }
        public string Message { get; set; }
    }
}
