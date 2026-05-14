using System;
using System.Globalization;
using System.Windows.Data;

namespace TravelAgency.WPF.Converters;

/// <summary>Display mapping for status names (kept English; normalizes unknown/empty values).</summary>
public sealed class BookingStatusToRomanianConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var key = value?.ToString()?.Trim() ?? "";
        return key switch
        {
            "Pending" => "Pending",
            "Confirmed" => "Confirmed",
            "Rejected" => "Rejected",
            "Cancelled" => "Cancelled",
            "Canceled" => "Cancelled",
            _ => string.IsNullOrEmpty(key) ? "—" : key
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
