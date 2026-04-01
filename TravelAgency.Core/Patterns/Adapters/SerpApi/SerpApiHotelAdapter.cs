using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using TravelAgency.Core.Interfaces;
using TravelAgency.Core.Models.External.SerpApi;
using TravelAgency.Core.Models.Locations;

namespace TravelAgency.Core.Patterns.Adapters.SerpApi
{
    public class SerpApiHotelAdapter : IHotelSearchProvider
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        private readonly string _apiKey;
        private readonly string _baseUrl;

        public SerpApiHotelAdapter()
        {
            // Some runs (e.g. launching from an IDE/debugger) may not inherit updated
            // Windows environment variables into the current process.
            _apiKey = Environment.GetEnvironmentVariable("SERPAPI_API_KEY");
            if (string.IsNullOrWhiteSpace(_apiKey))
                _apiKey = Environment.GetEnvironmentVariable(
                    "SERPAPI_API_KEY",
                    EnvironmentVariableTarget.User);

            if (string.IsNullOrWhiteSpace(_apiKey))
                _apiKey = Environment.GetEnvironmentVariable(
                    "SERPAPI_API_KEY",
                    EnvironmentVariableTarget.Machine);

            if (string.IsNullOrWhiteSpace(_apiKey))
                throw new InvalidOperationException("Missing environment variable: SERPAPI_API_KEY");

            _baseUrl = ConfigurationManager.AppSettings["SerpApi.BaseUrl"]
                ?? "https://serpapi.com/search.json";
        }

        public async Task<List<HotelSearchOption>> SearchHotelsAsync(
            string destination,
            DateTime checkInDate,
            DateTime checkOutDate,
            int adults)
        {
            if (string.IsNullOrWhiteSpace(destination))
                throw new ArgumentException("Destination is required.", nameof(destination));

            if (checkOutDate <= checkInDate)
                throw new ArgumentException("Check-out date must be later than check-in date.");

            if (adults <= 0)
                throw new ArgumentException("Adults must be greater than 0.", nameof(adults));

            string url =
                $"{_baseUrl}" +
                $"?engine=google_hotels" +
                $"&q={Uri.EscapeDataString(destination)}" +
                $"&check_in_date={checkInDate:yyyy-MM-dd}" +
                $"&check_out_date={checkOutDate:yyyy-MM-dd}" +
                $"&adults={adults}" +
                $"&currency=EUR" +
                $"&hl=ro" +
                $"&gl=fr" +
                $"&api_key={_apiKey}";

            var response = await _httpClient.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"HTTP {(int)response.StatusCode} - {response.ReasonPhrase}\n\n{json}");
            }

            var result = JsonSerializer.Deserialize<SerpApiHotelSearchResponse>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (result?.Properties == null)
                return new List<HotelSearchOption>();

            var hotels = new List<HotelSearchOption>();

            foreach (var property in result.Properties)
            {
                hotels.Add(new HotelSearchOption
                {
                    Name = property.Name ?? string.Empty,
                    Description = property.Description ?? string.Empty,
                    Link = property.Link ?? string.Empty,
                    PropertyToken = property.PropertyToken ?? string.Empty,
                    Latitude = property.GpsCoordinates?.Latitude ?? 0,
                    Longitude = property.GpsCoordinates?.Longitude ?? 0,
                    PricePerNight = property.RatePerNight?.ExtractedLowest,
                    TotalPrice = property.TotalRate?.ExtractedLowest,
                    HotelClass = property.ExtractedHotelClass,
                    ThumbnailUrl = property.Images != null && property.Images.Count > 0
                        ? property.Images[0].Thumbnail
                        : string.Empty
                });
            }

            return hotels;
        }
    }
}