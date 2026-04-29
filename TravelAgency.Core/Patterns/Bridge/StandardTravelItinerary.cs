using TravelAgency.Core.Interfaces;

namespace TravelAgency.Core.Patterns.Bridge
{
    public sealed class StandardTravelItinerary : TravelItineraryBase
    {
        public StandardTravelItinerary(string name, ITransport transport, IStay stay)
            : base(name, transport, stay)
        {
        }

        public override string GetSummary()
        {
            return $"{Name} (standard): {Transport.GetType().Name} + {Stay.GetType().Name}";
        }
    }
}

