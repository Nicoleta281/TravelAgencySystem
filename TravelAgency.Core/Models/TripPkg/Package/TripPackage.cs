using System;
using System.Collections.Generic;
using System.Linq;
using TravelAgency.Core.Interfaces;
using TravelAgency.Core.Prototypes;

namespace TravelAgency.Core.Models.TripPkg.Package
{
    public class TripPackage : IPrototype<TripPackage>
    {
        public string Name { get; set; } = "";
        public double Price { get; set; }
        public Season? Season { get; set; }

        public List<TripDay> Days { get; set; } = new();

        public ITransport? Transport { get; set; }
        public IStay? Stay { get; set; }
        public List<IExtraService> ExtraServices { get; set; } = new();

        // ===== Helpers pentru UI =====
        public string TransportName => Transport?.GetType().Name ?? "N/A";
        public string StayName => Stay?.GetType().Name ?? "N/A";

        public IEnumerable<string> ExtraServiceNames =>
            ExtraServices?.Select(x => x.GetType().Name) ?? Enumerable.Empty<string>();

        public void AddDay(TripDay day)
        {
            if (day == null) throw new ArgumentNullException(nameof(day));
            Days.Add(day);
        }

        public void AddExtraService(IExtraService service)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            ExtraServices.Add(service);
        }

        // ===== Prototype =====
        public TripPackage ShallowClone() => (TripPackage)MemberwiseClone();

        public TripPackage DeepClone()
        {
            var copy = (TripPackage)MemberwiseClone();

            if (Season != null)
            {
                copy.Season = new Season
                {
                    Name = Season.Name,
                    StartDate = Season.StartDate,
                    EndDate = Season.EndDate
                };
            }

            copy.ExtraServices = ExtraServices?.ToList() ?? new List<IExtraService>();
            copy.Days = Days?.ToList() ?? new List<TripDay>();

            // Transport/Stay rămân shallow (deocamdată)
            copy.Transport = Transport;
            copy.Stay = Stay;

            return copy;
        }

        public override string ToString()
        {
            var extras = ExtraServices != null && ExtraServices.Count > 0
                ? string.Join(", ", ExtraServices.Select(x => x.GetType().Name))
                : "None";

            var seasonText = Season != null
                ? $"{Season.Name} ({Season.StartDate:yyyy-MM-dd} → {Season.EndDate:yyyy-MM-dd})"
                : "No season";

            return $"{Name} | {Price:F2} | {seasonText} | {TransportName} | {StayName} | Extras: {extras}";
        }
    }
}