using System;
using System.Globalization;
using System.Windows.Data;

namespace TravelAgency.WPF.Converters;

public sealed class StringEqualsConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
            return false;

        var a = values[0]?.ToString() ?? "";
        var b = values[1]?.ToString() ?? "";

        var equal = string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        var invert = string.Equals(parameter?.ToString(), "Invert", StringComparison.OrdinalIgnoreCase);
        var match = invert ? !equal : equal;

        // Optional third value: gate (e.g., IsMouseOver / IsPressed)
        if (values.Length >= 3)
        {
            var gate = values[2] is bool bb ? bb : System.Convert.ToBoolean(values[2] ?? false);
            return match && gate;
        }

        return match;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
