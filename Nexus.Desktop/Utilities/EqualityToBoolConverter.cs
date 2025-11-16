using Avalonia.Data.Converters;
using Nexus.Core.Models;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Nexus.Desktop.Utilities
{
    public class EqualityToBoolConverter : IMultiValueConverter
    {
        public static readonly EqualityToBoolConverter Instance = new();

        public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count < 2)
                return false;

            var left = values[0];
            var right = values[1];

            // Notes
            if (left is Note ln && right is Note rn)
                return ln.Id == rn.Id;

            // Topics
            if (left is Topic lt && right is Topic rt)
                return lt.Id == rt.Id || lt.Name == rt.Name;

            return Equals(left, right);
        }
    }
}