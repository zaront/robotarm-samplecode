using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using zArm.Api;
using zArm.Api.Specialized;
using zArm.Api.Upload;

namespace Mimic.ViewModel
{
    class FirmwareVM : BaseVM, IModule
    {
        public static readonly Version RequiredFirmware = Version.Parse(Properties.Settings.Default.RequiredFirmware);

        Uploader _uploader;
        public ObservableStringWriter Log { get; }
        PropertyObserver<ConnectionVM> _connectionEvents;
        string _flashActionText;
        string _flashInstructions;
        PortVM _selectedPort;
        public FirmwareModelVM[] FirmwareModels { get; }
        FirmwareModelVM _selectedFirmwareModel;
        FirmwareImageVM _selectedFirmwareImage;
        public RelayCommand FlashCommand { get; }
        public ObservableCollection<PortVM> Ports { get; }
        public ICommand SelectFileImageCommand { get; }
        Visibility _progressVisibility = Visibility.Hidden;
        int _progressMax;
        int _progressValue;
        string _progressMessage;
        bool _uploadSuccessful;
        bool _progressRunning;
        string _currentFirmware;
        string _modelDescription;
        bool _flashRequired;
		bool _armResetNeeded;

		public FirmwareVM()
        {
            //set fields
            Log = new ObservableStringWriter();
            _uploader = new Uploader();
            _uploader.Out = Log;
            _uploader.Status += Uploader_Status;
            var connection = App.Instance.Connection;
            FlashCommand = new RelayCommandAsync(Flash);
            SelectFileImageCommand = new RelayCommandAsync(SelectFileImage);
            Ports = connection.Ports;
            var storage = App.Instance.Storage;
			FirmwareModels = (from f in storage.Firmware
                              group f by f.Model into g
                              orderby g.Key descending
                              select new FirmwareModelVM()
                              {
                                  Model = g.Key,
                                  Images = g.Select(i => new FirmwareImageVM(i)
                                  {
                                      Description = $"{i.Model}  v{i.Version}  released {i.ReleaseDate.ToShortDateString()}"
                                  }).OrderByDescending(i=>i.Firmware.Version).ToArray()
                              }
                              ).ToArray();
            //select the most recent
            SelectedFirmwareModel = FirmwareModels.First();
            SelectedFirmwareImage = SelectedFirmwareModel.Images.First();

            //connect to events
            _connectionEvents = new PropertyObserver<ConnectionVM>(connection)
                .RegisterHandler(i => i.CurrentArm, CurrentRobotChanged)
                .RegisterHandler(i => i.Ports, CurrentRobotChanged);
                
            CurrentRobotChanged(connection);
        }

		public void ShowingModule()
		{
		}

		public void HidingModule()
		{
			//reset arm settings
			if (_armResetNeeded)
			{
				_armResetNeeded = false;
				//await Task.Delay(2000); //wait 2 sec
				//App.Instance.Connection.ArmListener.ResetCurrentArm();
			}
		}

		public Visibility ProgressVisibility
        {
            get { return _progressVisibility; }
            set { _progressVisibility = value; FirePropertyChanged(); }
        }

        public int ProgressMax
        {
            get { return _progressMax; }
            set { _progressMax = value; FirePropertyChanged(); }
        }

        public int ProgressValue
        {
            get { return _progressValue; }
            set { _progressValue = value; FirePropertyChanged(); }
        }

        public string ProgressMessage
        {
            get { return _progressMessage; }
            set { _progressMessage = value; FirePropertyChanged(); }
        }

        public bool ProgressRunning
        {
            get { return _progressRunning; }
            set { _progressRunning = value; FirePropertyChanged(); }
        }

        public string CurrentFirmware
        {
            get { return _currentFirmware; }
            set { _currentFirmware = value; FirePropertyChanged(); }
        }

        public string ModelDescription
        {
            get { return _modelDescription; }
            set { _modelDescription = value; FirePropertyChanged(); }
        }

