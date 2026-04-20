using System.Collections.Generic;
using TravelAgency.Core.Models.Booking;

namespace TravelAgency.Core.Patterns.Iterator
{
    public class AllBookingIterator : IIterator<Booking>
    {
        private readonly List<Booking> _bookings;
        private int _position;

        public AllBookingIterator(List<Booking> bookings)
        {
            _bookings = bookings;
            _position = 0;
        }

        public bool HasNext()
        {
            return _position < _bookings.Count;
        }

        public Booking Next()
        {
            return _bookings[_position++];
        }
    }
}