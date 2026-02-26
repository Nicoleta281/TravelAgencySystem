using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using TravelAgency.Core.Data.Mappers;
using TravelAgency.Core.Models.TripPkg.Package;

namespace TravelAgency.Core.Data.Repositories
{
    public class EfTripPackageRepository : ITripPackageRepository
    {
        public IReadOnlyList<TripPackage> GetAll()
        {
            using var db = TravelAgencyDbContextFactory.Create();
            var entities = db.TripPackages.AsNoTracking().ToList();
            return entities.Select(TripPackageMapper.FromEntity).ToList();
        }

        public TripPackage Add(TripPackage trip)
        {
            using var db = TravelAgencyDbContextFactory.Create();
            db.TripPackages.Add(TripPackageMapper.ToEntity(trip));
            db.SaveChanges();
            return trip;
        }
    }
}