using System;
using System.Linq;
using FluentValidation;
using TravelAgency.Core.Models;
using TravelAgency.Core.Models.TripPkg.Package;
using TravelAgency.Core.Models.TripPkg.Services;
using TravelAgency.Core.Patterns.Bridge;
using TravelAgency.Core.Patterns.Builders;
using TravelAgency.Core.Patterns.Factories.AbstractFactory;
using TravelAgency.Core.Validators;

namespace TravelAgency.Core.Services
{
    public class TripCreationService
    {
        private readonly TripComponentFactorySelector _componentFactorySelector;
        private readonly TripDirector _director;
        private readonly TripPackageBuilder _builder;

        public TripCreationService(
            TripComponentFactorySelector componentFactorySelector,
            TripDirector director,
            TripPackageBuilder builder)
        {
            _componentFactorySelector = componentFactorySelector
                ?? throw new ArgumentNullException(nameof(componentFactorySelector));
            _director = director ?? throw new ArgumentNullException(nameof(director));
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        public TripPackage CreateTrip(TripRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            ITripComponentFactory factory = _componentFactorySelector.Select(request.TripType);

            var transportType = string.IsNullOrWhiteSpace(request.TransportType)
                ? "Train"
                : request.TransportType;

            var transport = factory.CreateTransport(transportType);
            var stay = factory.CreateStay();

            // Builder + Director orchestrate the complex object graph (including Flyweight shared info).
            _director.ChangeBuilder(_builder);
            var trip = _director.Build(request);

            // doar daca utilizatorul a dat explicit final price
            if (request.FinalPrice > 0)
                trip.Price = request.FinalPrice;

            // componente create prin Abstract Factory
            trip.Transport = transport;
            trip.Stay = stay;

            // Bridge: Abstraction (itinerary) varies independently from implementors (transport/stay).
            // We pick the refined abstraction based on the trip type.
            trip.Itinerary = string.Equals(request.TripType, "Premium", StringComparison.OrdinalIgnoreCase)
                ? new PremiumTravelItinerary(trip.Name, transport, stay)
                : new StandardTravelItinerary(trip.Name, transport, stay);

            // pentru UI / fallback vizual
            trip.TransportDisplayName = !string.IsNullOrWhiteSpace(request.TransportType)
                ? request.TransportType
                : transportType;

            trip.StayDisplayName = request.AccommodationType ?? "";

            // refacem serviciile suplimentare dupa checkbox-uri
            trip.ExtraServices.Clear();

            if (request.AirportTransfer)
                trip.AddExtraService(new AirportTransfer());

            if (request.TravelInsurance)
                trip.AddExtraService(new TravelInsurance());

            if (request.TourGuide)
                trip.AddExtraService(new TourGuide());

            if (request.FreeCancellation)
                trip.AddExtraService(new FreeCancellation());

            // minim o zi
            if (trip.Days.Count == 0)
                trip.AddDay(new TripDay());

            // validare finala
            var validator = new TripPackageValidator();
            var result = validator.Validate(trip);

            if (!result.IsValid)
            {
                var message = string.Join(Environment.NewLine, result.Errors.Select(e => e.ErrorMessage));
                throw new ValidationException(message);
            }

            return trip;
        }
    }
}