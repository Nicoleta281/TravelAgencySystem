using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelAgency.Core.Patterns.Flyweight
{
    public class PackageSharedInfo
    {
        public string Destination { get; }
        public string Country { get; }
        public string DepartureCity { get; }
        public string AccommodationName { get; }
        public string MealPlan { get; }
        public string TransportType { get; }
        public string StayType { get; }

        public PackageSharedInfo(
            string destination,
            string country,
            string departureCity,
            string accommodationName,
            string mealPlan,
            string transportType,
            string stayType)
        {
            Destination = destination ?? "";
            Country = country ?? "";
            DepartureCity = departureCity ?? "";
            AccommodationName = accommodationName ?? "";
            MealPlan = mealPlan ?? "";
            TransportType = transportType ?? "";
            StayType = stayType ?? "";
        }
    }
}