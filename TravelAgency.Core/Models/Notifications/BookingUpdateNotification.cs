using System;

namespace TravelAgency.Core.Models.Notifications
{
    /// <summary>Înregistrare afișată clientului când se schimbă starea unei rezervări (observer in-app).</summary>
    public sealed class BookingUpdateNotification
    {
        public Guid Id { get; } = Guid.NewGuid();

        public DateTime CreatedAt { get; } = DateTime.Now;

        public int BookingId { get; init; }

        public string TripDisplayName { get; init; } = "";

        public string OldStatus { get; init; } = "";

        public string NewStatus { get; init; } = "";

        public string Title { get; init; } = "";

        public string Detail { get; init; } = "";

        public bool IsRead { get; set; }
    }
}
