using System;
using TravelAgency.Core.Builders;
using TravelAgency.Core.Factories.AbstractFactory;
using TravelAgency.Core.Models;
using TravelAgency.Core.Models.TripPkg.Package;
using TravelAgency.Core.Models.TripPkg.Services;

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

            var trip = director.Make(request);

            // completare explicita a tuturor campurilor importante
            trip.Name = request.PackageName ?? "";

            // Only override price when FinalPrice provided (>0). Builder already sets price otherwise.
            if (request.FinalPrice > 0)
                trip.Price = request.FinalPrice;

            trip.Destination = request.Destination ?? "";
            trip.Country = request.Country ?? "";
            trip.DepartureCity = request.DepartureCity ?? "";
            trip.AccommodationName = request.AccommodationName ?? "";
            trip.MealPlan = request.MealPlan ?? "";
            trip.AvailableSeats = request.AvailableSeats;

            trip.DiscountPercent = request.DiscountPercent;
            trip.VatPercent = request.VatPercent;
            trip.ExtraCharges = request.ExtraCharges;

            // season: only set if both dates provided to match builder behavior
            if (request.StartDate.HasValue && request.EndDate.HasValue)
            {
                trip.Season = new Season
                {
                    Name = $"{request.Destination}, {request.Country} trip",
                    StartDate = request.StartDate.Value,
                    EndDate = request.EndDate.Value
                };
            }

            // componente create prin factory
            trip.Transport = transport;
            trip.Stay = stay;

            // Use resolved transportType/stay fallback so display matches actual components
            trip.TransportDisplayName = !string.IsNullOrWhiteSpace(request.TransportType) ? request.TransportType : transportType;
            trip.StayDisplayName = request.AccommodationType ?? "";

            // refacem serviciile suplimentare exact dupa checkbox-uri
            trip.ExtraServices.Clear();

            if (request.AirportTransfer)
                trip.AddExtraService(new AirportTransfer());

            if (request.TravelInsurance)    trip.AddExtraService(   
                trip.AddExtraService(new TravelInsurance());

            if (request.TourGuide)
                trip.AddExtraService(new TourGuide());

            if (request.FreeCancellation)
                trip.AddExtraService(new FreeCancellation());

            // minim o zi
            if (trip.Days.Count == 0)
                trip.AddDay(new TripDay());

            return trip;
        }
    }
}