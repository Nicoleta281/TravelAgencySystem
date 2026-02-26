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
            return new TripPackageEntity
            {
                Name = trip.Name,
                Price = trip.Price,

                SeasonName = trip.Season?.Name ?? "",
                SeasonStartDate = DateTime.SpecifyKind(trip.Season.StartDate, DateTimeKind.Utc),
                SeasonEndDate = DateTime.SpecifyKind(trip.Season.EndDate, DateTimeKind.Utc),

                TransportType = trip.Transport?.GetType().Name ?? "",
                StayType = trip.Stay?.GetType().Name ?? "",
                ExtraServices = string.Join(",", trip.ExtraServices.Select(x => x.GetType().Name))
            };
        }

        public static TripPackage FromEntity(TripPackageEntity e)
        {
            return new TripPackage
            {
                Name = e.Name,
                Price = e.Price,
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