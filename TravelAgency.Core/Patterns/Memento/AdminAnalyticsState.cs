using TravelAgency.Core.Patterns.Memento;

namespace TravelAgency.Core.Models.Analytics
{
    public class AdminAnalyticsState
    {
        public int TotalBookings { get; set; }
        public int ConfirmedBookings { get; set; }
        public int RejectedBookings { get; set; }
        public double TotalRevenue { get; set; }
        public int TotalUsers { get; set; }
        public string TopDestination { get; set; } = "";

        public AdminAnalyticsMemento Save()
        {
            return new AdminAnalyticsMemento(
                TotalBookings,
                ConfirmedBookings,
                RejectedBookings,
                TotalRevenue,
                TotalUsers,
                TopDestination
            );
        }

        public void Restore(AdminAnalyticsMemento memento)
        {
            TotalBookings = memento.TotalBookings;
            ConfirmedBookings = memento.ConfirmedBookings;
            RejectedBookings = memento.RejectedBookings;
            TotalRevenue = memento.TotalRevenue;
            TotalUsers = memento.TotalUsers;
            TopDestination = memento.TopDestination;
        }
    }
}