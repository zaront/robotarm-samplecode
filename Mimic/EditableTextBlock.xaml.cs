using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Mimic
{
    public partial class EditableTextBlock : UserControl
	{
		public static readonly DependencyProperty AcceptsReturnProperty = System.Windows.Controls.Primitives.TextBoxBase.AcceptsReturnProperty.AddOwner(typeof(EditableTextBlock));
		public static readonly DependencyProperty IsEditingProperty = DependencyProperty.Register(nameof(IsEditing), typeof(bool), typeof(EditableTextBlock), new PropertyMetadata(new PropertyChangedCallback(EditableTextBlock.HandleIsEditingChanged)));
		public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(EditableTextBlock), (PropertyMetadata)new FrameworkPropertyMetadata((object)null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
		public static readonly DependencyProperty TextWhenEmptyProperty = DependencyProperty.Register(nameof(TextWhenEmpty), typeof(string), typeof(EditableTextBlock));
		public static readonly DependencyProperty TextWrappingProperty = System.Windows.Controls.TextBox.TextWrappingProperty.AddOwner(typeof(EditableTextBlock));

		public event EventHandler<TextChangingEventArgs> TextChanging;

		public EditableTextBlock()
		{
			InitializeComponent();
		}

		private void Control_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ClickCount != 2)
				return;
			e.Handled = true;
			this.IsEditing = !this.IsEditing;
			if (!this.IsEditing)
				return;
		}

		private void Control_IsKeyboardFocusWithinChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if ((bool)e.NewValue)
				return;
			this.IsEditing = false;
		}

		public bool AcceptsReturn
		{
			get { return (bool)this.GetValue(EditableTextBlock.AcceptsReturnProperty); }
			set { this.SetValue(EditableTextBlock.AcceptsReturnProperty, (object)value); }
		}

		public bool IsEditing
		{
			get { return (bool)this.GetValue(EditableTextBlock.IsEditingProperty); }
			set { this.SetValue(EditableTextBlock.IsEditingProperty, (object)value); }
		}

		public string Text
		{
			get { return (string)this.GetValue(EditableTextBlock.TextProperty); }
			set { this.SetValue(EditableTextBlock.TextProperty, (object)value); }
		}

		public string TextWhenEmpty
		{
			get { return (string)this.GetValue(EditableTextBlock.TextWhenEmptyProperty); }
			set { this.SetValue(EditableTextBlock.TextWhenEmptyProperty, (object)value); }
		}

		public TextWrapping TextWrapping
		{
			get { return (TextWrapping)this.GetValue(EditableTextBlock.TextWrappingProperty); }
			set { this.SetValue(EditableTextBlock.TextWrappingProperty, (object)value); }
		}

		private static void HandleIsEditingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			//validate
			if ((bool)e.NewValue || !(bool)e.OldValue)
				return;

			var me = ((EditableTextBlock)d);
			var binding = me.TextBox.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty);
			if (binding.IsDirty)
			{
				var args = new TextChangingEventArgs() { Text = me.TextBox.Text, DataContext = me.DataContext };
				me.TextChanging?.Invoke(me, args);
				if (args.Cancel)
					binding.UpdateTarget(); //revert old value
				else
				{
					if (me.TextBox.Text != args.Text)
						me.TextBox.Text = args.Text;
					binding.UpdateSource(); //update with new value
				}
			}
				
		}

		void Fire()
		{

		}

		private void TextBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Return)
			{
				e.Handled = true;
				IsEditing = false;
			}
			if (e.Key == Key.Escape)
			{
				e.Handled = true;
				TextBox.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty).UpdateTarget(); //revert old value
				IsEditing = false;
			}
		}

		private void TextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (TextBox.IsVisible)
			{
				TextBox.Focus();
				TextBox.SelectAll();
			}
		}



		public class TextChangingEventArgs : EventArgs
		{
			public string Text { get; set; }
			public bool Cancel { get; set; }
			public object DataContext { get; set; }
		}
	}
}