        void UpdateFlashButton()
        {
            //enable flashing
            if (_selectedPort != null && _selectedFirmwareImage != null)
            {
                FlashCommand.Enabled = true;
                FlashActionText = "Install Firmware";
            }
            else
                FlashCommand.Enabled = false;
        }

        public FirmwareImageVM SelectedFirmwareImage
        {
            get { return _selectedFirmwareImage; }
            set { _selectedFirmwareImage = value;  FirePropertyChanged(); UpdateFlashButton(); }
        }

        public FirmwareModelVM SelectedFirmwareModel
        {
            get { return _selectedFirmwareModel; }
            set
            {
                _selectedFirmwareModel = value;
                FirePropertyChanged();
                //select lattest firmware image by default
                SelectedFirmwareImage = _selectedFirmwareModel.Images.FirstOrDefault();
                UpdateFlashButton();

                //set model description
                if (_selectedFirmwareModel == null)
                    ModelDescription = null;
                else if (_selectedFirmwareModel.Model == "zArmA1")
                    ModelDescription = "this arm has a button and single color LED";
                else if (_selectedFirmwareModel.Model == "zArmB1")
                    ModelDescription = "this arm has a knob and multicolor light";
            }
        }

        public string FlashActionText
        {
            get { return _flashActionText; }
            set { _flashActionText = value; FirePropertyChanged(); }
        }

        public string FlashInstructions
        {
            get { return _flashInstructions; }
            set { _flashInstructions = value; FirePropertyChanged(); }
        }

        public bool FlashRequired
        {
            get { return _flashRequired; }
            set { _flashRequired = value; FirePropertyChanged(); }
        }

        public PortVM SelectedPort
        {
            get { return _selectedPort; }
            set { _selectedPort = value; FirePropertyChanged(); UpdateFlashButton(); }
        }

        void CurrentRobotChanged(ConnectionVM sender)
        {
            FlashRequired = false;
            if (sender.CurrentArm != null)
            {
                CurrentFirmware = $"v{sender.CurrentArm.Settings.Version}";

                //select current robots port
                var selectedPort = Ports.FirstOrDefault(i => i.Port == sender.CurrentArm.Communication.ConnectedPort);
                if (selectedPort != null)
                    SelectedPort = selectedPort;

                if (sender.CurrentArm.Settings.Version < RequiredFirmware)
                {
                    FlashInstructions = $"An upgrade to you robots firmware is required in order to use this software.  Your robot must be updated to a firmware version of v{RequiredFirmware} or beyond";
                    FlashCommand.Enabled = true;
                    FlashActionText = "Upgrade Firmware";
                    FlashRequired = true;
                }
                else
                {
                    //select current robots model
                    var model = sender.CurrentArm.Settings.ModelNumber;
                    var robotModel = FirmwareModels.FirstOrDefault(i => i.Model == model);
                    if (robotModel != null)
                        SelectedFirmwareModel = robotModel;

                    var newerFirmwareAvailible = sender.CurrentArm.Settings.Version < SelectedFirmwareImage.Firmware.Version;

                    if (newerFirmwareAvailible)
                    {
                        FlashInstructions = "A new firmware version for your robot is availible";
                        FlashCommand.Enabled = true;
                        FlashActionText = "Upgrade Firmware";
                    }
                    else
                    {
                        FlashInstructions = "Your robot is up to date";
                        FlashCommand.Enabled = false;
                        FlashActionText = "Upgrade Firmware";
                    }
                }
            }
            else
            {
                CurrentFirmware = null;

                //select best guess port
                var selectedPort = Ports.FirstOrDefault(i => i.PossibleArm);
                if (selectedPort != null)
                    SelectedPort = selectedPort;

                if (Ports.Count == 0)
                {
                    FlashInstructions = "Please connect your robot to your PC by a USB cable";
                    FlashCommand.Enabled = false;
                    FlashActionText = "Install Firmware";
                }
                else
                {
                    FlashInstructions = "No robot recognized.  Install firmware onto your robot.";
                    FlashCommand.Enabled = true;
                    FlashActionText = "Install Firmware";
                }
            }
        }

