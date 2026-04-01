using System;
using System.Collections.Generic;
using System.Linq;
using TravelAgency.Core.Data.Repositories;
using TravelAgency.Core.Models.Booking;

namespace TravelAgency.Core.Services
{
    public class BookingAccessService : IBookingAccessService
    {
        private readonly IBookingRepository _bookingRepository;

        public BookingAccessService(IBookingRepository bookingRepository)
        {
            _bookingRepository = bookingRepository;
        }

        public List<Booking> GetPendingBookings()
        {
            return _bookingRepository.GetPending().ToList();
        }

        public List<Booking> GetBookingsForCurrentUser()
        {
            // serviciul real nu decide securitatea
            // proxy-ul va controla cine poate vedea ce
            return _bookingRepository.GetAll().ToList();
        }

        public void SubmitBooking(Booking booking)
        {
            if (booking == null)
                throw new ArgumentNullException(nameof(booking));

            _bookingRepository.Add(booking);
        }

        public void ApproveBooking(Booking booking)
        {
            if (booking == null)
                throw new ArgumentNullException(nameof(booking));

            booking.ConfirmBooking();
            _bookingRepository.Update(booking);
        }

        public void RejectBooking(Booking booking)
        {
            if (booking == null)
                throw new ArgumentNullException(nameof(booking));

            booking.RejectBooking();
            _bookingRepository.Update(booking);
        }
    }
}