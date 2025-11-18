using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Nexus.Desktop.Utilities
{
    public class TitleTrimConverter : IValueConverter
    {
        public static TitleTrimConverter Instance { get; } = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s)
            {
                if (s.Length <= 15)
                    return s;

                return s.Substring(0, 15) + "…";
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
