using TravelAgency.Core.Builders;
using TravelAgency.Core.Factories.AbstractFactory;
using TravelAgency.Core.Models;
using TravelAgency.Core.Models.TripPkg.Package;

namespace TravelAgency.Core.Services
{
    public class TripCreationService
    {
        public TripPackage CreateTrip(TripRequest request)
        {
            ITripComponentFactory factory = request.TripType == "Premium"
                ? new PremiumTripFactory()
                : new BudgetTripFactory();

            var transport = factory.CreateTransport(request.TransportType);
            var stay = factory.CreateStay();
            var extraService = factory.CreateExtraService();

            var trip = new TripPackageBuilder()
                .WithName(request.Name)
                .WithPrice(request.Price)
                .WithTransport(transport)
                .WithStay(stay)
                .WithExtra(extraService)
                .WithDay(new TripDay())
                .Build();

            return trip;
        }
    }
}