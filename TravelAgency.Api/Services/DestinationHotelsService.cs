using System.Collections.Concurrent;
using System.Text.Json;
using TravelAgency.Core.Patterns.Adapters.SerpApi;
using TravelAgency.Core.Services;

namespace TravelAgency.Api.Services;

public sealed class DestinationHotelsService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _cacheDir;
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _keyLocks = new();

    public DestinationHotelsService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        IWebHostEnvironment env)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _cacheDir = Path.Combine(env.ContentRootPath, "cache", "hotels");
        Directory.CreateDirectory(_cacheDir);
    }

    public async Task<DestinationHotelsResponse> GetHotelsAsync(
        string city,
        string? country,
        DateTime checkIn,
        DateTime checkOut,
        int adults,
        int limit,
        CancellationToken cancellationToken)
    {
        city = (city ?? "").Trim();
        country = (country ?? "").Trim();
        adults = Math.Clamp(adults, 1, 10);
        limit = Math.Clamp(limit, 1, 20);

        if (city.Length == 0)
            return new DestinationHotelsResponse(city, country, checkIn, checkOut, adults, []);

        var query = string.IsNullOrWhiteSpace(country) ? city : $"{city}, {country}";
        // v2: hotel image URL prefers SerpApi original_image over tiny thumbnail (cache bust).
        var key = Slugify($"v2|{query}|{checkIn:yyyyMMdd}|{checkOut:yyyyMMdd}|{adults}|{limit}");
        var cachePath = Path.Combine(_cacheDir, $"{key}.json");

        var cached = await TryReadCacheAsync(cachePath, cancellationToken);
        if (cached != null)
            return cached;

        var gate = _keyLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            cached = await TryReadCacheAsync(cachePath, cancellationToken);
            if (cached != null)
                return cached;

            var apiKey =
                _configuration["SerpApi:ApiKey"]
                ?? Environment.GetEnvironmentVariable("SERPAPI_API_KEY")
                ?? "";

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("Missing SerpApi API key (set SerpApi:ApiKey or SERPAPI_API_KEY).");

            var options = new SerpApiOptions
            {
                ApiKey = apiKey,
                Language = _configuration["SerpApi:Language"] ?? "en",
                Country = _configuration["SerpApi:Country"] ?? "us",
                Currency = _configuration["SerpApi:Currency"] ?? "EUR"
            };

            var adapter = new SerpApiHotelAdapter(_httpClientFactory.CreateClient(), options);
            var svc = new HotelSearchService(adapter);
            var hotels = await svc.SearchHotelsAsync(query, checkIn, checkOut, adults);

            var trimmed = hotels.Take(limit).Select(h => new DestinationHotel(
                Name: h.Name,
                Description: h.Description,
                Link: h.Link,
                ThumbnailUrl: h.ThumbnailUrl,
                HotelClass: h.HotelClass,
                PricePerNight: h.PricePerNight,
                TotalPrice: h.TotalPrice
            )).ToList();

            var result = new DestinationHotelsResponse(city, country, checkIn, checkOut, adults, trimmed);
            await WriteCacheAsync(cachePath, result, cancellationToken);
            return result;
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<DestinationHotelsResponse?> TryReadCacheAsync(string path, CancellationToken ct)
    {
        try
        {
            if (!File.Exists(path))
                return null;
            var json = await File.ReadAllTextAsync(path, ct);
            return JsonSerializer.Deserialize<DestinationHotelsResponse>(json, JsonOpts());
        }
        catch
        {
            return null;
        }
    }

    private async Task WriteCacheAsync(string path, DestinationHotelsResponse value, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(value, JsonOpts());
        await File.WriteAllTextAsync(path, json, ct);
    }

    private static JsonSerializerOptions JsonOpts() => new(JsonSerializerDefaults.Web);

    private static string Slugify(string s)
    {
        var chars = (s ?? "")
            .Trim()
            .ToLowerInvariant()
            .Select(ch =>
                char.IsLetterOrDigit(ch) ? ch :
                ch is ' ' or '-' or '_' or '|' ? '-' :
                '-')
            .ToArray();

        var slug = new string(chars);
        while (slug.Contains("--", StringComparison.Ordinal))
            slug = slug.Replace("--", "-", StringComparison.Ordinal);

        return slug.Trim('-');
    }

    public sealed record DestinationHotelsResponse(
        string City,
        string? Country,
        DateTime CheckIn,
        DateTime CheckOut,
        int Adults,
        List<DestinationHotel> Hotels);

    public sealed record DestinationHotel(
        string Name,
        string Description,
        string Link,
        string ThumbnailUrl,
        int? HotelClass,
        decimal? PricePerNight,
        decimal? TotalPrice);
}

