using System;
using TravelAgency.Core.Data.Repositories;
using TravelAgency.Core.Models.Booking;
using TravelAgency.Core.Patterns.Observer;

namespace TravelAgency.Core.Services
{
    public class BookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly BookingNotificationService _bookingNotificationService;

        public BookingService(
            IBookingRepository bookingRepository,
            BookingNotificationService bookingNotificationService)
        {
            _bookingRepository = bookingRepository ?? throw new ArgumentNullException(nameof(bookingRepository));
            _bookingNotificationService = bookingNotificationService ?? throw new ArgumentNullException(nameof(bookingNotificationService));
        }

        public Booking SubmitBooking(Booking booking)
        {
            if (booking == null)
                throw new ArgumentNullException(nameof(booking));

            var oldStatus = booking.StatusName;

            booking.SubmitRequest();
            var addedBooking = _bookingRepository.Add(booking);

            _bookingNotificationService.Notify(
                new BookingStatusChangedEvent(addedBooking, oldStatus, addedBooking.StatusName));

            return addedBooking;
        }

        public void ConfirmBooking(Booking booking)
        {
            if (booking == null)
                throw new ArgumentNullException(nameof(booking));

            var oldStatus = booking.StatusName;

            booking.ConfirmBooking();
            _bookingRepository.Update(booking);

            _bookingNotificationService.Notify(
                new BookingStatusChangedEvent(booking, oldStatus, booking.StatusName));
        }

        public void RejectBooking(Booking booking)
        {
            if (booking == null)
                throw new ArgumentNullException(nameof(booking));

            var oldStatus = booking.StatusName;

            booking.RejectBooking();
            _bookingRepository.Update(booking);

            _bookingNotificationService.Notify(
                new BookingStatusChangedEvent(booking, oldStatus, booking.StatusName));
        }

        public void CancelBooking(Booking booking)
        {
            if (booking == null)
                throw new ArgumentNullException(nameof(booking));

            var oldStatus = booking.StatusName;

            booking.CancelBooking();
            _bookingRepository.Update(booking);

            _bookingNotificationService.Notify(
                new BookingStatusChangedEvent(booking, oldStatus, booking.StatusName));
        }
    }
}