using System;
using System.Collections.Generic;
using System.Linq;
using TravelAgency.Core.Models.Booking;
using TravelAgency.Core.Models.Users;

namespace TravelAgency.Core.Services
{
    public class BookingAccessProxy : IBookingAccessService
    {
        private readonly IBookingAccessService _realService;
        private readonly User _currentUser;

        public BookingAccessProxy(IBookingAccessService realService, User currentUser)
        {
            _realService = realService ?? throw new ArgumentNullException(nameof(realService));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        }

        public List<Booking> GetPendingBookings()
        {
            if (_currentUser is not Agent)
                throw new UnauthorizedAccessException("Only agents can view pending bookings.");

            return _realService.GetPendingBookings();
        }

        public List<Booking> GetBookingsForCurrentUser()
        {
            var allBookings = _realService.GetBookingsForCurrentUser();

            if (_currentUser is Agent)
                return allBookings;

            if (_currentUser is Client client)
            {
                return allBookings
                    .Where(b => b.Client != null &&
                                !string.IsNullOrWhiteSpace(b.Client.Username) &&
                                b.Client.Username == client.Username)
                    .ToList();
            }

            throw new UnauthorizedAccessException("Unknown user type.");
        }

        public void SubmitBooking(Booking booking)
        {
            if (_currentUser is not Client client)
                throw new UnauthorizedAccessException("Only clients can submit bookings.");

            if (booking == null)
                throw new ArgumentNullException(nameof(booking));

            if (booking.Client == null || booking.Client.Username != client.Username)
                throw new UnauthorizedAccessException("Clients can submit only their own bookings.");

            _realService.SubmitBooking(booking);
        }

        public void ApproveBooking(Booking booking)
        {
            if (_currentUser is not Agent)
                throw new UnauthorizedAccessException("Only agents can approve bookings.");

            _realService.ApproveBooking(booking);
        }

        public void RejectBooking(Booking booking)
        {
            if (_currentUser is not Agent)
                throw new UnauthorizedAccessException("Only agents can reject bookings.");

            _realService.RejectBooking(booking);
        }
    }
}