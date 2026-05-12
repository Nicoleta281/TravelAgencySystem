using System.Collections.Generic;

namespace TravelAgency.Core.Data.Repositories
{
    public interface IClientPackageFavoriteRepository
    {
        IReadOnlyList<int> GetFavoriteTripPackageIds(string clientUsername);

        bool IsFavorite(string clientUsername, int tripPackageId);

        void Add(string clientUsername, int tripPackageId);

        void Remove(string clientUsername, int tripPackageId);
    }
}