        async Task Flash()
        {
            //validate
            if (SelectedPort == null || SelectedFirmwareImage == null)
                return;

            //prompt
            var result = await App.Instance.ShowMessageAsync(FlashActionText, $"Continue with {FlashActionText.ToLower()} of \"{SelectedFirmwareImage}\" to {SelectedPort.Port}?", MahApps.Metro.Controls.Dialogs.MessageDialogStyle.AffirmativeAndNegative);
            if (result != MahApps.Metro.Controls.Dialogs.MessageDialogResult.Affirmative)
                return;

			//perform flash
			var filePath = SelectedFirmwareImage.Firmware.GetHexFile();
			await Flash(SelectedPort.Port, filePath);
			SelectedFirmwareImage.Firmware.DeleteHexFile();
        }

		async Task Flash(string port, string filePath)
		{
			//validate
			if (port == null || !File.Exists(filePath))
				return;

			FlashCommand.Enabled = false;
			Log.Clear();
			ProgressVisibility = Visibility.Hidden;
			ProgressMax = 1000;
			ProgressValue = 0;
			ProgressMessage = null;
			ProgressRunning = true;
			_uploadSuccessful = false;

			//close connection
			var arm = App.Instance.Connection.CurrentArm;
			var connected = (arm != null && arm.Communication.IsConnected && arm.Communication.ConnectedPort == SelectedPort.Port);
			if (connected)
				arm.Communication.Disconnect();

			//upload image
			await Task.Run(() =>
			{
				try
				{
					_uploader.Upload(filePath, port);
				}
				catch
				{ }
			});

			if (!_uploadSuccessful)
				ProgressMessage = "ERROR Uploading Firmware";
			else
			{
				await Task.Delay(2000); //if flashed, wait 2 seconds for arduino to restart

				//re-scan ports
				App.Instance.Connection.Rescan();
				_armResetNeeded = true;

			}

			ProgressRunning = false;
			FlashCommand.Enabled = true;
		}

		private void Uploader_Status(object sender, UploaderStatusEventArgs e)
        {
            if (SwitchToMainThread(sender,e)) return;

            if (ProgressVisibility != Visibility.Visible)
                ProgressVisibility = Visibility.Visible;
            if (e.Successful)
            {
                ProgressMessage = "Firmware Upload Completed";
                _uploadSuccessful = true;
            }
            if (e.Error != null)
                ProgressMessage = e.Error;
            if (e.Total > 0)
            {
                if (ProgressMax != e.Total)
                    ProgressMax = e.Total;
                if (ProgressValue != e.Written)
                    ProgressValue = e.Written;
            }

        }

        async Task SelectFileImage()
        {
			//select file
			OpenFileDialog openFile = new OpenFileDialog();
			openFile.Filter = "Hex files (*.hex)|*.hex|All files (*.*)|*.*";
			if (openFile.ShowDialog() != DialogResult.OK)
				return;

			//perform flash
			if (SelectedPort != null)
				await Flash(SelectedPort.Port, openFile.FileName);
		}



        public class ObservableStringWriter : StringWriter, INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            public override void Write(string value)
            {
                base.Write(value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Text"));
            }

            public string Text
            {
                get { return ToString(); }
            }

            public void Clear()
            {
                Flush();
                GetStringBuilder().Clear();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Text"));
            }
        }


    }



    class FirmwareModelVM : BaseVM
    {
        public string Model { get; set; }
        public FirmwareImageVM[] Images { get; set; }
        public override string ToString()
        {
            return Model;
        }
    }

    class FirmwareImageVM : BaseVM
    {
        public IFirmware Firmware { get; }
        public string Description { get; set; }
        public FirmwareImageVM(IFirmware firmware)
        {
            Firmware = firmware;
        }
        public override string ToString()
        {
            return Description;
        }
    }
}
