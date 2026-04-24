using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TravelAgency.Core.Interfaces;
using TravelAgency.Core.Models.External.GeoDb;
using TravelAgency.Core.Models.Locations;

namespace TravelAgency.Core.Patterns.Adapters.GeoDb
{
    public class GeoDbLocationAdapter : ILocationSearchProvider
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly SemaphoreSlim _geoDbHttpGate = new SemaphoreSlim(1, 1);
        private static DateTimeOffset _lastGeoDbRequestUtc = DateTimeOffset.MinValue;

        private readonly string _apiKey;
        private readonly string _citiesUrl;
        private readonly string _countriesUrl;
        private readonly string _host;

        public GeoDbLocationAdapter()
        {
            _apiKey = Environment.GetEnvironmentVariable("RAPIDAPI_KEY") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(_apiKey))
                _apiKey = Environment.GetEnvironmentVariable("RAPIDAPI_KEY", EnvironmentVariableTarget.User) ?? string.Empty;

            if (string.IsNullOrWhiteSpace(_apiKey))
                _apiKey = Environment.GetEnvironmentVariable("RAPIDAPI_KEY", EnvironmentVariableTarget.Machine) ?? string.Empty;

            if (string.IsNullOrWhiteSpace(_apiKey))
                throw new InvalidOperationException("Missing environment variable: RAPIDAPI_KEY");

            _citiesUrl = ConfigurationManager.AppSettings["GeoDb.BaseUrl"]
                ?? "https://wft-geo-db.p.rapidapi.com/v1/geo/cities";

            var root =
                _citiesUrl.EndsWith("/cities", StringComparison.OrdinalIgnoreCase)
                    ? _citiesUrl[..^"/cities".Length]
                    : "https://wft-geo-db.p.rapidapi.com/v1/geo";

            _countriesUrl = ConfigurationManager.AppSettings["GeoDb.CountriesUrl"]
                ?? $"{root}/countries";

