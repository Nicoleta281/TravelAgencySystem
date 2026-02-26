using System.Collections.Generic;
using TravelAgency.Core.Models.TripPkg.Package;

namespace TravelAgency.Core.Data.Repositories
{
    public interface ITripPackageRepository
    {
        IReadOnlyList<TripPackage> GetAll();
        TripPackage Add(TripPackage trip);
    }
}
