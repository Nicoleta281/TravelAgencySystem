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

        public void Delete(int id)
        {
            using var db = TravelAgencyDbContextFactory.Create();
            var entity = db.TripPackages.FirstOrDefault(x => x.Id == id);
            if (entity == null)
                return;

            db.TripPackages.Remove(entity);
            db.SaveChanges();

        }

        public  void Update (TripPackage trip)
        {
            using var db = TravelAgencyDbContextFactory.Create();

            var entity = db.TripPackages.FirstOrDefault(x => x.Id == trip.Id);

            if (entity == null)
                return;

            var mapped =TripPackageMapper.ToEntity(trip);

            entity.Name = mapped.Name;
            entity.Price = mapped.Price;
            entity.SeasonName = mapped.SeasonName;
            entity.SeasonStartDate = mapped.SeasonStartDate;
            entity.SeasonEndDate = mapped.SeasonEndDate;
            entity.TransportType= mapped.TransportType;
            entity.ExtraServices = mapped.ExtraServices;

            db.SaveChanges ();
        }

    }
}