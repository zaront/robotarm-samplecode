using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using zArm.Api;
using zArm.Api.Commands;
using zArm.Api.Specialized;

namespace Mimic.ViewModel
{
    class RobotSettingsVM : BaseVM, IModule
    {
        ObservableCollection<SettingValue> _settings;
		bool _armResetNeeded;
		EntityInfo _selectedBackupFile;
		EntityInfo[] _backupFiles;


		public ICommand ResetCommand { get; }
		public ICommand BackupCommand { get; }
		public RelayCommand RestoreCommand { get; }
		public RelayCommand DeleteCommand { get; }

		public RobotSettingsVM()
        {
			//set fields
			ResetCommand = new RelayCommandAsync(Reset);
			BackupCommand = new RelayCommandAsync(Backup);
			RestoreCommand = new RelayCommandAsync(Restore) { Enabled = false };
			DeleteCommand = new RelayCommand(Delete) { Enabled = false };

		}

		public void ShowingModule()
        {
            RefreshSettings();
			RefreshBackupFiles();
		}

        public void HidingModule()
        {
			//reset arm settings
			if (_armResetNeeded)
			{
				_armResetNeeded = false;
				App.Instance.Connection.ArmListener.ResetCurrentArm();
			}
        }

        async Task Reset()
        {
            //confirm
            var result = await App.Instance.ShowMessageAsync("Reset to factory defaults", "Are you sure you want to reset your robot setting to factory defaults?", MahApps.Metro.Controls.Dialogs.MessageDialogStyle.AffirmativeAndNegative);
            if (result != MahApps.Metro.Controls.Dialogs.MessageDialogResult.Affirmative)
                return;

            await App.Instance.Arm.ResetSettingsAsync();
			_armResetNeeded = true;
			RefreshSettings();
        }

        void RefreshSettings()
        {
            var settingValues = App.Instance.Arm.Settings.GetWriteCommands().Select(i => new SettingValue(i, () => _armResetNeeded = true));
            Settings = new ObservableCollection<SettingValue>(settingValues);
        }

        public ObservableCollection<SettingValue> Settings
        {
            get { return _settings; }
            set { _settings = value; FirePropertyChanged(); }
        }

		async Task Backup()
		{
			//save settings
			var settings = App.Instance.Arm.Settings;
			var name = string.IsNullOrWhiteSpace(settings.NickName) ? settings.ModelNumber : settings.NickName;
			var fileName = $"{DateTime.Now.ToString("yyyy-MM-dd h.mm.ss tt")} [{name}] v{settings.FirmwareVersion}";
			App.Instance.Storage.BackupSettings.Save(settings, new EntityInfo() { Name = fileName });
			RefreshBackupFiles();

			//show message
			await App.Instance.ShowMessageAsync("Backed up", $"You robots setting have been backed up to \"{ fileName}\"");
		}

		async Task Restore()
		{
			//validate
			if (SelectedBackupFile == null)
				return;

			//confirm
			var result = await App.Instance.ShowMessageAsync("Restore all robot settings", $"Are you sure you want to replace your robot setting with \"{SelectedBackupFile.Name}\"?", MahApps.Metro.Controls.Dialogs.MessageDialogStyle.AffirmativeAndNegative);
			if (result != MahApps.Metro.Controls.Dialogs.MessageDialogResult.Affirmative)
				return;

			//set all settings
			var settings = App.Instance.Storage.BackupSettings.Get(SelectedBackupFile);

			App.Instance.Arm.SetSettings(settings);
			_armResetNeeded = true;
			RefreshSettings();
		}

		void Delete()
		{
			//validate
			if (SelectedBackupFile == null)
				return;

			//delete the file
			App.Instance.Storage.BackupSettings.Delete(SelectedBackupFile);
			RefreshBackupFiles();
		}

		public EntityInfo SelectedBackupFile
		{
			get { return _selectedBackupFile; }
			set
			{
				_selectedBackupFile = value;
				FirePropertyChanged();

				//update UI
				RestoreCommand.Enabled = _selectedBackupFile != null;
				DeleteCommand.Enabled = _selectedBackupFile != null;
			}
		}

		public EntityInfo[] BackupFiles
		{
			get { return _backupFiles; }
			set { _backupFiles = value; FirePropertyChanged(); }
		}

		void RefreshBackupFiles()
		{
			BackupFiles = App.Instance.Storage.BackupSettings.GetAll();
		}

		public class SettingValue : BaseErrorVM
        {
            int _settingID;
            Type _dataType;
            string _value;
			Action _onChanged;


			public string Name { get; }

            public SettingValue (SettingWriteCommand setting, Action onChanged = null)
            {
				//set fields
				_onChanged = onChanged;
                _settingID = setting.SettingID;
                var settingID = (SettingsID)setting.SettingID;
                //get type
                _dataType = (settingID.GetType().GetMember(settingID.ToString())[0].GetCustomAttributes(typeof(DataTypeAttribute), false)[0] as DataTypeAttribute).Type;

                Name = settingID.ToString();
                _value = setting.Value;
            }

            public string Value
            {
                get { return _value; }
                set
                {
                    //set the value
                    _value = value;
                    FirePropertyChanged();

                    //set an error if not the right format
                    try
                    {
                        var convertedValue = ParamerterConverter.ChangeType(value, _dataType);
                        if (convertedValue == null && !string.IsNullOrWhiteSpace(value))
                            SetError(GetErrorMessage());
                        else
                            ClearError();
                    }
                    catch
                    {
                        SetError(GetErrorMessage());
                    }

                    //update the property
                    if (!HasErrors)
                    {
                        var settings = new Settings();
                        settings.Set(new SettingReadResponse() { SettingID = _settingID, Value = _value });
                        App.Instance.Arm.SetSettings(settings);
						_onChanged?.Invoke();
                    }
                }
            }

            string GetErrorMessage()
            {
                if (_dataType == typeof(int?))
                    return "must be a valid number";
                if (_dataType == typeof(float?))
                    return "must be a valid decimal number";
                return "invalid format";
            }
        }

    }
}
