using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace Mimic
{
	public class DropDownButton : ToggleButton
	{
		public static readonly DependencyProperty MenuProperty = DependencyProperty.Register("Menu", typeof(ContextMenu), typeof(DropDownButton), new UIPropertyMetadata(null, OnMenuChanged));

		public DropDownButton()
		{
			// Bind the ToogleButton.IsChecked property to the drop-down's IsOpen property
			Binding binding = new Binding("Menu.IsOpen");
			binding.Source = this;
			this.SetBinding(IsCheckedProperty, binding);
			DataContextChanged += (sender, args) =>
			{
				if (Menu != null)
					Menu.DataContext = DataContext;
			};
		}

		public ContextMenu Menu
		{
			get { return (ContextMenu)GetValue(MenuProperty); }
			set { SetValue(MenuProperty, value); }
		}

		private static void OnMenuChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var dropDownButton = (DropDownButton)d;
			var contextMenu = (ContextMenu)e.NewValue;
			contextMenu.DataContext = dropDownButton.DataContext;
		}

		protected override void OnClick()
		{
			base.OnClick();

			if (Menu != null)
			{
				Menu.PlacementTarget = this;
				Menu.Placement = PlacementMode.Bottom;
				Menu.IsOpen = true;
			}
		}
	}
}
