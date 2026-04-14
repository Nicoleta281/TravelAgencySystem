using System.Collections.Generic;
using TravelAgency.Core.Data.Entities;

namespace TravelAgency.Core.Data.Repositories
{
    public interface IAdminAnalyticsSnapshotRepository
    {
        List<AdminAnalyticsSnapshotEntity> GetAll();
        void Add(AdminAnalyticsSnapshotEntity entity);
        void Delete(int id);
    }
}