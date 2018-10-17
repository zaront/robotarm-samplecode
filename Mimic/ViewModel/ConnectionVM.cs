using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using zArm.Api;

namespace Mimic.ViewModel
{
    class ConnectionVM : BaseVM
    {
        string _status;
        string _statusDetail;
        string _currentStatusDetail;
        PortScannedEventArgs _bestScannedPort;
        string _statusMenu;
        Brush _statusColor;
        Brush _statusMenuColor;
        bool _scanning;
        bool _isConnected;
		bool _isScanning;
        DateTime _lastComError;
        bool _showingError;
        bool _statusFlashing;
        public ArmListener ArmListener { get; }
        public ObservableCollection<PortVM> Ports { get; } = new ObservableCollection<PortVM>();
        public ICommand StopCommand { get; }
        public ICommand DisconnectCommand { get; }
        public ICommand RenameCommand { get; }
        public ICommand ConnectCommand { get; }
        public ICommand RescanCommand { get; }
		public ICommand ResetCommand { get; }

		public ConnectionVM(ArmListener armListener)
        {
            //set fields
            ArmListener = armListener;
            StopCommand = new RelayCommand(Stop);
            DisconnectCommand = new RelayCommand(Disconnect);
            RenameCommand = new RelayCommandAsync(Rename);
            ConnectCommand = new RelayCommand(Connect);
            RescanCommand = new RelayCommand(Rescan);
			ResetCommand = new RelayCommand(Reset);

			//connect to events
			ArmListener.CurrentArmChanged += CurrentArmChanged;
            ArmListener.ScanningChanges += ScanningChanges;
            ArmListener.PortScanned += PortScanned;
            (Ports as INotifyCollectionChanged).CollectionChanged += Port_CollectionChanged;

            RefreshStatus();
        }

        void Port_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            //this will enabled changes to the port collection to be recognized as a port change
            FirePropertyChanged("Ports");
        }

        void Stop()
        {
            if (CurrentArm != null)
                CurrentArm.Servos.Off();
        }

		void Reset()
		{
			if (CurrentArm != null)
				CurrentArm.SoftReset();
		}

        void Disconnect()
        {
            if (CurrentArm != null)
                CurrentArm.Communication.Disconnect();
        }

        void Connect()
        {
            if (CurrentArm != null)
                CurrentArm.Communication.Connect();
        }

        async Task Rename()
        {
            var newName = await App.Instance.ShowInputAsync("Name Your Robot", "Enter an friendly name for your robot", new MetroDialogSettings() { DefaultText = CurrentArm.Settings.NickName });
            if (newName != null)
            {
                CurrentArm.Settings.NickName = newName;
                RefreshStatus();
            }
        }

        void PortScanned(object sender, PortScannedEventArgs e)
        {
            if (SwitchToMainThread(sender, e)) return;

            //update the Ports collection
            var port = Ports.FirstOrDefault(i => i.Port == e.Port);
            if (port != null)
                Ports.Remove(port);
            if (!e.IsRemoved)
            {
                if (port == null)
                    port = new PortVM() { Port = e.Port, PossibleArm = e.PossibleArm };
                if (e.ErrorMessage != null)
                    port.Message = e.ErrorMessage;
                else if (e.HasArm)
                    port.Message = $"{e.Model}" + (!string.IsNullOrWhiteSpace(e.NickName) ? $"  ({e.NickName})" : string.Empty);
                else if (port.PossibleArm)
                    port.Message = "possible robot needing firmware";

                _bestScannedPort = PickBetterPort(_bestScannedPort, e);

                Ports.Add(port);
            }

            //reset settings
            if (e.NeedsSettingsReset)
            {
                ResetRequired(e);
            }
        }

        PortScannedEventArgs PickBetterPort(PortScannedEventArgs a, PortScannedEventArgs b)
        {
            if (a == null && b == null)
                return a;
            if (a == null)
                return b;
            if (b == null)
                return a;
            if (b.PossibleArm && !a.PossibleArm)
                return b;
            if (b.HasArm && !a.HasArm)
                return b;
            return a;
        }

        async void ResetRequired(PortScannedEventArgs e)
        {
            //confirm
            var result = await App.Instance.ShowMessageAsync("Robot may require a reset to factory defaults", "If you continue to receive this message and if a new firmware was flashed that is incompatible with the robots previous setting, then the connected robot may require its settings to be reset to factory defaults.  Would you like to do this now?", MahApps.Metro.Controls.Dialogs.MessageDialogStyle.AffirmativeAndNegative);
            if (result != MahApps.Metro.Controls.Dialogs.MessageDialogResult.Affirmative)
                return;

            e.Arm.Communication.Connect(e.Port);
            await e.Arm.ResetSettingsAsync();
            e.Arm.Communication.Disconnect();
            Rescan();
        }

        public void Rescan()
        {
            ArmListener.StopListening();
			ArmListener.CurrentArm?.Communication?.Disconnect();
            ArmListener.CurrentArm = null;
            ArmListener.Listen();
        }

