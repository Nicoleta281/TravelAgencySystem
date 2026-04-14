using TravelAgency.Core.Data.Entities;
using TravelAgency.Core.Patterns.Memento;

namespace TravelAgency.Core.Data.Mappers
{
    public static class AdminAnalyticsSnapshotMapper
    {
        public static AdminAnalyticsSnapshotEntity ToEntity(
            AdminAnalyticsMemento memento,
            int activeUsers,
            int blockedUsers)
        {
            return new AdminAnalyticsSnapshotEntity
            {
                SavedAt = memento.SavedAt,
                TotalBookings = memento.TotalBookings,
                ConfirmedBookings = memento.ConfirmedBookings,
                RejectedBookings = memento.RejectedBookings,
                TotalRevenue = memento.TotalRevenue,
                TotalUsers = memento.TotalUsers,
                ActiveUsers = activeUsers,
                BlockedUsers = blockedUsers,
                TopDestination = memento.TopDestination
            };
        }

        public static AdminAnalyticsMemento ToMemento(AdminAnalyticsSnapshotEntity entity)
        {
            return new AdminAnalyticsMemento(
                entity.TotalBookings,
                entity.ConfirmedBookings,
                entity.RejectedBookings,
                entity.TotalRevenue,
                entity.TotalUsers,
                entity.TopDestination,
                entity.SavedAt
            );
        }
    }
}