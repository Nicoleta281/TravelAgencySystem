using System;
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
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            ITripComponentFactory factory = request.TripType == "Premium"
                ? new PremiumTripFactory()
                : new BudgetTripFactory();

            var transportType = string.IsNullOrWhiteSpace(request.TransportType)
                ? "Train"
                : request.TransportType;

            var transport = factory.CreateTransport(transportType);
            var stay = factory.CreateStay();
            var extraService = factory.CreateExtraService();

            var builder = new TripPackageBuilder();
            var director = new TripDirector(builder);

            var trip = director.Make(request);

            // Componentele create prin Factory
            trip.Transport = transport;
            trip.Stay = stay;

            if (extraService != null)
                trip.AddExtraService(extraService);

            // minim o zi, ca sa nu ramana gol
            trip.AddDay(new TripDay());

            return trip;
        }
    }
}