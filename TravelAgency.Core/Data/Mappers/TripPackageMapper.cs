using System;
using System.Linq;
using TravelAgency.Core.Data.Entities;
using TravelAgency.Core.Models.TripPkg.Package;

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

                SeasonName = season.Name,
                SeasonStartDate = DateTime.SpecifyKind(season.StartDate, DateTimeKind.Utc),
                SeasonEndDate = DateTime.SpecifyKind(season.EndDate, DateTimeKind.Utc),

                TransportType = trip.Transport?.GetType().Name
                ?? trip.TransportDisplayName
                ?? "",
                StayType = trip.Stay?.GetType().Name
           ?? trip.StayDisplayName
           ?? "",
                ExtraServices = string.Join(",", trip.ExtraServices.Select(x => x.GetType().Name))
            };
        }

        public static TripPackage FromEntity(TripPackageEntity e)
        {
            return new TripPackage
            {
                Id = e.Id,
                Name = e.Name,
                Price = e.Price,
                TransportDisplayName = e.TransportType ?? "",
                StayDisplayName = e.StayType ?? "",
                Season = new Season
                {
                    Name = e.SeasonName,
                    StartDate = e.SeasonStartDate,
                    EndDate = e.SeasonEndDate
                }
            };
        }
    }
}