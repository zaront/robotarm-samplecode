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
    class ResourceIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var imageName = value as string;
            if (imageName == null)
                return null;

            //get icon from resources
            var icon = Application.Current.Resources[imageName];
            if (icon != null)
                return icon;

            //if not there, load icons and get it
            var iconResource = new ResourceDictionary();
            iconResource.Source = new Uri("/Resources/Icons.xaml", UriKind.RelativeOrAbsolute);
            return iconResource[imageName];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException("ResourceIconConverter can only be used OneWay.");
        }
    }
}
