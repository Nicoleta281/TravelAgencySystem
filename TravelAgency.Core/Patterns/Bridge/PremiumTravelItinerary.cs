using TravelAgency.Core.Interfaces;

namespace TravelAgency.Core.Patterns.Bridge
{
    public sealed class PremiumTravelItinerary : TravelItineraryBase
    {
        public PremiumTravelItinerary(string name, ITransport transport, IStay stay)
            : base(name, transport, stay)
        {
        }

        public override string GetSummary()
        {
            return $"{Name} (premium): priority boarding + {Transport.GetType().Name} + {Stay.GetType().Name}";
        }
    }
}

