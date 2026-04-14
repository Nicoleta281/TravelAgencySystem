using System;

namespace TravelAgency.Core.Patterns.Memento
{
    public class AdminAnalyticsMemento
    {
        public int TotalBookings { get; }
        public int ConfirmedBookings { get; }
        public int RejectedBookings { get; }
        public double TotalRevenue { get; }
        public int TotalUsers { get; }
        public string TopDestination { get; }
        public DateTime SavedAt { get; }

        public AdminAnalyticsMemento(
            int totalBookings,
            int confirmedBookings,
            int rejectedBookings,
            double totalRevenue,
            int totalUsers,
            string topDestination,
            DateTime? savedAt = null)
        {
            TotalBookings = totalBookings;
            ConfirmedBookings = confirmedBookings;
            RejectedBookings = rejectedBookings;
            TotalRevenue = totalRevenue;
            TotalUsers = totalUsers;
            TopDestination = topDestination;
            SavedAt = savedAt ?? DateTime.UtcNow;

        }
        public DateTime SavedAtLocal => SavedAt.ToLocalTime();
    }
}