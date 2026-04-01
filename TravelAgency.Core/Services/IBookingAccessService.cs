using System.Collections.Generic;
using TravelAgency.Core.Models.Booking;

namespace TravelAgency.Core.Services
{
    public interface IBookingAccessService
    {
        List<Booking> GetPendingBookings();
        List<Booking> GetBookingsForCurrentUser();
        void SubmitBooking(Booking booking);
        void ApproveBooking(Booking booking);
        void RejectBooking(Booking booking);
    }
}