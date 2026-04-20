using System.Collections.Generic;
using System.Linq;
using TravelAgency.Core.Models.Booking;

namespace TravelAgency.Core.Patterns.Iterator
{
    public class ConfirmedBookingIterator : IIterator<Booking>
    {
        private readonly List<Booking> _confirmedBookings;
        private int _position;

        public ConfirmedBookingIterator(List<Booking> bookings)
        {
            _confirmedBookings = bookings
                .Where(b => b.StatusName == "Confirmed")
                .ToList();

            _position = 0;
        }

        public bool HasNext()
        {
            return _position < _confirmedBookings.Count;
        }

        public Booking Next()
        {
            return _confirmedBookings[_position++];
        }
    }
}