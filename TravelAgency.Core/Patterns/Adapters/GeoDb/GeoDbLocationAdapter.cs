using System;
using System.Collections.Generic;
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
        private readonly HttpClient _httpClient;
        private readonly GeoDbOptions _options;

        private readonly SemaphoreSlim _geoDbHttpGate = new SemaphoreSlim(1, 1);
        private DateTimeOffset _lastGeoDbRequestUtc = DateTimeOffset.MinValue;

        public GeoDbLocationAdapter(HttpClient httpClient, GeoDbOptions options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options ?? throw new ArgumentNullException(nameof(options));

            if (string.IsNullOrWhiteSpace(_options.ApiKey))
                throw new InvalidOperationException("Missing GeoDB (RapidAPI) API key.");
        }

        private async Task<string> SendGeoDbGetAsync(string url, CancellationToken cancellationToken)
        {
            const int maxAttempts = 4;

            await _geoDbHttpGate.WaitAsync(cancellationToken);
            try
            {
                for (int attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    var minInterval = TimeSpan.FromMilliseconds(Math.Clamp(_options.MinRequestIntervalMs, 0, 5000));
                    var now = DateTimeOffset.UtcNow;
                    var elapsed = now - _lastGeoDbRequestUtc;
                    if (_lastGeoDbRequestUtc != DateTimeOffset.MinValue && elapsed < minInterval)
                    {
                        await Task.Delay(minInterval - elapsed, cancellationToken);
                    }

                    using var request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Headers.Add("X-RapidAPI-Key", _options.ApiKey);
                    request.Headers.Add("X-RapidAPI-Host", _options.Host);

                    using var response = await _httpClient.SendAsync(request, cancellationToken);
                    var json = await response.Content.ReadAsStringAsync(cancellationToken);
                    _lastGeoDbRequestUtc = DateTimeOffset.UtcNow;

                    if (response.StatusCode == HttpStatusCode.TooManyRequests && attempt < maxAttempts)
                    {
                        var retryAfterMs = 750 * attempt;
                        if (response.Headers.RetryAfter?.Delta is TimeSpan delta && delta > TimeSpan.Zero)
                            retryAfterMs = Math.Max(retryAfterMs, (int)Math.Ceiling(delta.TotalMilliseconds));

                        // Small jitter helps when multiple clients hit the same limiter.
                        retryAfterMs += Random.Shared.Next(50, 200);
                        await Task.Delay(retryAfterMs, cancellationToken);
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

        public async Task<List<LocationOption>> SearchLocationsAsync(string query, int limit = 10, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<LocationOption>();

            string trimmedQuery = query.Trim();
            string url =
                $"{_options.CitiesUrl}" +
                $"?namePrefix={Uri.EscapeDataString(trimmedQuery)}" +
                $"&types=CITY" +
                $"&limit={limit}" +
                $"&sort=-population";

            var json = await SendGeoDbGetAsync(url, cancellationToken);

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

        public async Task<List<CountryOption>> SearchCountriesAsync(string query, int limit = 10, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<CountryOption>();

            string trimmedQuery = query.Trim();
            string url =
                $"{_options.CountriesUrl}" +
                $"?namePrefix={Uri.EscapeDataString(trimmedQuery)}" +
                $"&limit={limit}";

            var json = await SendGeoDbGetAsync(url, cancellationToken);

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

        public async Task<List<LocationOption>> GetCitiesByCountryCodeAsync(string countryCode, int limit = 20, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(countryCode))
                return new List<LocationOption>();

            string code = countryCode.Trim();
            string url =
                $"{_options.CitiesUrl}" +
                $"?countryIds={Uri.EscapeDataString(code)}" +
                $"&types=CITY" +
                $"&limit={limit}" +
                $"&sort=-population";

            var json = await SendGeoDbGetAsync(url, cancellationToken);

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