using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TravelAgency.Core.Models.External.SerpApi
{
    public class SerpApiHotelSearchResponse
    {
        [JsonPropertyName("properties")]
        public List<SerpApiHotelProperty> Properties { get; set; } = new();
    }

    public class SerpApiHotelProperty
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("link")]
        public string Link { get; set; } = string.Empty;

        [JsonPropertyName("property_token")]
        public string PropertyToken { get; set; } = string.Empty;

        [JsonPropertyName("gps_coordinates")]
        public SerpApiGpsCoordinates GpsCoordinates { get; set; } = new();

        [JsonPropertyName("rate_per_night")]
        public SerpApiRatePerNight RatePerNight { get; set; } = new();

        [JsonPropertyName("total_rate")]
        public SerpApiTotalRate TotalRate { get; set; } = new();

        [JsonPropertyName("extracted_hotel_class")]
        public int? ExtractedHotelClass { get; set; }

        [JsonPropertyName("images")]
        public List<SerpApiImage> Images { get; set; } = new();
    }

    public class SerpApiGpsCoordinates
    {
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }
    }

    public class SerpApiRatePerNight
    {
        [JsonPropertyName("lowest")]
        public string Lowest { get; set; } = string.Empty;

        [JsonPropertyName("extracted_lowest")]
        public decimal? ExtractedLowest { get; set; }

        [JsonPropertyName("before_taxes_fees")]
        public string BeforeTaxesFees { get; set; } = string.Empty;

        [JsonPropertyName("extracted_before_taxes_fees")]
        public decimal? ExtractedBeforeTaxesFees { get; set; }
    }

    public class SerpApiTotalRate
    {
        [JsonPropertyName("lowest")]
        public string Lowest { get; set; } = string.Empty;

        [JsonPropertyName("extracted_lowest")]
        public decimal? ExtractedLowest { get; set; }

        [JsonPropertyName("before_taxes_fees")]
        public string BeforeTaxesFees { get; set; } = string.Empty;

        [JsonPropertyName("extracted_before_taxes_fees")]
        public decimal? ExtractedBeforeTaxesFees { get; set; }
    }

    public class SerpApiImage
    {
        [JsonPropertyName("thumbnail")]
        public string Thumbnail { get; set; } = string.Empty;

        [JsonPropertyName("original_image")]
        public string OriginalImage { get; set; } = string.Empty;
    }
}