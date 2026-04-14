using System.Collections.Generic;
using System.Linq;
using TravelAgency.Core.Data.Entities;

namespace TravelAgency.Core.Data.Repositories
{
    public class EfAdminAnalyticsSnapshotRepository : IAdminAnalyticsSnapshotRepository
    {
        private readonly TravelAgencyDbContext _dbContext;

        public EfAdminAnalyticsSnapshotRepository(TravelAgencyDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<AdminAnalyticsSnapshotEntity> GetAll()
        {
            return _dbContext.AdminAnalyticsSnapshots
                .OrderByDescending(x => x.SavedAt)
                .ToList();
        }

        public void Add(AdminAnalyticsSnapshotEntity entity)
        {
            _dbContext.AdminAnalyticsSnapshots.Add(entity);
            _dbContext.SaveChanges();
        }

        public void Delete(int id)
        {
            var entity = _dbContext.AdminAnalyticsSnapshots.FirstOrDefault(x => x.Id == id);
            if (entity == null)
                return;

            _dbContext.AdminAnalyticsSnapshots.Remove(entity);
            _dbContext.SaveChanges();
        }
    }
}