using System.ComponentModel.DataAnnotations;
using TravelAgency.Core.Interfaces;
using TravelAgency.Core.Models.TripPkg.Package;
using FluentValidation;
using FluentValidation.Results;

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
            var validator = new TripPackageValidator();
            var result = validator.Validate(_trip);

            if (!result.IsValid)
            {
                throw new InvalidOperationException(result.Errors[0].ErrorMessage);
            }
            return _trip;
        }
    }
}

