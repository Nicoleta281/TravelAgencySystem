using System;
using TravelAgency.Core.Models.Booking;
using TravelAgency.Core.Models.Notifications;
using TravelAgency.Core.Patterns.Observer;

namespace TravelAgency.Core.Services
{
    /// <summary>Creează înregistrări de notificare pentru client din evenimente de schimbare status (SRP: separat de UI).</summary>
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
                trip = "Pachet";

            var title = "Actualizare rezervare";
            var detail =
                $"#{e.Booking.Id} „{trip}”: {BookingStatusDisplay.ToRomanian(oldS)} → {BookingStatusDisplay.ToRomanian(newS)}";

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
