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
                    "Acest pachet este încă în draft și nu poate fi rezervat. Te rugăm să alegi alt pachet.");
            }

            // Invariant: TripPackage.AvailableSeats == totalCapacity - count(Confirmed).
            // Pending nu decrementează AvailableSeats până la confirmare, deci locuri „rezervate” de cereri
            // în așteptare = doar Pending. Nu comparăm (Pending+Confirmed) cu AvailableSeats (ar număra C de două ori).
            var pendingCount = _bookingRepository.CountByTripPackageIdAndStatuses(tripPackageId, "Pending");
            if (pendingCount >= trip.AvailableSeats)
            {
                throw new InvalidOperationException(
                    "Nu mai sunt disponibile locuri la acest pachet. Te rugăm să alegi alt pachet.");
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