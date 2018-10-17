using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace Mimic.Converters
{
    public class ColorLuminosityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var brush = value as SolidColorBrush;
            if (brush == null)
                return value;

            var luminosityPercent = double.Parse(parameter.ToString());
            var color = (HSLColor)brush.Color;
            color.Luminosity = color.Luminosity * luminosityPercent;

            return new SolidColorBrush(color);


        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException("IsNullConverter can only be used OneWay.");
        }
    }
}
