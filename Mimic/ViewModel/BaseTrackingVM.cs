using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Mimic.ViewModel
{
    abstract class BaseTrackingVM : BaseVM, IChangeTracking
    {
        bool _isChanged;

        public event EventHandler HasChanged;

        public bool IsChanged
        {
            get { return _isChanged; }
            protected set
            {
                if (_isChanged != value)
                {
                    _isChanged = value;
                    FirePropertyChanged();
                }

                if (_isChanged)
                    HasChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public virtual void AcceptChanges()
        {
            IsChanged = false;
        }

        protected void FirePropertyChanged(bool isDirty = false, [CallerMemberName] string propertyName = null)
        {
            FirePropertyChanged(propertyName);

            if (isDirty && propertyName != "IsChanged")
                IsChanged = true;
        }
    }

    class ObservableCollectionTracking<T> : ObservableCollection<T>, IChangeTracking
    {
        bool _isChanged;

        public event EventHandler HasChanged;

        public bool IsChanged
        {
            get { return _isChanged; }
            protected set
            {
                if (_isChanged == value)
                    return;
                _isChanged = value;
                OnPropertyChanged(new PropertyChangedEventArgs("IsChanged"));
            }
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnCollectionChanged(e);

            if (e.OldItems != null)
                foreach (var item in e.OldItems.OfType<INotifyPropertyChanged>())
                {
                    item.PropertyChanged -= Item_PropertyChanged;
                    var tracking = item as BaseTrackingVM;
                    if (tracking != null)
                        tracking.HasChanged -= Tracking_HasChanged;
                }
            if (e.NewItems != null)
                foreach (var item in e.NewItems.OfType<INotifyPropertyChanged>())
                {
                    item.PropertyChanged += Item_PropertyChanged;
                    var tracking = item as BaseTrackingVM;
                    if (tracking != null)
                        tracking.HasChanged += Tracking_HasChanged;
                }
        }

        private void Tracking_HasChanged(object sender, EventArgs e)
        {
            HasChanged?.Invoke(this, EventArgs.Empty);
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsChanged")
            {
                var item = sender as IChangeTracking;
                if (item != null && item.IsChanged)
                    IsChanged = true;
            }
        }

        public void AcceptChanges()
        {
            //update all children
            foreach (var item in this.OfType<IChangeTracking>())
                if (item.IsChanged)
                    item.AcceptChanges();

            IsChanged = false;
        }
    }
}
