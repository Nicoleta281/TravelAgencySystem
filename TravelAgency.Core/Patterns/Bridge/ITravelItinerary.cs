using TravelAgency.Core.Interfaces;

namespace TravelAgency.Core.Patterns.Bridge
{
    public interface ITravelItinerary
    {
        ITransport Transport { get; }
        IStay Stay { get; }

        string Name { get; }
        string GetSummary();
        void Execute();
    }
}

