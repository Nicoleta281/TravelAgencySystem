using System;
using TravelAgency.Core.Patterns.Factories.AbstractFactory;

namespace TravelAgency.Core.Services
{
    public sealed class TripComponentFactorySelector
    {
        public ITripComponentFactory Select(string? tripType)
        {
            var normalized = (tripType ?? string.Empty).Trim();

            if (string.Equals(normalized, "Premium", StringComparison.OrdinalIgnoreCase))
                return new PremiumTripFactory();

            return new BudgetTripFactory();
        }
    }
}
