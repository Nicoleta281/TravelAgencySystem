using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using TravelAgency.Core.Interfaces;
using TravelAgency.Core.Models.External.GeoDb;
using TravelAgency.Core.Models.Locations;

namespace TravelAgency.Core.Patterns.Adapters.GeoDb
{
    public class GeoDbLocationAdapter : ILocationSearchProvider
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly string _host;

        public GeoDbLocationAdapter()
        {
            _apiKey = Environment.GetEnvironmentVariable("RAPIDAPI_KEY");
            if (string.IsNullOrWhiteSpace(_apiKey))
                _apiKey = Environment.GetEnvironmentVariable("RAPIDAPI_KEY", EnvironmentVariableTarget.User);

            if (string.IsNullOrWhiteSpace(_apiKey))
                _apiKey = Environment.GetEnvironmentVariable("RAPIDAPI_KEY", EnvironmentVariableTarget.Machine);

            if (string.IsNullOrWhiteSpace(_apiKey))
                throw new InvalidOperationException("Missing environment variable: RAPIDAPI_KEY");

            _baseUrl = ConfigurationManager.AppSettings["GeoDb.BaseUrl"]
                ?? "https://wft-geo-db.p.rapidapi.com/v1/geo/cities";

            _host = ConfigurationManager.AppSettings["GeoDb.Host"]
                ?? "wft-geo-db.p.rapidapi.com";
        }

        public async Task<List<LocationOption>> SearchLocationsAsync(string query, int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<LocationOption>();

            string url =
                $"{_baseUrl}" +
                $"?namePrefix={Uri.EscapeDataString(query)}" +
                $"&limit={limit}" +
                $"&sort=-population";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-RapidAPI-Key", _apiKey);
            request.Headers.Add("X-RapidAPI-Host", _host);

            using var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"HTTP {(int)response.StatusCode} - {response.ReasonPhrase}\n\n{json}");
            }

            var result = JsonSerializer.Deserialize<GeoDbCitiesResponse>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (result?.Data == null)
                return new List<LocationOption>();

            var locations = new List<LocationOption>();

            foreach (var city in result.Data)
            {
                if (string.IsNullOrWhiteSpace(city.City) || string.IsNullOrWhiteSpace(city.Country))
                    continue;

                locations.Add(new LocationOption
                {
                    City = city.City ?? string.Empty,
                    Country = city.Country ?? string.Empty,
                    CountryCode = city.CountryCode ?? string.Empty
                });
            }

            return locations;
        }
    }
}