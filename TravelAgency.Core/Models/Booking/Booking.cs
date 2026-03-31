using System;
using System.Collections.Generic;
using TravelAgency.Core.Models.TripPkg.Package;
using TravelAgency.Core.Models.Users;

namespace TravelAgency.Core.Models.Booking
{
    public class Booking
    {
        public int Id { get; set; }

        public DateTime BookingDate { get; set; } = DateTime.Now;

        public Client? Client { get; set; }

        public TripPackage? TripPackage { get; set; }

        public BookingStatus? Status { get; set; }

        public List<string> SelectedExtras { get; set; } = new();

        public double BasePrice { get; set; }

        public double TotalPrice { get; set; }

        public void ConfirmBooking()
        {
            Status = new BookingStatus { Name = "Confirmed" };
        }

        public void CancelBooking()
        {
            Status = new BookingStatus { Name = "Cancelled" };
        }

        public void ChangeStatus(BookingStatus status)
        {
            Status = status;
        }
    }
}