using System;
using TravelAgency.Core.Interfaces;

namespace TravelAgency.Core.Patterns.Bridge
{
    public abstract class TravelItineraryBase : ITravelItinerary
    {
        protected TravelItineraryBase(string name, ITransport transport, IStay stay)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Trip" : name.Trim();
            Transport = transport ?? throw new ArgumentNullException(nameof(transport));
            Stay = stay ?? throw new ArgumentNullException(nameof(stay));
        }

        public string Name { get; }
        public ITransport Transport { get; }
        public IStay Stay { get; }

        public virtual void Execute()
        {
            Transport.Travel();
            Stay.CheckIn();
        }

        public abstract string GetSummary();
    }
}

