using TravelAgency.Core.Interfaces;
using TravelAgency.Core.Models.TripPkg.Package;

namespace TravelAgency.Core.Builders
{
    public class TripPackageBuilder : ITripPackageBuilder
    {
        private TripPackage _trip = new TripPackage();

        public void Reset()
        {
            _trip = new TripPackage();
        }

        public void SetName(string name) => _trip.Name = name;
        public void SetPrice(double price) => _trip.Price = price;
        public void SetSeason(Season season) => _trip.Season = season;
        public void SetTransport(ITransport transport) => _trip.Transport = transport;
        public void SetStay(IStay stay) => _trip.Stay = stay;

        public void AddDay(TripDay day) => _trip.AddDay(day);
        public void AddExtraService(IExtraService service) => _trip.AddExtraService(service);

        // ===== Fluent API (pentru chain în WPF) =====
        public TripPackageBuilder WithName(string name) { SetName(name); return this; }
        public TripPackageBuilder WithPrice(double price) { SetPrice(price); return this; }
        public TripPackageBuilder WithSeason(Season season) { SetSeason(season); return this; }
        public TripPackageBuilder WithTransport(ITransport transport) { SetTransport(transport); return this; }
        public TripPackageBuilder WithStay(IStay stay) { SetStay(stay); return this; }
        public TripPackageBuilder WithDay(TripDay day) { AddDay(day); return this; }
        public TripPackageBuilder WithExtra(IExtraService service) { AddExtraService(service); return this; }

        public TripPackage Build()
        {
            if (string.IsNullOrWhiteSpace(_trip.Name))
                throw new InvalidOperationException("TripPackage trebuie să aibă Name.");

            if (_trip.Price <= 0)
                throw new InvalidOperationException("TripPackage trebuie să aibă Price > 0.");

            if (_trip.Transport == null)
                throw new InvalidOperationException("TripPackage trebuie să aibă Transport.");

            if (_trip.Stay == null)
                throw new InvalidOperationException("TripPackage trebuie să aibă Stay.");

            if (_trip.Days == null || _trip.Days.Count == 0)
                throw new InvalidOperationException("TripPackage trebuie să aibă cel puțin o zi.");

            return _trip;
        }
    }
}

