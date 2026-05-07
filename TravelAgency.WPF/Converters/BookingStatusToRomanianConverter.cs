using System;
using System.Globalization;
using System.Windows.Data;

namespace TravelAgency.WPF.Converters;

/// <summary>Mapare afișare RO pentru numele de status din model (EN), fără a schimba datele din DB.</summary>
public sealed class BookingStatusToRomanianConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var key = value?.ToString()?.Trim() ?? "";
        return key switch
        {
            "Pending" => "În așteptare",
            "Confirmed" => "Confirmată",
            "Rejected" => "Respinsă",
            "Cancelled" => "Anulată",
            _ => string.IsNullOrEmpty(key) ? "—" : key
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
