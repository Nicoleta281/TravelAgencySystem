using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using TravelAgency.Core.Data.Entities;

namespace TravelAgency.Core.Data.Repositories
{
    public class EfClientPackageFavoriteRepository : IClientPackageFavoriteRepository
    {
        public IReadOnlyList<int> GetFavoriteTripPackageIds(string clientUsername)
        {
            var u = (clientUsername ?? "").Trim();
            if (u.Length == 0)
                return Array.Empty<int>();

            using var db = TravelAgencyDbContextFactory.Create();
            return db.ClientPackageFavorites
                .AsNoTracking()
                .Where(f => f.ClientUsername == u)
                .OrderByDescending(f => f.SavedAtUtc)
                .Select(f => f.TripPackageId)
                .ToList();
        }

        public bool IsFavorite(string clientUsername, int tripPackageId)
        {
            var u = (clientUsername ?? "").Trim();
            if (u.Length == 0 || tripPackageId <= 0)
                return false;

            using var db = TravelAgencyDbContextFactory.Create();
            return db.ClientPackageFavorites.Any(f => f.ClientUsername == u && f.TripPackageId == tripPackageId);
        }

        public void Add(string clientUsername, int tripPackageId)
        {
            var u = (clientUsername ?? "").Trim();
            if (u.Length == 0 || tripPackageId <= 0)
                return;

            using var db = TravelAgencyDbContextFactory.Create();
            if (db.ClientPackageFavorites.Any(f => f.ClientUsername == u && f.TripPackageId == tripPackageId))
                return;

            db.ClientPackageFavorites.Add(new ClientPackageFavoriteEntity
            {
                ClientUsername = u,
                TripPackageId = tripPackageId,
                SavedAtUtc = DateTime.UtcNow
            });
            db.SaveChanges();
        }

        public void Remove(string clientUsername, int tripPackageId)
        {
            var u = (clientUsername ?? "").Trim();
            if (u.Length == 0 || tripPackageId <= 0)
                return;

            using var db = TravelAgencyDbContextFactory.Create();
            var row = db.ClientPackageFavorites
                .FirstOrDefault(f => f.ClientUsername == u && f.TripPackageId == tripPackageId);
            if (row == null)
                return;

            db.ClientPackageFavorites.Remove(row);
            db.SaveChanges();
        }
    }
}
