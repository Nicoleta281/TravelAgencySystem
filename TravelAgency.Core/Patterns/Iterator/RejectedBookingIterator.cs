using System.Collections.Generic;
using System.Linq;
using TravelAgency.Core.Models.Booking;

namespace TravelAgency.Core.Patterns.Iterator
{
    public class RejectedBookingIterator : IIterator<Booking>
    {
        private readonly List<Booking> _rejectedBookings;
        private int _position;

        public RejectedBookingIterator(List<Booking> bookings)
        {
            _rejectedBookings = bookings
                .Where(b => b.StatusName == "Rejected")
                .ToList();

            _position = 0;
        }

        public bool HasNext()
        {
            return _position < _rejectedBookings.Count;
        }

        public Booking Next()
        {
            return _rejectedBookings[_position++];
        }
    }
}