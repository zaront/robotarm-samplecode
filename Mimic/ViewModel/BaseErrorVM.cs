using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Mimic.ViewModel
{
    abstract class BaseErrorVM : BaseVM, INotifyDataErrorInfo
    {
        Dictionary<string, string[]> _errors = new Dictionary<string, string[]>();

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        protected void ClearError([CallerMemberName] string propertyName = null)
        {
            if (_errors.ContainsKey(propertyName))
            {
                _errors.Remove(propertyName);
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            }
        }

        protected void SetError(string[] error = null, [CallerMemberName] string propertyName = null)
        {
            if (error == null)
            {
                ClearError(propertyName);
                return;
            }

            if (_errors.ContainsKey(propertyName))
            {
                error = error.Concat(_errors[propertyName]).ToArray();
                _errors[propertyName] = error;
            }
            else
                _errors.Add(propertyName, error);
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        protected void SetError(string error, [CallerMemberName] string propertyName = null)
        {
            SetError(new string[] { error }, propertyName);
        }

        public bool HasErrors
        {
            get { return _errors.Count != 0; }
        }

        public IEnumerable GetErrors(string propertyName)
        {
            if (_errors.ContainsKey(propertyName))
                return _errors[propertyName];
            return null;
        }
    }
}
