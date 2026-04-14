using System.Collections.Generic;
using System.Linq;

namespace TravelAgency.Core.Patterns.Memento
{
    public class AdminAnalyticsHistory
    {
        private readonly List<AdminAnalyticsMemento> _snapshots = new();

        public void AddSnapshot(AdminAnalyticsMemento memento)
        {
            _snapshots.Insert(0, memento);
        }

        public IReadOnlyList<AdminAnalyticsMemento> GetSnapshots()
        {
            return _snapshots.AsReadOnly();
        }

        public void RemoveSnapshot(AdminAnalyticsMemento memento)
        {
            _snapshots.Remove(memento);
        }
    }
}