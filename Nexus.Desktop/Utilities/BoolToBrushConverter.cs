using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace Nexus.Desktop.Utilities
{
    public class BoolToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isNew = value is bool b && b;

            return isNew
                ? new SolidColorBrush(Avalonia.Media.Color.Parse("#334466")) // special color for "New Topic"
                : Avalonia.Media.Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

}
