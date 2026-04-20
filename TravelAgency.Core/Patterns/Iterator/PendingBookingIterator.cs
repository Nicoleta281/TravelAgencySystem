using System.Collections.Generic;
using System.Linq;
using TravelAgency.Core.Models.Booking;

namespace TravelAgency.Core.Patterns.Iterator
{
    public class PendingBookingIterator : IIterator<Booking>
    {
        private readonly List<Booking> _pendingBookings;
        private int _position;

        public PendingBookingIterator(List<Booking> bookings)
        {
            _pendingBookings = bookings
                .Where(b => b.StatusName == "Pending")
                .ToList();

            _position = 0;
        }

        public bool HasNext()
        {
            return _position < _pendingBookings.Count;
        }

        public Booking Next()
        {
            return _pendingBookings[_position++];
        }
    }
}