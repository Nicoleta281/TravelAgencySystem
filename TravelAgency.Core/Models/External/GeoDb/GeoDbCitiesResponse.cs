using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TravelAgency.Core.Models.External.GeoDb
{
    public class GeoDbCitiesResponse
    {
        [JsonPropertyName("data")]
        public List<GeoDbCityData>? Data { get; set; }
    }

    public class GeoDbCityData
    {
        // GeoDB REST responses typically use "name" for populated places.
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        // Some responses / older fields may still use "city".
        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("countryCode")]
        public string? CountryCode { get; set; }
    }
}