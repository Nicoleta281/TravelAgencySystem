using System;
using System.Collections.ObjectModel;
using TravelAgency.Core.Models.TripPkg.Package;

namespace TravelAgency.Core.Singletons
{
    public sealed class TripRepository
    {
        private static readonly Lazy<TripRepository> _instance =
            new Lazy<TripRepository>(() => new TripRepository());

        public static TripRepository Instance => _instance.Value;

        private TripRepository() { }

        public ObservableCollection<TripPackage> Trips { get; } = new();

        public void Add(TripPackage trip)
        {
            if (trip == null) throw new ArgumentNullException(nameof(trip));
            Trips.Add(trip);
        }
    }
}