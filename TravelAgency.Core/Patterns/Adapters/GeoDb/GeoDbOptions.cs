namespace TravelAgency.Core.Patterns.Adapters.GeoDb
{
    public sealed class GeoDbOptions
    {
        public string ApiKey { get; init; } = "";
        public string CitiesUrl { get; init; } = "https://wft-geo-db.p.rapidapi.com/v1/geo/cities";
        public string CountriesUrl { get; init; } = "https://wft-geo-db.p.rapidapi.com/v1/geo/countries";
        public string Host { get; init; } = "wft-geo-db.p.rapidapi.com";

        /// <summary>
        /// Simple pacing to reduce HTTP 429 bursts on stricter plans.
        /// </summary>
        public int MinRequestIntervalMs { get; init; } = 450;
    }
}

