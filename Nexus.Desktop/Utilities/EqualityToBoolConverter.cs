using Avalonia.Data.Converters;
using System.Collections.Generic;
using System.Globalization;
using Nexus.Core.Models;

namespace Nexus.Desktop.Utilities
{
    public class EqualityToBoolConverter : IMultiValueConverter
    {
        public static readonly EqualityToBoolConverter Instance = new();

        public object Convert(IList<object?> values, System.Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count >= 2 && values[0] is Note sel && values[1] is Note cur)
                return sel.Id == cur.Id;
            return false;
        }
    }
}