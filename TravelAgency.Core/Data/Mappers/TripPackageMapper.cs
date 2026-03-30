using System;
using System.Linq;
using TravelAgency.Core.Data.Entities;
using TravelAgency.Core.Interfaces;
using TravelAgency.Core.Models.TripPkg.Package;
using TravelAgency.Core.Models.TripPkg.Services;

namespace TravelAgency.Core.Data.Mappers
{
    public static class TripPackageMapper
    {
        public static TripPackageEntity ToEntity(TripPackage trip)
        {
            var season = trip.Season ?? new Season
            {
                Name = "Default",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddMonths(3)
            };

            return new TripPackageEntity
            {
                Id = trip.Id,
                Name = trip.Name,
                Price = trip.Price,

                TripType = trip.TripType ?? "",
                Category = trip.Category ?? "",
                ShortDescription = trip.ShortDescription ?? "",
                PricingNotes = trip.PricingNotes ?? "",
                BasePrice = trip.BasePrice,

                SeasonName = season.Name,
                SeasonStartDate = DateTime.SpecifyKind(season.StartDate, DateTimeKind.Utc),
                SeasonEndDate = DateTime.SpecifyKind(season.EndDate, DateTimeKind.Utc),

                TransportType = trip.Transport?.GetType().Name
                    ?? trip.TransportDisplayName
                    ?? "",

                StayType = trip.Stay?.GetType().Name
                    ?? trip.StayDisplayName
                    ?? "",

                ExtraServices = string.Join(",", trip.ExtraServices.Select(x => x.GetType().Name)),

                Destination = trip.Destination,
                Country = trip.Country,
                DepartureCity = trip.DepartureCity,
                AccommodationName = trip.AccommodationName,
                MealPlan = trip.MealPlan,
                AvailableSeats = trip.AvailableSeats,

                DiscountPercent = trip.DiscountPercent,
                VatPercent = trip.VatPercent,
                ExtraCharges = trip.ExtraCharges
            };
        }

        public static TripPackage FromEntity(TripPackageEntity e)
        {
            var trip = new TripPackage
            {
                Id = e.Id,
                Name = e.Name,
                Price = e.Price,

                TripType = e.TripType ?? "",
                Category = e.Category ?? "",
                ShortDescription = e.ShortDescription ?? "",
                PricingNotes = e.PricingNotes ?? "",
                BasePrice = e.BasePrice,

                TransportDisplayName = e.TransportType ?? "",
                StayDisplayName = e.StayType ?? "",

                Destination = e.Destination ?? "",
                Country = e.Country ?? "",
                DepartureCity = e.DepartureCity ?? "",
                AccommodationName = e.AccommodationName ?? "",
                MealPlan = e.MealPlan ?? "",
                AvailableSeats = e.AvailableSeats,

                DiscountPercent = e.DiscountPercent,
                VatPercent = e.VatPercent,
                ExtraCharges = e.ExtraCharges,

                Season = new Season
                {
                    Name = e.SeasonName,
                    StartDate = e.SeasonStartDate,
                    EndDate = e.SeasonEndDate
                }
            };

            if (!string.IsNullOrWhiteSpace(e.ExtraServices))
            {
                var serviceNames = e.ExtraServices
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim());

                foreach (var serviceName in serviceNames)
                {
                    var service = CreateExtraServiceByName(serviceName);
                    if (service != null)
                        trip.ExtraServices.Add(service);
                }
            }

            return trip;
        }

        private static IExtraService? CreateExtraServiceByName(string serviceName)
        {
            return serviceName switch
            {
                "Breakfast" => new Breakfast(),
                "Guide" => new Guide(),
                "Insurance" => new Insurance(),
                "AirportTransfer" => new AirportTransfer(),
                "TravelInsurance" => new TravelInsurance(),
                "TourGuide" => new TourGuide(),
                "FreeCancellation" => new FreeCancellation(),
                _ => null
            };
        }
    }
}