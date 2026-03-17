using System;
using TravelAgency.Core.Interfaces;
using TravelAgency.Core.Models.TripPkg.Services;
using TravelAgency.Core.Models.TripPkg.Stay;
using TravelAgency.Core.Models.TripPkg.Transport;

namespace TravelAgency.Core.Factories.AbstractFactory
{
    public class BudgetTripFactory : ITripComponentFactory
    {
        public ITransport CreateTransport(string transportType)
        {
            return transportType switch
            {
                "Bus" => new Bus(),
                "Train" => new Train(),
                "Plane" => new Plane(),
                _ => throw new ArgumentException("Tip de transport invalid.")
            };
        }

        public IStay CreateStay() => new Hostel();

        public IExtraService CreateExtraService() => new Breakfast();
    }
}