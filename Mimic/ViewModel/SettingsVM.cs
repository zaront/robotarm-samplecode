using Mimic.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Mimic.ViewModel
{
    class SettingsVM : BaseVM
    {
        public ICommand ResetCommand { get; }
        public event EventHandler HasReset;

        public SettingsVM()
        {
            //set fields
            ResetCommand = new RelayCommand(Reset);
        }

        void Reset()
        {
            Settings.Default.Reset();
			//prevent restoring last versions settings file
			Settings.Default.SettingsUpgradeNeeded = false;
			Settings.Default.Save();

			//re-read all properties again
			FirePropertyChanged("FirstTimeUse");
			FirePropertyChanged("ShowChangeLogAfterUpgrade");
			FirePropertyChanged("RunScratchInBackground");

			HasReset?.Invoke(this, EventArgs.Empty);
        }

        public bool FirstTimeUse
        {
            get { return Settings.Default.FirstTimeUse; }
            set
            {
                Settings.Default.FirstTimeUse = value;
                Settings.Default.Save();
                FirePropertyChanged();
            }
        }

		public bool ShowChangeLogAfterUpgrade
		{
			get { return Settings.Default.ShowChangeLogAfterUpgrade; }
			set
			{
				Settings.Default.ShowChangeLogAfterUpgrade = value;
				Settings.Default.Save();
				FirePropertyChanged();
			}
		}

		public bool RunScratchInBackground
		{
			get { return Settings.Default.RunScratchInBackground; }
			set
			{
				Settings.Default.RunScratchInBackground = value;
				Settings.Default.Save();
				FirePropertyChanged();
			}
		}
	}
}
