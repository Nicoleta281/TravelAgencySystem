using System;
using TravelAgency.Core.Data.Repositories;
using TravelAgency.Core.Models.Booking;
using TravelAgency.Core.Patterns.Observer;

namespace TravelAgency.Core.Services
{
    public class BookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly ITripPackageRepository _tripPackageRepository;
        private readonly BookingNotificationService _bookingNotificationService;

        public BookingService(
            IBookingRepository bookingRepository,
            ITripPackageRepository tripPackageRepository,
            BookingNotificationService bookingNotificationService)
        {
            _bookingRepository = bookingRepository ?? throw new ArgumentNullException(nameof(bookingRepository));
            _tripPackageRepository = tripPackageRepository ?? throw new ArgumentNullException(nameof(tripPackageRepository));
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

            var tripPackageId = booking.TripPackage?.Id ?? 0;
            if (tripPackageId <= 0)
                throw new InvalidOperationException("Booking is missing a valid trip package id.");

            var trip = _tripPackageRepository.GetById(tripPackageId);
            if (trip == null)
                throw new InvalidOperationException("Trip package no longer exists.");

            // Confirmation: AvailableSeats already reflects seats left after confirmed bookings (T − C).
            // We do not compare count(Pending+Confirmed) to AvailableSeats — that would double-count Confirmed.
            if (string.Equals(oldStatus, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                if (trip.AvailableSeats <= 0)
                {
                    throw new InvalidOperationException(
                        "There are no seats left for this package. Please choose another package or try again later.");
                }

                trip.AvailableSeats -= 1;
                _tripPackageRepository.Update(trip);
            }

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

            var tripPackageId = booking.TripPackage?.Id ?? 0;

            booking.RejectBooking();
            _bookingRepository.Update(booking);

            if (tripPackageId > 0 &&
                string.Equals(oldStatus, "Confirmed", StringComparison.OrdinalIgnoreCase))
            {
                ReleaseOneSeat(tripPackageId);
            }

            _bookingNotificationService.Notify(
                new BookingStatusChangedEvent(booking, oldStatus, booking.StatusName));
        }

        public void CancelBooking(Booking booking)
        {
            if (booking == null)
                throw new ArgumentNullException(nameof(booking));

            var oldStatus = booking.StatusName;

            var tripPackageId = booking.TripPackage?.Id ?? 0;

            booking.CancelBooking();
            _bookingRepository.Update(booking);

            if (tripPackageId > 0 &&
                string.Equals(oldStatus, "Confirmed", StringComparison.OrdinalIgnoreCase))
            {
                ReleaseOneSeat(tripPackageId);
            }

            _bookingNotificationService.Notify(
                new BookingStatusChangedEvent(booking, oldStatus, booking.StatusName));
        }

        private void ReleaseOneSeat(int tripPackageId)
        {
            var trip = _tripPackageRepository.GetById(tripPackageId);
            if (trip == null)
                return;

            trip.AvailableSeats += 1;
            _tripPackageRepository.Update(trip);
        }
    }
}