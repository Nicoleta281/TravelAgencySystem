using TravelAgency.Core.Models.Booking;

namespace TravelAgency.Core.Patterns.Observer
{
    public class BookingStatusChangedEvent
    {
        public Booking Booking { get; }
        public string OldStatus { get; }
        public string NewStatus { get; }

        public BookingStatusChangedEvent(Booking booking, string oldStatus, string newStatus)
        {
            Booking = booking;
            OldStatus = oldStatus;
            NewStatus = newStatus;
        }
    }
}