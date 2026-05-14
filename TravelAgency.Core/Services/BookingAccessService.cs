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
        private readonly ITripPackageRepository _tripPackageRepository;

        public BookingAccessService(
            IBookingRepository bookingRepository,
            ITripPackageRepository tripPackageRepository)
        {
            _bookingRepository = bookingRepository;
            _tripPackageRepository = tripPackageRepository;
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

            var tripPackageId = booking.TripPackage?.Id ?? 0;
            if (tripPackageId <= 0)
                throw new InvalidOperationException("Booking is missing a valid trip package id.");

            var trip = _tripPackageRepository.GetById(tripPackageId);
            if (trip == null)
                throw new InvalidOperationException("Trip package no longer exists.");

            // Draft packages are not bookable until finalized by an agent.
            if (!string.IsNullOrWhiteSpace(trip.PricingNotes) &&
                trip.PricingNotes.Trim().Equals("DRAFT", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "This package is still a draft and cannot be booked. Please choose another package.");
            }

            // Invariant: TripPackage.AvailableSeats == totalCapacity - count(Confirmed).
            // Pending does not decrement AvailableSeats until confirmation, so seats held by pending
            // requests = Pending only. We do not compare (Pending+Confirmed) to AvailableSeats (would double-count Confirmed).
            var pendingCount = _bookingRepository.CountByTripPackageIdAndStatuses(tripPackageId, "Pending");
            if (pendingCount >= trip.AvailableSeats)
            {
                throw new InvalidOperationException(
                    "There are no seats left for this package. Please choose another package.");
            }

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