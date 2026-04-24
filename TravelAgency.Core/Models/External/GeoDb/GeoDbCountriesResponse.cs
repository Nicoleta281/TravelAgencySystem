using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TravelAgency.Core.Models.External.GeoDb
{
    public class GeoDbCountriesResponse
    {
        [JsonPropertyName("data")]
        public List<GeoDbCountry>? Data { get; set; }
    }

    public class GeoDbCountry
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}

