using System;

namespace TravelAgency.Core.Models.Booking
{
    /// <summary>Etichete românești pentru stări de rezervare (UI notificări / rapoarte).</summary>
    public static class BookingStatusDisplay
    {
        public static string ToRomanian(string? status)
        {
            return (status ?? "").Trim() switch
            {
                "Pending" => "în așteptare",
                "Confirmed" => "confirmată",
                "Rejected" => "respinsă",
                "Cancelled" => "anulată",
                _ => string.IsNullOrWhiteSpace(status) ? "—" : status!
            };
        }

        public static bool AreEquivalent(string? a, string? b) =>
            string.Equals((a ?? "").Trim(), (b ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
