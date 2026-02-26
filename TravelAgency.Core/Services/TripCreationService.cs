using System;
using TravelAgency.Core.Builders;
using TravelAgency.Core.Factories;
using TravelAgency.Core.Interfaces;
using TravelAgency.Core.Models.TripPkg.Package;

namespace TravelAgency.Core.Services
{
    public class TripCreationService
    {
        public TripPackage CreateTrip(string tripType, string transportType, string name, double price)
        {
            // Abstract Factory: familia Premium/Budget
            ITripComponentFactory componentFactory =
                tripType == "Premium" ? new PremiumTripFactory() : new BudgetTripFactory();

            // Factory Method: transportul ales
            TransportFactory transportFactory = transportType switch
            {
                "Plane" => new PlaneFactory(),
                "Bus" => new BusFactory(),
                _ => new TrainFactory()
            };

            var transport = transportFactory.CreateTransport();

            var season = new Season
            {
                Name = "Default",
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddMonths(3)
            };

            // Builder: asamblează TripPackage final
            return new TripPackageBuilder()
                .WithName(name)
                .WithPrice(price)
                .WithSeason(season)
                .WithTransport(transport)
                .WithStay(componentFactory.CreateStay())
                .WithExtra(componentFactory.CreateExtraService())
                .WithDay(new TripDay())
                .Build();
        }
    }
}
