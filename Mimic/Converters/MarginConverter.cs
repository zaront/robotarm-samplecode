using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Mimic.Converters
{
    public class MarginConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var position = 0;
            if (parameter is int)
                position = (int)parameter;

            if (position == 2)
                return new Thickness(0, System.Convert.ToDouble(value), 0, 0);
            else if (position == 3)
                return new Thickness(0, 0, System.Convert.ToDouble(value), 0);
            else if (position == 4)
                return new Thickness(0, 0, 0, System.Convert.ToDouble(value));
            else
                return new Thickness(System.Convert.ToDouble(value), 0, 0, 0);
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
