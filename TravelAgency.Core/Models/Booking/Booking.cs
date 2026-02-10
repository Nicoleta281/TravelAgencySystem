using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravelAgency.Core.Models.TripPkg;
using TravelAgency.Core.Models.TripPkg.Package;
using TravelAgency.Core.Models.Users;

namespace TravelAgency.Core.Models.Booking
{
    public class Booking
    {
        public DateTime BookingDate { get; set; }
        public Client Client { get; set; }
        public TripPackage TripPackage { get; set; }
        public BookingStatus Status { get; set; }

        public void ConfirmBooking() { }
        public void CancelBooking() { }
        public void ChangeStatus(BookingStatus status) { }
    }
}
