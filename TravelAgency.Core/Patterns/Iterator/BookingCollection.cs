using System.Collections.Generic;
using TravelAgency.Core.Models.Booking;

namespace TravelAgency.Core.Patterns.Iterator
{
    public class BookingCollection : IBookingCollection
    {
        private readonly List<Booking> _bookings;

        public BookingCollection(List<Booking> bookings)
        {
            _bookings = bookings;
        }

        public IIterator<Booking> CreateAllIterator()
        {
            return new AllBookingIterator(_bookings);
        }

        public IIterator<Booking> CreatePendingIterator()
        {
            return new PendingBookingIterator(_bookings);
        }

        public IIterator<Booking> CreateConfirmedIterator()
        {
            return new ConfirmedBookingIterator(_bookings);
        }

        public IIterator<Booking> CreateRejectedIterator()
        {
            return new RejectedBookingIterator(_bookings);
        }
    }
}