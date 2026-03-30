using TravelAgency.Core.Interfaces;
using TravelAgency.Core.Prototypes;
using TravelAgency.Core.Patterns.Composite;

namespace TravelAgency.Core.Models.TripPkg.Package
{
    public class TripPackage : IPrototype<TripPackage>
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public double Price { get; set; }

        public string Destination { get; set; } = "";
        public string Country { get; set; } = "";
        public string DepartureCity { get; set; } = "";
        public string AccommodationName { get; set; } = "";
        public string MealPlan { get; set; } = "";
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


        // ===== Helpers pentru UI =====

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