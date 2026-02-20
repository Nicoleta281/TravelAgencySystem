using TravelAgency.Core.Prototypes;

namespace TravelAgency.Core.Models.TripPkg.Package
{
    public class Activity : IPrototype<Activity>
    {
        public string Name { get; set; } = "";
        public string? Description { get; set; }

        public Activity ShallowClone() => (Activity)this.MemberwiseClone();

        public Activity DeepClone() => (Activity)this.MemberwiseClone();
    }
}
