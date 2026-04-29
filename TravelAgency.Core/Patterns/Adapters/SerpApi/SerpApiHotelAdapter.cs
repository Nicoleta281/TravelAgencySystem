using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TravelAgency.Core.Interfaces;
using TravelAgency.Core.Models.External.SerpApi;
using TravelAgency.Core.Models.Locations;

namespace TravelAgency.Core.Patterns.Adapters.SerpApi
{
    public class SerpApiHotelAdapter : IHotelSearchProvider
    {
        private readonly HttpClient _httpClient;
        private readonly SerpApiOptions _options;

        public SerpApiHotelAdapter(HttpClient httpClient, SerpApiOptions options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options ?? throw new ArgumentNullException(nameof(options));

            if (string.IsNullOrWhiteSpace(_options.ApiKey))
                throw new InvalidOperationException("Missing SerpApi API key.");
        }

        public async Task<List<HotelSearchOption>> SearchHotelsAsync(
            string destination,
            DateTime checkInDate,
            DateTime checkOutDate,
            int adults,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(destination))
                throw new ArgumentException("Destination is required.", nameof(destination));

            if (checkOutDate <= checkInDate)
                throw new ArgumentException("Check-out date must be later than check-in date.");

            if (adults <= 0)
                throw new ArgumentException("Adults must be greater than 0.", nameof(adults));

            string url =
                $"{_options.BaseUrl}" +
                $"?engine=google_hotels" +
                $"&q={Uri.EscapeDataString(destination)}" +
                $"&check_in_date={checkInDate:yyyy-MM-dd}" +
                $"&check_out_date={checkOutDate:yyyy-MM-dd}" +
                $"&adults={adults}" +
                $"&currency={Uri.EscapeDataString(_options.Currency)}" +
                $"&hl={Uri.EscapeDataString(_options.Language)}" +
                $"&gl={Uri.EscapeDataString(_options.Country)}" +
                $"&api_key={Uri.EscapeDataString(_options.ApiKey)}";

            using var response = await _httpClient.GetAsync(url, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);

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
