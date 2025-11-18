using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Nexus.Desktop.Utilities
{
    public class BoolToOpacityConverter : IValueConverter
    {
        public static readonly BoolToOpacityConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is bool b && b) ? 1.0 : 0.0;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
           => throw new NotSupportedException();
    }
}
