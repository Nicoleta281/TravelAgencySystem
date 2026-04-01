using System.Collections.Generic;

namespace TravelAgency.Core.Patterns.Flyweight
{
    public class PackageSharedInfoFactory
    {
        private readonly Dictionary<string, PackageSharedInfo> _cache = new();

        public PackageSharedInfo GetOrCreate(
            string destination,
            string country,
            string departureCity,
            string accommodationName,
            string mealPlan,
            string transportType,
            string stayType)
        {
            string key = BuildKey(
                destination,
                country,
                departureCity,
                accommodationName,
                mealPlan,
                transportType,
                stayType);

            if (!_cache.TryGetValue(key, out var sharedInfo))
            {
                sharedInfo = new PackageSharedInfo(
                    destination,
                    country,
                    departureCity,
                    accommodationName,
                    mealPlan,
                    transportType,
                    stayType);

                _cache[key] = sharedInfo;
            }

            return sharedInfo;
        }

        public int CachedObjectsCount => _cache.Count;

        private string BuildKey(
            string destination,
            string country,
            string departureCity,
            string accommodationName,
            string mealPlan,
            string transportType,
            string stayType)
        {
            return Normalize(destination) + "|" +
                   Normalize(country) + "|" +
                   Normalize(departureCity) + "|" +
                   Normalize(accommodationName) + "|" +
                   Normalize(mealPlan) + "|" +
                   Normalize(transportType) + "|" +
                   Normalize(stayType);
        }

        private string Normalize(string value)
        {
            return (value ?? "").Trim().ToLowerInvariant();
        }
    }
}