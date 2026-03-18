using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelAgency.Core.Models
{
    public class TripRequest
    {
        // Step 1
        public string PackageName { get; set; } = "";
        public string TripType { get; set; } = "";
        public string Category { get; set; } = "";
        public string ShortDescription { get; set; } = "";

        // Step 2
        public string Destination { get; set; } = "";
        public string Country { get; set; } = "";
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int NumberOfDays { get; set; }

        // Step 3
        public string TransportType { get; set; } = "";
        public string DepartureCity { get; set; } = "";
        public string AccommodationType { get; set; } = "";
        public string AccommodationName { get; set; } = "";
        public string MealPlan { get; set; } = "";
        public int AvailableSeats { get; set; }

        public bool AirportTransfer { get; set; }
        public bool TravelInsurance { get; set; }
        public bool TourGuide { get; set; }
        public bool FreeCancellation { get; set; }

        // Step 4
        public double BasePrice { get; set; }
        public double DiscountPercent { get; set; }
        public double VatPercent { get; set; }
        public double ExtraCharges { get; set; }
        public double FinalPrice { get; set; }
    }
}
