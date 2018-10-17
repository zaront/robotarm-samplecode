using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Mimic.Converters
{
    public class VisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
			if (value is Enum)
			{
				var e = value as Enum;
				var p = parameter as Enum;
				if (e != null && p != null)
					return e.HasFlag(p) ? Visibility.Visible : Visibility.Collapsed;
			}
			else
			{
				if (value != null && parameter != null)
					return value.Equals(parameter) ? Visibility.Visible : Visibility.Collapsed;
			}

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException("VisibilityConverter can only be used OneWay.");
        }
    }
}
