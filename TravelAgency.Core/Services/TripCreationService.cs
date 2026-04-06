using System;
using System.Linq;
using FluentValidation;
using TravelAgency.Core.Models;
using TravelAgency.Core.Models.TripPkg.Package;
using TravelAgency.Core.Models.TripPkg.Services;
using TravelAgency.Core.Patterns.Builders;
using TravelAgency.Core.Patterns.Factories.AbstractFactory;
using TravelAgency.Core.Validators;

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

            var builder = new TripPackageBuilder();
            var director = new TripDirector(builder);

            var trip = director.Build(request);

            // completare explicita a campurilor non-flyweight
            trip.Name = request.PackageName ?? "";
            trip.TripType = request.TripType ?? "";
            trip.Category = request.Category ?? "";
            trip.ShortDescription = request.ShortDescription ?? "";
            trip.BasePrice = request.BasePrice;
            trip.AvailableSeats = request.AvailableSeats;

            trip.DiscountPercent = request.DiscountPercent;
            trip.VatPercent = request.VatPercent;
            trip.ExtraCharges = request.ExtraCharges;

            // doar daca utilizatorul a dat explicit final price
            if (request.FinalPrice > 0)
                trip.Price = request.FinalPrice;

            // season: doar daca sunt ambele date
            if (request.StartDate.HasValue && request.EndDate.HasValue)
            {
                trip.Season = new Season
                {
                    Name = $"{request.Destination}, {request.Country} trip",
                    StartDate = request.StartDate.Value,
                    EndDate = request.EndDate.Value
                };
            }

            // componente create prin Abstract Factory
            trip.Transport = transport;
            trip.Stay = stay;

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