using System;
using TravelAgency.Core.Models.Booking;

namespace TravelAgency.Core.Patterns.State
{
    public class PendingBookingState : IBookingState
    {
        public string Name => "Pending";

        public void Confirm(Booking booking)
        {
            booking.SetState(new ConfirmedBookingState());
        }

        public void Reject(Booking booking)
        {
            booking.SetState(new RejectedBookingState());
        }

        public void Cancel(Booking booking)
        {
            booking.SetState(new CancelledBookingState());
        }
    }
}