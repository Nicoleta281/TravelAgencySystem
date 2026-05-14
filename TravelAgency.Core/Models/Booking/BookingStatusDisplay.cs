using System;

namespace TravelAgency.Core.Models.Booking
{
    /// <summary>Human-readable booking status labels for UI.</summary>
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

        /// <summary>Canonical English labels for client notifications and English UI.</summary>
        public static string ToEnglish(string? status)
        {
            return (status ?? "").Trim() switch
            {
                "Pending" => "Pending",
                "Confirmed" => "Confirmed",
                "Rejected" => "Rejected",
                "Cancelled" => "Cancelled",
                _ => string.IsNullOrWhiteSpace(status) ? "—" : status!
            };
        }

        public static bool AreEquivalent(string? a, string? b) =>
            string.Equals((a ?? "").Trim(), (b ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
