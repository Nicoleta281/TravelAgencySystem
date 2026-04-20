using System;
using TravelAgency.Core.Models.Booking;

namespace TravelAgency.Core.Patterns.State
{
    public class RejectedBookingState : IBookingState
    {
        public string Name => "Rejected";

        public void Confirm(Booking booking)
        {
            throw new InvalidOperationException("Rejected booking cannot be confirmed.");
        }

        public void Reject(Booking booking)
        {
            throw new InvalidOperationException("Booking is already rejected.");
        }

        public void Cancel(Booking booking)
        {
            throw new InvalidOperationException("Rejected booking cannot be cancelled.");
        }
    }
}
