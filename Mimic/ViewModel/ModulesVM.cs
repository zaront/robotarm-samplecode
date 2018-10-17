using MahApps.Metro.Controls;
using Mimic.Converters;
using Mimic.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Mimic.ViewModel
{
    class ModulesVM : BaseVM, IModule
	{
        ModuleVM _selectedModule;
        TransitionType _selectedModuleTransition;
        ModuleVM _menu;
        ConnectionVM _connection;
        SettingsVM _settings;
        PropertyObserver<ConnectionVM> _connectionEvents;
        PropertyObserver<SettingsVM> _settingsEvents;
		int _moduleColumns;
		bool _isModuleShowing;
		MessageDialog _scanningDialog;

        public ObservableCollection<ModuleVM> Modules { get; }
        public ObservableCollection<ObservableCollection<ModuleVM>> ModuleColumns { get; }
        public ICommand ModuleSizeChangedCommand { get; }

        public ModulesVM(ConnectionVM connection, SettingsVM settings, ModuleVM menu, params ModuleVM[] modules)
        {
            //set fields
            _connection = connection;
            _settings = settings;
            _menu = menu;
            Modules = new ObservableCollection<ModuleVM>(modules);
            ModuleColumns = new ObservableCollection<ObservableCollection<ModuleVM>>();
            ModuleSizeChangedCommand = new RelayCommand<SizeChangedEventArgs>(ModuleSizeChanged);

			//connect to events
			_connectionEvents = new PropertyObserver<ConnectionVM>(_connection)
				.RegisterHandler(i => i.CurrentArm, CurrentRobotChanged)
				.RegisterHandler(i => i.Ports, PortsChanged)
				.RegisterHandler(i => i.IsScanning, ScanningChanged);
            _settingsEvents = new PropertyObserver<SettingsVM>(_settings)
                .RegisterHandler(i => i.FirstTimeUse, FirstTimeUseChanged);
            settings.HasReset += Settings_HasReset;
            foreach (var module in modules)
                module.Selected += Module_Selected;
            App.Current.Exit += Current_Exit;

            UpdateRobotChanged();
            GotoMenu();
        }

		void IModule.ShowingModule()
		{
			_isModuleShowing = true;
		}

		void IModule.HidingModule()
		{
			_isModuleShowing = false;
			ScanningChanged(null);
		}

		private void Current_Exit(object sender, ExitEventArgs e)
        {
            UnloadControl(_selectedModule);
        }

        void ModuleSizeChanged(SizeChangedEventArgs e)
        {
            //determine if columns need to change (based on width)
            var columns = (int)Math.Floor(e.NewSize.Width / 330);
            if (columns == 0)
                columns = 1;
            if (_moduleColumns == columns)
                return;

            //resize columns
            _moduleColumns = columns;
            var columnCount = (int)Math.Ceiling((Modules.Count + Modules.Count(i => i.LargeTile)) / (double)columns);
            ModuleColumns.Clear();
            int indexOffset = 0;
            foreach (var moduleGroup in Modules.Select((x, i) =>
            {
                indexOffset += x.LargeTile ? 1 : 0;
                return new { Index = i + indexOffset, Value = x };
            }).GroupBy(x => x.Index / columnCount).Select(x => x.Select(v => v.Value)))
                ModuleColumns.Add(new ObservableCollection<ModuleVM>(moduleGroup));

        }

        void Module_Selected(ModuleVM module)
        {
            SelectedModule = module;
        }

        private void CurrentRobotChanged(ConnectionVM sender)
        {
            UpdateRobotChanged();
        }

        private void Settings_HasReset(object sender, EventArgs e)
        {
            UpdateAlerts();
        }

        private void PortsChanged(ConnectionVM sender)
        {
            UpdateAlerts();
        }

        private void FirstTimeUseChanged(SettingsVM sender)
        {
            UpdateAlerts();
        }

		private void ScanningChanged(ConnectionVM sender)
		{
			//TODO: work in progress

			////close if module not showing
			//if (!_isModuleShowing && _scanningDialog != null)
			//{
			//	_scanningDialog.Close();
			//	_scanningDialog = null;
			//	return;
			//}

			//if (_isModuleShowing)
			//{
			//	//show if scanning
			//	if (_connection.IsScanning && _scanningDialog == null)
			//	{
			//		_scanningDialog = new MessageDialog() { Title = "new connection detected", Message = "scanning.  please wait..." };
			//		App.Instance.ShowDialog(_scanningDialog).ContinueWith(i => { });
			//	}

			//	//close if not scanning
			//	else if (!_connection.IsScanning && _scanningDialog != null)
			//	{
			//		_scanningDialog.Close();
			//		_scanningDialog = null;
			//	}
			//}
		}

		void UpdateRobotChanged()
        {
            var hasRobot = _connection.CurrentArm != null;
            var isCalibrated = hasRobot && (_connection.CurrentArm.Settings.Calibrated.GetValueOrDefault() > CalibrationVM.CalibrationCompleteStage);

            //update FirstTimeUse flag
            if (hasRobot && _settings.FirstTimeUse)
                _settings.FirstTimeUse = false;

            //update robot required modules
            foreach (var module in Modules)
            {
                if (module.RobotRequired)
                {
                    //disable modules that require a robot
                    module.SelectedCommand.Enabled = hasRobot;
                    if (!hasRobot)
                        module.DisabledReason = "Robot Connection Required";

                    //disable modules that require caliberation
                    if (module.CalibrationRequired && !isCalibrated)
                    {
                        module.SelectedCommand.Enabled = false;
                        if (hasRobot)
                            module.DisabledReason = "Calibration Required";
                    }

                    //unload cached control that require a robot
                    if (!hasRobot)
                        module.Control = null;

                    //go to menu if current module requires a robot
                    if (module == SelectedModule && !hasRobot)
                    {
						GotoMenu();
						if (_connection.ArmListener.IsResettingCurrentArm)
							App.Instance.ShowMessageAsync("Reset connection", "The connection to your robot was reset in order to reload your robots settings.").ContinueWith(i=> UpdateAlerts(true));
						else
							App.Instance.ShowMessageAsync("Lost connection", "Oops, the connection to your robot was lost.").ContinueWith(i => UpdateAlerts(true));
                    }
                }
            }

            //update menu alerts
            UpdateAlerts();
        }

        public void UpdateAlerts(bool reset = false)
        {
            var hasRobot = _connection.CurrentArm != null;
            var hasPossibleRobot = !hasRobot && _connection.Ports.Count(i => i.PossibleArm) != 0;
            var needsUpgrade = hasRobot && _connection.CurrentArm.Settings.Version < FirmwareVM.RequiredFirmware;
            var needsCalibration = hasRobot && !(_connection.CurrentArm.Settings.Calibrated.GetValueOrDefault() > CalibrationVM.CalibrationCompleteStage);
            var firstTime = _settings.FirstTimeUse;

            //set alerts
            SetAlert(typeof(BuildInstructionsControl), firstTime && !hasRobot && !hasPossibleRobot, reset);
            SetAlert(typeof(FirmwareControl), (firstTime && hasPossibleRobot) || (hasRobot && needsUpgrade), reset);
            SetAlert(typeof(CalibrationControl), hasRobot && needsCalibration, reset);
        }

        void SetAlert(Type controlType, bool enableAlert, bool reset)
        {
            var module = Modules.FirstOrDefault(i => i.ControlType == controlType);
            if (module != null)
            {
                if (enableAlert)
                {
                    if (module.Alert != true || reset)
                        module.Alert = true;
                }
                else
                {
					if (module.Alert != false || reset)
					{
						if (reset)
							Task.Run(async () => { await Task.Delay(50); module.Alert = true; await Task.Delay(50); module.Alert = false; });
						else
							module.Alert = false;
					}
				}
            }
        }

        public ModuleVM SelectedModule
        {
            get { return _selectedModule; }
            set
            {
                if (value == null)
                    value = _menu;
                UnloadControl(_selectedModule);
                _selectedModule = value;
                LoadControl(_selectedModule);
                FirePropertyChanged();
            }
        }

        public TransitionType SelectedModuleTransition
        {
            get { return _selectedModuleTransition; }
            set { _selectedModuleTransition = value; FirePropertyChanged(); }
        }

        public void GotoMenu()
        {
            SelectedModule = null;
        }

        void LoadControl(ModuleVM module)
        {
            //construct control
            if (module.Control == null)
            {
                module.Control = Activator.CreateInstance(module.ControlType) as UserControl;
                //attach the view model
                if (module.ViewModelType != null)
                    module.Control.DataContext = Activator.CreateInstance(module.ViewModelType);
            }

            //set transition
            SelectedModuleTransition = module.Transition;

            //call module methods
            if (module.Control != null)
            {
                var mod = module.Control as IModule;
                mod?.ShowingModule();
                mod = module.Control.DataContext as IModule;
                mod?.ShowingModule();
				if (mod == null && module == _menu)
				{
					mod = this as IModule;
					mod?.ShowingModule();
				}
            }
        }

        void UnloadControl(ModuleVM module)
        {
            if (module == null)
                return;

            //call module methods
            if (module.Control != null)
            {
                var mod = module.Control as IModule;
                mod?.HidingModule();
                mod = module.Control.DataContext as IModule;
                mod?.HidingModule();
				if (mod == null && module == _menu)
				{
					mod = this as IModule;
					mod?.HidingModule();
				}
			}
        }
	}

    class ModuleVM : BaseVM
    {
        bool _alert;
        SolidColorBrush _backgroundColor;
        SolidColorBrush _borderColor;
        string _disabledReason;
        public string Name { get; }
        public Type ControlType { get; }
        public Type ViewModelType { get; }
        public UserControl Control { get; set; }
        public TransitionType Transition { get; set; } = TransitionType.Left;
        public bool LargeTile { get; set; }
        public bool RobotRequired { get; set; }
        public bool CalibrationRequired { get; set; }
        public string ImageName { get; set; }
        public string IconName { get; set; }
        public bool DarkText { get; set; }
        public RelayCommand SelectedCommand { get; }

        public event Action<ModuleVM> Selected;

        public ModuleVM(string name, Type controlType, Type viewModelType = null)
        {
            //set fields
            Name = name;
            ControlType = controlType;
            ViewModelType = viewModelType;
            SelectedCommand = new RelayCommand(()=> { Selected?.Invoke(this); });
            BackgroundColor = ModuleColors.Teal;
        }

        public bool Alert
        {
            get { return _alert; }
            set { _alert = value; FirePropertyChanged(); }
        }

        public SolidColorBrush BackgroundColor
        {
            get { return _backgroundColor; }
            set { _backgroundColor = value.CloneCurrentValue(); FirePropertyChanged(); BorderColor = value; }
        }

        public SolidColorBrush BorderColor
        {
            get { return _borderColor; }
            set { _borderColor = value.CloneCurrentValue(); FirePropertyChanged(); }
        }

        public string DisabledReason
        {
            get { return _disabledReason; }
            set { _disabledReason = value; FirePropertyChanged(); }
        }
    }

    public static class ModuleColors
    {
        public static SolidColorBrush Teal = new SolidColorBrush(Color.FromRgb(1,134,155));
        public static SolidColorBrush Blue = new SolidColorBrush(Color.FromRgb(39, 117, 236));
        public static SolidColorBrush Purple = new SolidColorBrush(Color.FromRgb(176, 0, 173));
        public static SolidColorBrush Orange = new SolidColorBrush(Color.FromRgb(216, 89, 48));
        public static SolidColorBrush Green = new SolidColorBrush(Color.FromRgb(0, 141, 0));
        public static SolidColorBrush Yellow = new SolidColorBrush(Color.FromRgb(141, 141, 0));
        public static SolidColorBrush GreenBright = new SolidColorBrush(Color.FromRgb(177, 228, 42));
        public static SolidColorBrush Red = new SolidColorBrush(Color.FromRgb(190, 30, 75));
        public static SolidColorBrush Background = new SolidColorBrush(Color.FromRgb(37, 37, 37));
    }

    interface IModule
    {
        void ShowingModule();
        void HidingModule();
    }
}
