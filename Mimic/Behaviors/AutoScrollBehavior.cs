using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interactivity;

namespace Mimic.Behaviors
{
    public class AutoScrollBehavior : Behavior<ListBox>
    {
        public static readonly DependencyProperty EnabledProperty = DependencyProperty.Register("Enabled", typeof(bool), typeof(AutoScrollBehavior), new PropertyMetadata(true));
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register("ItemsSource", typeof(object), typeof(AutoScrollBehavior), new PropertyMetadata(OnItemsSource));

        IDisposable _collectionChangedEvent;

        static void OnItemsSource(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var me = source as AutoScrollBehavior;

            //disconnect from last list
            if (me._collectionChangedEvent != null)
            {
                me._collectionChangedEvent.Dispose();
                me._collectionChangedEvent = null;
            }

            //connect to new list events
            var collection = e.NewValue as INotifyCollectionChanged;
            if (collection != null)
            {
                me._collectionChangedEvent = Observable.FromEventPattern<NotifyCollectionChangedEventArgs>(collection, "CollectionChanged")
                    .Sample(TimeSpan.FromSeconds(.2))
                    .Subscribe(i => App.Current.Dispatcher.BeginInvoke((Action)(() => me.CollectionChanged(i.Sender, i.EventArgs))));
            }
        }

        void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            //validate
            if (!Enabled)
                return;

            //scroll to end
            if (AssociatedObject.Items.Count > 0)
                AssociatedObject.ScrollIntoView(AssociatedObject.Items[AssociatedObject.Items.Count - 1]);
        }

        public bool Enabled
        {
            get { return (bool)GetValue(EnabledProperty); }
            set { SetValue(EnabledProperty, value); }
        }

        public object ItemsSource
        {
            get { return GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }
    }
}
