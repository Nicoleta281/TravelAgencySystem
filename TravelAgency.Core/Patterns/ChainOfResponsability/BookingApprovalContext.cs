using TravelAgency.Core.Models.Booking;

namespace TravelAgency.Core.Patterns.ChainOfResponsibility
{
    public class BookingApprovalContext
    {
        public BookingApprovalContext(Booking booking)
        {
            Booking = booking;
        }

        public Booking Booking { get; }
    }
}