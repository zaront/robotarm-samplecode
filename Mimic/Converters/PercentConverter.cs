using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Mimic.Converters
{
    public class PercentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            
            if (value is float || value is decimal || value is double)
            {
                var percent = (double)value;
                var max = 0.0;
                if (parameter is float || parameter is decimal || parameter is double || parameter is int || parameter is long)
                    max = (double)parameter;
                if (parameter is string)
                    max = double.Parse(parameter as string);
                if (max != 0)
                    return percent * max;
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException("PercentConverter can only be used OneWay.");
        }
    }
}
