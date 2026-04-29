namespace TravelAgency.Core.Patterns.Adapters.SerpApi
{
    public sealed class SerpApiOptions
    {
        public string ApiKey { get; init; } = "";
        public string BaseUrl { get; init; } = "https://serpapi.com/search.json";

        public string Currency { get; init; } = "EUR";
        public string Language { get; init; } = "ro";
        public string Country { get; init; } = "fr";
    }
}