        void ScanningChanges(object sender, ScanningChangesEventArgs e)
        {
            if (SwitchToMainThread(sender, e)) return;

            _scanning = e.Scanning;

            //set status detail message
            if (!e.Scanning)
            {
                if (_bestScannedPort != null)
                {
                    if (_bestScannedPort.ErrorMessage != null)
                        _currentStatusDetail = _bestScannedPort.ErrorMessage;
                    else if (!_bestScannedPort.PossibleArm)
                        _currentStatusDetail = "no robot found";
                    else if (!_bestScannedPort.HasArm)
                        _currentStatusDetail = "possible robot needing firmware on " + _bestScannedPort.Port;
                }
            }
            else
            {
                _currentStatusDetail = null;
                _bestScannedPort = null;
            }
            
            RefreshStatus();
        }

        void CurrentArmChanged(object sender, CurrentArmEventArgs e)
        {
            if (SwitchToMainThread(sender, e)) return;

            //disconnect events from prev arm
            if (e.PrevArm != null)
            {
                e.PrevArm.Communication.ConnectionChanged -= Communication_ConnectionChanged;
                e.PrevArm.Communication.CommunicationError -= Communication_CommunicationError;
            }

            //connect to new arm
            if (e.Arm != null)
            {
                e.Arm.Communication.ConnectionChanged += Communication_ConnectionChanged;
                e.Arm.Communication.CommunicationError += Communication_CommunicationError;
            }

            RefreshStatus();

            //trigger that the arm has changed
            CurrentArm = e.Arm;
            IsConnected = e.Arm != null;
        }

        void Communication_CommunicationError(object sender, ComErrorEventArgs e)
        {
            e.Handled = true;

            //skip errors if too frequent
            if (_showingError || DateTime.Now - _lastComError < TimeSpan.FromSeconds(3))
                return;

            if (SwitchToMainThread(sender, e)) return;

            ShowError(e.Error);
            _lastComError = DateTime.Now;
        }

        async void ShowError(CommunicationException error)
        {
            _showingError = true;
            await App.Instance.ShowMessageAsync("Communication Error", error.Message);
            _showingError = false;
        }

        void Communication_ConnectionChanged(object sender, ConnectionChangedEventArgs e)
        {
            if (SwitchToMainThread(sender, e)) return;

            RefreshStatus();
            IsConnected = e.IsConnected;
        }

        public Arm CurrentArm
        {
            get { return ArmListener.CurrentArm; }
            private set { FirePropertyChanged(); }
        }

        void RefreshStatus()
        {

            if (ArmListener.CurrentArm != null && ArmListener.CurrentArm.Communication.IsConnected)
            {
                var armName = ArmListener.CurrentArm.Settings.NickName;
                if (String.IsNullOrWhiteSpace(armName))
                    armName = ArmListener.CurrentArm.Settings.ModelNumber;

                Status = $"Connected to {armName} on {ArmListener.CurrentArm.Communication.ConnectedPort}";
                StatusColor = ModuleColors.Green;
                StatusMenu = "connected - " + armName;
                StatusMenuColor = Brushes.White;
                StatusFlashing = false;
                StatusDetail = null;
				IsScanning = false;
			}
            else if (_scanning)
            {
                Status = "Scanning...";
                StatusColor = ModuleColors.Orange;
                StatusMenu = "new connection - scanning...";
                StatusMenuColor = ModuleColors.GreenBright.Clone();
                StatusFlashing = true;
                StatusDetail = null;
				IsScanning = true;
            }
            else
            {
                Status = "Disconnected";
                StatusColor = Brushes.Black;
                StatusMenu = "no connection";
                StatusMenuColor = Brushes.White;
                StatusFlashing = false;
                StatusDetail = _currentStatusDetail;
				IsScanning = false;
			}
        }

		public string Status
        {
            get { return _status; }
            set { _status = value; FirePropertyChanged(); }
        }

        public string StatusDetail
        {
            get { return _statusDetail; }
            set { _statusDetail = value; FirePropertyChanged(); }
        }

        public string StatusMenu
        {
            get { return _statusMenu; }
            set { _statusMenu = value; FirePropertyChanged(); }
        }

        public Brush StatusColor
        {
            get { return _statusColor; }
            set { _statusColor = value; FirePropertyChanged(); }
        }

        public bool StatusFlashing
        {
            get { return _statusFlashing; }
            set { _statusFlashing = value; FirePropertyChanged(); }
        }

        public Brush StatusMenuColor
        {
            get { return _statusMenuColor; }
            set { _statusMenuColor = value; FirePropertyChanged(); }
        }

        public bool IsConnected
        {
            get { return _isConnected; }
            set { _isConnected = value; FirePropertyChanged(); }
        }

		public bool IsScanning
		{
			get { return _isScanning; }
			set { _isScanning = value; FirePropertyChanged(); }
		}
	}


    class PortVM : BaseVM
    {
        public string Port { get; set; }
        public bool PossibleArm { get; set; }
        public string Message { get; set; }

        public override string ToString()
        {
            if (string.IsNullOrWhiteSpace(Message))
                return Port;
            return $"{Port} - {Message}";
        }
    }
}
