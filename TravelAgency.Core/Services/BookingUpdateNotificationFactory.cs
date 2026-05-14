using System;
using TravelAgency.Core.Models.Booking;
using TravelAgency.Core.Models.Notifications;
using TravelAgency.Core.Patterns.Observer;

namespace TravelAgency.Core.Services
{
    /// <summary>Builds client booking notifications from status-change events (SRP: separate from UI).</summary>
    public static class BookingUpdateNotificationFactory
    {
        public static BookingUpdateNotification? TryCreate(BookingStatusChangedEvent e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            var oldS = (e.OldStatus ?? "").Trim();
            var newS = (e.NewStatus ?? "").Trim();
            if (BookingStatusDisplay.AreEquivalent(oldS, newS))
                return null;

            var trip = e.Booking.TripPackage?.Name?.Trim();
            if (string.IsNullOrWhiteSpace(trip))
                trip = "Package";

            var title = "Booking update";
            var detail =
                $"#{e.Booking.Id} \"{trip}\": {BookingStatusDisplay.ToEnglish(oldS)} → {BookingStatusDisplay.ToEnglish(newS)}";

            return new BookingUpdateNotification
            {
                BookingId = e.Booking.Id,
                TripDisplayName = trip,
                OldStatus = oldS,
                NewStatus = newS,
                Title = title,
                Detail = detail,
                IsRead = false,
            };
        }
    }
}
