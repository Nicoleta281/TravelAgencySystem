using System;
using TravelAgency.Core.Models.Booking;

namespace TravelAgency.Core.Patterns.State
{
    public class ConfirmedBookingState : IBookingState
    {
        public string Name => "Confirmed";

        public void Confirm(Booking booking)
        {
            throw new InvalidOperationException("Booking is already confirmed.");
        }

        public void Reject(Booking booking)
        {
            throw new InvalidOperationException("Confirmed booking cannot be rejected.");
        }

        public void Cancel(Booking booking)
        {
            booking.SetState(new CancelledBookingState());
        }
    }
}