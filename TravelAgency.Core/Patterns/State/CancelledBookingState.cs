using System;
using TravelAgency.Core.Models.Booking;

namespace TravelAgency.Core.Patterns.State
{
    public class CancelledBookingState : IBookingState
    {
        public string Name => "Cancelled";

        public void Confirm(Booking booking)
        {
            throw new InvalidOperationException("Cancelled booking cannot be confirmed.");
        }

        public void Reject(Booking booking)
        {
            throw new InvalidOperationException("Cancelled booking cannot be rejected.");
        }

        public void Cancel(Booking booking)
        {
            throw new InvalidOperationException("Booking is already cancelled.");
        }
    }
}