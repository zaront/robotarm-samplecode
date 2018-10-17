using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Mimic.ViewModel
{
    abstract class BaseVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void FirePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SwitchToMainThread(params object[] allMethodParameters)
        {
            //get to the main thread
            if (App.Current == null)
                return true;
            if (!App.Current.Dispatcher.CheckAccess())
            {
                var method = new StackFrame(1).GetMethod();
                App.Current.Dispatcher.BeginInvoke((Action)(() => method.Invoke(this, allMethodParameters)));
                return true;
            }
            return false;
        }

        protected bool SwitchToMainThread(Action action)
        {
            //get to the main thread
            if (App.Current == null)
                return true;
            if (!App.Current.Dispatcher.CheckAccess())
            {
                App.Current.Dispatcher.BeginInvoke(action);
                return true;
            }
            return false;
        }
    }    
}
