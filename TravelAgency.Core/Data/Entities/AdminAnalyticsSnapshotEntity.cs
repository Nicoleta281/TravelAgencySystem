using System;

namespace TravelAgency.Core.Data.Entities
{
    public class AdminAnalyticsSnapshotEntity
    {
        public int Id { get; set; }
        public DateTime SavedAt { get; set; }

        public int TotalBookings { get; set; }
        public int ConfirmedBookings { get; set; }
        public int RejectedBookings { get; set; }

        public double TotalRevenue { get; set; }

        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int BlockedUsers { get; set; }

        public string TopDestination { get; set; } = "";
    }
}