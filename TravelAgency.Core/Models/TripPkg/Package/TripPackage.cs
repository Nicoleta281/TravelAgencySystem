using System;
using System.Collections.Generic;
using System.Linq;
using TravelAgency.Core.Interfaces;
using TravelAgency.Core.Patterns.Composite;
using TravelAgency.Core.Patterns.Flyweight;
using TravelAgency.Core.Patterns.Prototypes;

namespace TravelAgency.Core.Models.TripPkg.Package
{
    public class TripPackage : IPrototype<TripPackage>
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public double Price { get; set; }

        // UI fields (persisted in DB)
        public string TripType { get; set; } = "";
        public string Category { get; set; } = "";
        public string ShortDescription { get; set; } = "";
        public string PricingNotes { get; set; } = "";
        public double BasePrice { get; set; }

        // Flyweight
        public PackageSharedInfo? SharedInfo { get; set; }

        public string Destination => SharedInfo?.Destination ?? "";
        public string Country => SharedInfo?.Country ?? "";
        public string DepartureCity => SharedInfo?.DepartureCity ?? "";
        public string AccommodationName => SharedInfo?.AccommodationName ?? "";
        public string MealPlan => SharedInfo?.MealPlan ?? "";

        public int AvailableSeats { get; set; }

        public double DiscountPercent { get; set; }
        public double VatPercent { get; set; }
        public double ExtraCharges { get; set; }

        public Season? Season { get; set; }

        public List<TripDay> Days { get; set; } = new();

        public ITransport? Transport { get; set; }
        public IStay? Stay { get; set; }
        public List<IExtraService> ExtraServices { get; set; } = new();

        public string TransportDisplayName { get; set; } = "";
        public string StayDisplayName { get; set; } = "";
        public IExtraServiceComponent? ExtraServiceBundle { get; set; }

        public int DaysCount
        {
            get
            {
                if (Season == null)
                    return 0;

                return (Season.EndDate.Date - Season.StartDate.Date).Days + 1;
            }
        }

        public string TransportName =>
            Transport != null
                ? Transport.GetType().Name
                : !string.IsNullOrWhiteSpace(TransportDisplayName)
                    ? TransportDisplayName
                    : "N/A";

        public string StayName =>
            Stay != null
                ? Stay.GetType().Name
                : !string.IsNullOrWhiteSpace(StayDisplayName)
                    ? StayDisplayName
                    : "N/A";

        public IEnumerable<string> ExtraServiceNames =>
            ExtraServices?.Select(x => x.GetType().Name) ?? Enumerable.Empty<string>();

        public void AddDay(TripDay day)
        {
            if (day == null)
                throw new ArgumentNullException(nameof(day));

            Days.Add(day);
        }

        public void AddExtraService(IExtraService service)
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            ExtraServices.Add(service);
        }

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

            // Flyweight ramane shared intentionat
            copy.SharedInfo = SharedInfo;

            // Transport/Stay raman shallow
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