using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace Mimic.Behaviors
{
    public class ScrollIntoViewBehavior : Behavior<ItemsControl>
    {
        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register("SelectedItem", typeof(object), typeof(ScrollIntoViewBehavior), new PropertyMetadata(SelecteItemChanged));

        static void SelecteItemChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            //validate
            if (e.NewValue == null)
                return;

            //find item (framework element)
            var me = source as ScrollIntoViewBehavior;
            var visual = me.AssociatedObject.ItemContainerGenerator.ContainerFromItem(e.NewValue) as FrameworkElement;
            if (visual == null)
                return;

            //scroll into view
            visual.BringIntoView();
        }

        public object SelectedItem
        {
            get { return GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }
    }
}