            _host = ConfigurationManager.AppSettings["GeoDb.Host"]
                ?? "wft-geo-db.p.rapidapi.com";
        }

        private static int GetMinRequestIntervalMs()
        {
            var raw = ConfigurationManager.AppSettings["GeoDb.MinRequestIntervalMs"];
            if (int.TryParse(raw, out int ms) && ms is >= 0 and <= 5000)
                return ms;

            // RapidAPI BASIC plans are often strict on requests/sec; pacing avoids HTTP 429 bursts.
            return 450;
        }

        private static async Task<string> SendGeoDbGetAsync(string url, string apiKey, string host)
        {
            const int maxAttempts = 4;

            await _geoDbHttpGate.WaitAsync();
            try
            {
                for (int attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    var minInterval = TimeSpan.FromMilliseconds(GetMinRequestIntervalMs());
                    var now = DateTimeOffset.UtcNow;
                    var elapsed = now - _lastGeoDbRequestUtc;
                    if (_lastGeoDbRequestUtc != DateTimeOffset.MinValue && elapsed < minInterval)
                    {
                        await Task.Delay(minInterval - elapsed);
                    }

                    using var request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Headers.Add("X-RapidAPI-Key", apiKey);
                    request.Headers.Add("X-RapidAPI-Host", host);

                    using var response = await _httpClient.SendAsync(request);
                    var json = await response.Content.ReadAsStringAsync();
                    _lastGeoDbRequestUtc = DateTimeOffset.UtcNow;

                    if (response.StatusCode == HttpStatusCode.TooManyRequests && attempt < maxAttempts)
                    {
                        var retryAfterMs = 750 * attempt;
                        if (response.Headers.RetryAfter?.Delta is TimeSpan delta && delta > TimeSpan.Zero)
                            retryAfterMs = Math.Max(retryAfterMs, (int)Math.Ceiling(delta.TotalMilliseconds));

                        // Small jitter helps when multiple clients hit the same limiter.
                        retryAfterMs += Random.Shared.Next(50, 200);
                        await Task.Delay(retryAfterMs);
                        continue;
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception(
                            $"HTTP {(int)response.StatusCode} - {response.ReasonPhrase}\n\n{json}");
                    }

                    return json;
                }

                throw new Exception("HTTP 429 - Too Many Requests\n\nGeoDB rate limit retries exhausted.");
            }
            finally
            {
                _geoDbHttpGate.Release();
            }
        }

        public async Task<List<LocationOption>> SearchLocationsAsync(string query, int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<LocationOption>();

            string trimmedQuery = query.Trim();
            string url =
                $"{_citiesUrl}" +
                $"?namePrefix={Uri.EscapeDataString(trimmedQuery)}" +
                $"&types=CITY" +
                $"&limit={limit}" +
                $"&sort=-population";

            var json = await SendGeoDbGetAsync(url, _apiKey, _host);

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
                var placeName = !string.IsNullOrWhiteSpace(city.City) ? city.City : city.Name;
                if (string.IsNullOrWhiteSpace(placeName) || string.IsNullOrWhiteSpace(city.Country))
                    continue;

                locations.Add(new LocationOption
                {
                    City = placeName ?? string.Empty,
                    Country = city.Country ?? string.Empty,
                    CountryCode = city.CountryCode ?? string.Empty
                });
            }

            // GeoDb can return administrative units (e.g., "* Parish", arrondissements) that are
            // not meaningful as travel destinations. We apply a small heuristic so that a query
            // like "paris" prioritizes "Paris, France" over "* Parish" results.
            string q = trimmedQuery.ToLowerInvariant();
            bool queryMentionsParish = q.Contains("parish");
            bool queryMentionsArrondissement = q.Contains("arrondissement");

            var filtered = locations
                .Where(l =>
                {
                    var cityName = (l.City ?? string.Empty).ToLowerInvariant();
                    if (!queryMentionsParish && cityName.Contains("parish"))
                        return false;
                    if (!queryMentionsArrondissement && cityName.Contains("arrondissement"))
                        return false;
                    return true;
                })
                .GroupBy(l => $"{l.City}|{l.Country}")
                .Select(g => g.First())
                .OrderByDescending(l => string.Equals(l.City, trimmedQuery, StringComparison.OrdinalIgnoreCase))
                .ThenBy(l => l.City)
                .ToList();

            return filtered;
        }

        public async Task<List<CountryOption>> SearchCountriesAsync(string query, int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<CountryOption>();

            string trimmedQuery = query.Trim();
            string url =
                $"{_countriesUrl}" +
                $"?namePrefix={Uri.EscapeDataString(trimmedQuery)}" +
                $"&limit={limit}";

            var json = await SendGeoDbGetAsync(url, _apiKey, _host);

            var result = JsonSerializer.Deserialize<GeoDbCountriesResponse>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (result?.Data == null)
                return new List<CountryOption>();

            return result.Data
                .Where(c => !string.IsNullOrWhiteSpace(c.Name) && !string.IsNullOrWhiteSpace(c.Code))
                .Select(c => new CountryOption
                {
                    Name = c.Name ?? string.Empty,
                    Code = c.Code ?? string.Empty
                })
                .GroupBy(c => c.Code, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderByDescending(c => string.Equals(c.Name, trimmedQuery, StringComparison.OrdinalIgnoreCase))
                .ThenBy(c => c.Name)
                .ToList();
        }

        public async Task<List<LocationOption>> GetCitiesByCountryCodeAsync(string countryCode, int limit = 20)
        {
            if (string.IsNullOrWhiteSpace(countryCode))
                return new List<LocationOption>();

            string code = countryCode.Trim();
            string url =
                $"{_citiesUrl}" +
                $"?countryIds={Uri.EscapeDataString(code)}" +
                $"&types=CITY" +
                $"&limit={limit}" +
                $"&sort=-population";

            var json = await SendGeoDbGetAsync(url, _apiKey, _host);

            var result = JsonSerializer.Deserialize<GeoDbCitiesResponse>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (result?.Data == null)
                return new List<LocationOption>();

            return result.Data
                .Select(city =>
                {
                    var placeName = !string.IsNullOrWhiteSpace(city.City) ? city.City : city.Name;
                    return new LocationOption
                    {
                        City = placeName ?? string.Empty,
                        Country = city.Country ?? string.Empty,
                        CountryCode = city.CountryCode ?? string.Empty
                    };
                })
                .Where(l => !string.IsNullOrWhiteSpace(l.City))
                .GroupBy(l => $"{l.City}|{l.Country}", StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(l => l.City)
                .ToList();
        }
    }
}