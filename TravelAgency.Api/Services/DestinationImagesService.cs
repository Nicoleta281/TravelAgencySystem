using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;

namespace TravelAgency.Api.Services;

public sealed class DestinationImagesService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly string _cacheDir;
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _keyLocks = new();

    public DestinationImagesService(IHttpClientFactory httpClientFactory, IConfiguration configuration, IWebHostEnvironment env)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _cacheDir = Path.Combine(env.ContentRootPath, "cache", "destinations");
        Directory.CreateDirectory(_cacheDir);
    }

    public async Task<DestinationImagesResponse> GetImagesAsync(
        string city,
        string? country,
        int limit,
        int? seed,
        CancellationToken cancellationToken)
    {
        city = (city ?? "").Trim();
        country = (country ?? "").Trim();
        limit = Math.Clamp(limit, 1, 20);

        if (city.Length == 0)
            return new DestinationImagesResponse(city, country, []);

        // v6: cache key version bump + include Unsplash-key presence to avoid
        // reusing stale cache created before key was configured.
        var hasUnsplashKey =
            !string.IsNullOrWhiteSpace(_configuration["Unsplash:AccessKey"]) ||
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("UNSPLASH_ACCESS_KEY"));
        // v8: Unsplash-only mode (no Wikimedia/Wikipedia). Include seed so same destination can yield varied sets.
        var key = Slugify($"v8|us={(hasUnsplashKey ? 1 : 0)}|{city}|{country}|{limit}|seed={seed ?? 0}");
        var cachePath = Path.Combine(_cacheDir, $"{key}.json");

        // Fast path: disk cache
        var cached = await TryReadCacheAsync(cachePath, cancellationToken);
        if (cached != null)
            return cached;

        // Per-key lock to avoid stampedes
        var gate = _keyLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            cached = await TryReadCacheAsync(cachePath, cancellationToken);
            if (cached != null)
                return cached;

            var query = string.IsNullOrWhiteSpace(country) ? city : $"{city}, {country}";
            var images = new List<DestinationImage>();

            // 0) Prefer Unsplash (stable image CDN, good thumbnails)
            foreach (var img in await FetchUnsplashImagesAsync(query, limit, seed, cancellationToken))
            {
                if (images.Count >= limit)
                    break;
                images.Add(img);
            }

            // Unsplash-only requested: do NOT fallback to other sources.
            // Other sources often return webp/avif which WPF cannot decode reliably.

            var result = new DestinationImagesResponse(city, country, images.Take(limit).ToList());
            await WriteCacheAsync(cachePath, result, cancellationToken);
            return result;
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<List<DestinationImage>> FetchUnsplashImagesAsync(string query, int limit, int? seed, CancellationToken ct)
    {
        try
        {
            var accessKey =
                _configuration["Unsplash:AccessKey"]
                ?? Environment.GetEnvironmentVariable("UNSPLASH_ACCESS_KEY")
                ?? "";

            if (string.IsNullOrWhiteSpace(accessKey))
                return [];

            var http = _httpClientFactory.CreateClient();
            http.Timeout = TimeSpan.FromSeconds(15);
            http.DefaultRequestHeaders.Remove("Authorization");
            http.DefaultRequestHeaders.Add("Authorization", "Client-ID " + accessKey.Trim());
            http.DefaultRequestHeaders.Remove("Accept-Version");
            http.DefaultRequestHeaders.Add("Accept-Version", "v1");

            // Deterministic variety: pick a stable page based on seed.
            // Unsplash search uses pagination; varying page gives different images for same query.
            var page = 1;
            if (seed.HasValue)
            {
                var s = seed.Value;
                if (s == int.MinValue) s = 0;
                page = 1 + (Math.Abs(s) % 3); // 1..3
            }

            var url =
                "https://api.unsplash.com/search/photos" +
                "?query=" + WebUtility.UrlEncode(query + " travel") +
                "&per_page=" + Math.Clamp(limit, 1, 12) +
                "&page=" + page +
                "&orientation=landscape" +
                "&content_filter=high";

            using var resp = await http.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode)
                return [];

            var json = await resp.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("results", out var resultsEl) ||
                resultsEl.ValueKind != JsonValueKind.Array)
                return [];

            var results = new List<DestinationImage>();
            foreach (var item in resultsEl.EnumerateArray())
            {
                if (results.Count >= limit)
                    break;

                if (!item.TryGetProperty("urls", out var urlsEl) || urlsEl.ValueKind != JsonValueKind.Object)
                    continue;

                var full = urlsEl.TryGetProperty("regular", out var regEl) ? regEl.GetString() : null;
                var thumb = urlsEl.TryGetProperty("small", out var smEl) ? smEl.GetString() : null;

                if (string.IsNullOrWhiteSpace(full))
                    continue;

                // Force WPF-decodable format (avoid auto=format -> webp/avif).
                full = NormalizeUnsplashUrl(full);
                if (!string.IsNullOrWhiteSpace(thumb))
                    thumb = NormalizeUnsplashUrl(thumb);

                string? author = null;
                if (item.TryGetProperty("user", out var userEl) &&
                    userEl.ValueKind == JsonValueKind.Object &&
                    userEl.TryGetProperty("name", out var nameEl))
                {
                    author = nameEl.GetString();
                }

                results.Add(new DestinationImage(
                    Url: full!,
                    ThumbUrl: string.IsNullOrWhiteSpace(thumb) ? null : thumb,
                    SourceUrl: "https://unsplash.com",
                    Attribution: string.IsNullOrWhiteSpace(author) ? "Unsplash" : $"Unsplash • {author}",
                    Width: null,
                    Height: null));
            }

            return results;
        }
        catch
        {
            return [];
        }
    }

    private static string NormalizeUnsplashUrl(string url)
    {
        try
        {
            var s = (url ?? "").Trim();
            if (s.Length == 0)
                return s;

            // Prefer jpg output.
            s = s.Replace("auto=format", "auto=compress", StringComparison.OrdinalIgnoreCase);
            if (s.Contains("fm=webp", StringComparison.OrdinalIgnoreCase))
                s = s.Replace("fm=webp", "fm=jpg", StringComparison.OrdinalIgnoreCase);
            if (s.Contains("fm=avif", StringComparison.OrdinalIgnoreCase))
                s = s.Replace("fm=avif", "fm=jpg", StringComparison.OrdinalIgnoreCase);
            if (!s.Contains("fm=", StringComparison.OrdinalIgnoreCase))
                s += (s.Contains('?', StringComparison.Ordinal) ? "&" : "?") + "fm=jpg";

            return s;
        }
        catch
        {
            return (url ?? "").Trim();
        }
    }

    private async Task<DestinationImagesResponse?> TryReadCacheAsync(string path, CancellationToken ct)
    {
        try
        {
            if (!File.Exists(path))
                return null;

            var json = await File.ReadAllTextAsync(path, ct);
            return JsonSerializer.Deserialize<DestinationImagesResponse>(json, JsonOpts());
        }
        catch
        {
            return null;
        }
    }

    private async Task WriteCacheAsync(string path, DestinationImagesResponse value, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(value, JsonOpts());
        await File.WriteAllTextAsync(path, json, ct);
    }

    private static JsonSerializerOptions JsonOpts() => new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true
    };

    private async Task<string?> ResolveWikipediaTitleAsync(string query, CancellationToken ct)
    {
        // Use OpenSearch for a best-effort title resolution
        // https://en.wikipedia.org/w/api.php?action=opensearch&search=Paris%2C%20France&limit=1&namespace=0&format=json
        var http = CreateWikiClient();

        var url =
            "https://en.wikipedia.org/w/api.php" +
            "?action=opensearch" +
            "&limit=1" +
            "&namespace=0" +
            "&format=json" +
            "&search=" + WebUtility.UrlEncode(query);

        using var resp = await http.GetAsync(url, ct);
        if ((int)resp.StatusCode == 429)
        {
            var delay = TryGetRetryAfterDelay(resp) ?? TimeSpan.FromSeconds(2);
            await Task.Delay(delay, ct);
            using var resp2 = await http.GetAsync(url, ct);
            var json2 = await resp2.Content.ReadAsStringAsync(ct);
            if (!resp2.IsSuccessStatusCode)
                return null;
            return ParseOpenSearchTitle(json2);
        }

        var json = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            return null;

        return ParseOpenSearchTitle(json);
    }

    private async Task<DestinationImage?> FetchWikipediaSummaryImageAsync(string title, CancellationToken ct)
    {
        var http = CreateWikiClient();

        var url = "https://en.wikipedia.org/api/rest_v1/page/summary/" + WebUtility.UrlEncode(title);
        using var resp = await http.GetAsync(url, ct);
        if ((int)resp.StatusCode == 429)
        {
            var delay = TryGetRetryAfterDelay(resp) ?? TimeSpan.FromSeconds(2);
            await Task.Delay(delay, ct);
            using var resp2 = await http.GetAsync(url, ct);
            if (!resp2.IsSuccessStatusCode)
                return null;
            var json2 = await resp2.Content.ReadAsStringAsync(ct);
            return ParseSummaryImage(json2, title);
        }
        if (!resp.IsSuccessStatusCode)
            return null;

        var json = await resp.Content.ReadAsStringAsync(ct);
        return ParseSummaryImage(json, title);
    }

    private DestinationImage? ParseSummaryImage(string json, string title)
    {
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("thumbnail", out var thumb) ||
            thumb.ValueKind != JsonValueKind.Object ||
            !thumb.TryGetProperty("source", out var srcEl))
            return null;

        var src = srcEl.GetString();
        if (string.IsNullOrWhiteSpace(src))
            return null;

        int? w = null;
        int? h = null;
        if (thumb.TryGetProperty("width", out var wEl) && wEl.TryGetInt32(out var wi)) w = wi;
        if (thumb.TryGetProperty("height", out var hEl) && hEl.TryGetInt32(out var hi)) h = hi;

        string? pageUrl = null;
        if (doc.RootElement.TryGetProperty("content_urls", out var cu) &&
            cu.ValueKind == JsonValueKind.Object &&
            cu.TryGetProperty("desktop", out var desk) &&
            desk.ValueKind == JsonValueKind.Object &&
            desk.TryGetProperty("page", out var pEl))
        {
            pageUrl = pEl.GetString();
        }

        return new DestinationImage(
            Url: src,
            ThumbUrl: null,
            SourceUrl: pageUrl,
            Attribution: "Wikipedia",
            Width: w,
            Height: h);
    }

    private async Task<List<DestinationImage>> FetchWikipediaMediaImagesAsync(
        string title,
        int limit,
        CancellationToken ct)
    {
        var http = CreateWikiClient();

        var url = "https://en.wikipedia.org/api/rest_v1/page/media/" + WebUtility.UrlEncode(title);
        using var resp = await http.GetAsync(url, ct);
        if ((int)resp.StatusCode == 429)
        {
            var delay = TryGetRetryAfterDelay(resp) ?? TimeSpan.FromSeconds(2);
            await Task.Delay(delay, ct);
            using var resp2 = await http.GetAsync(url, ct);
            if (!resp2.IsSuccessStatusCode)
                return [];
            var json2 = await resp2.Content.ReadAsStringAsync(ct);
            return ParseMediaImages(json2, title, limit);
        }
        if (!resp.IsSuccessStatusCode)
            return [];

        var json = await resp.Content.ReadAsStringAsync(ct);
        return ParseMediaImages(json, title, limit);
    }

    private List<DestinationImage> ParseMediaImages(string json, string title, int limit)
    {
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("items", out var items) ||
            items.ValueKind != JsonValueKind.Array)
            return [];

        var results = new List<DestinationImage>();

        foreach (var item in items.EnumerateArray())
        {
            if (results.Count >= limit)
                break;

            if (!item.TryGetProperty("type", out var typeEl) ||
                !string.Equals(typeEl.GetString(), "image", StringComparison.OrdinalIgnoreCase))
                continue;

            // Prefer original source, fallback to any srcset
            string? src = null;
            int? w = null;
            int? h = null;

            if (item.TryGetProperty("original", out var orig) &&
                orig.ValueKind == JsonValueKind.Object &&
                orig.TryGetProperty("source", out var sEl))
            {
                src = sEl.GetString();
                if (orig.TryGetProperty("width", out var wEl) && wEl.TryGetInt32(out var wi)) w = wi;
                if (orig.TryGetProperty("height", out var hEl) && hEl.TryGetInt32(out var hi)) h = hi;
            }

            if (string.IsNullOrWhiteSpace(src) &&
                item.TryGetProperty("srcset", out var srcset) &&
                srcset.ValueKind == JsonValueKind.Array &&
                srcset.GetArrayLength() > 0)
            {
                var last = srcset.EnumerateArray().Last();
                if (last.TryGetProperty("src", out var ss))
                    src = ss.GetString();
            }

            if (string.IsNullOrWhiteSpace(src))
                continue;

            results.Add(new DestinationImage(
                Url: src,
                ThumbUrl: null,
                SourceUrl: $"https://en.wikipedia.org/wiki/{WebUtility.UrlEncode(title)}",
                Attribution: "Wikipedia",
                Width: w,
                Height: h));
        }

        return results;
    }

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

    private HttpClient CreateWikiClient()
    {
        var http = _httpClientFactory.CreateClient();
        http.Timeout = TimeSpan.FromSeconds(10);

        // Wikipedia asks for a descriptive User-Agent.
        if (!http.DefaultRequestHeaders.UserAgent.Any())
            http.DefaultRequestHeaders.UserAgent.ParseAdd("TravelAgencySystem/1.0 (contact: local-dev)");

        return http;
    }

    private async Task<List<DestinationImage>> FetchSerpApiImagesAsync(string query, int limit, CancellationToken ct)
    {
        try
        {
            var apiKey =
                _configuration["SerpApi:ApiKey"]
                ?? Environment.GetEnvironmentVariable("SERPAPI_API_KEY")
                ?? "";

            if (string.IsNullOrWhiteSpace(apiKey))
                return [];

            var http = _httpClientFactory.CreateClient();
            http.Timeout = TimeSpan.FromSeconds(15);

            var url =
                "https://serpapi.com/search.json" +
                "?engine=google_images" +
                "&q=" + WebUtility.UrlEncode(query) +
                "&ijn=0" +
                "&num=" + Math.Clamp(limit, 1, 10) +
                "&hl=en" +
                "&api_key=" + WebUtility.UrlEncode(apiKey);

            using var resp = await http.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode)
                return [];

            var json = await resp.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("images_results", out var items) ||
                items.ValueKind != JsonValueKind.Array)
                return [];

            var results = new List<DestinationImage>();
            foreach (var item in items.EnumerateArray())
            {
                if (results.Count >= limit)
                    break;

                // Prefer original, fallback to thumbnail
                string? src = null;
                if (item.TryGetProperty("original", out var orig))
                    src = orig.GetString();
                if (string.IsNullOrWhiteSpace(src) && item.TryGetProperty("thumbnail", out var thumb))
                    src = thumb.GetString();

                if (string.IsNullOrWhiteSpace(src))
                    continue;

                // WPF (without extra codecs) can't decode webp/avif/svg reliably.
                // Some URLs include query strings, so check the URI path extension.
                if (Uri.TryCreate(src.Trim(), UriKind.Absolute, out var srcUri))
                {
                    var ext = Path.GetExtension(srcUri.AbsolutePath).ToLowerInvariant();
                    if (ext is ".webp" or ".avif" or ".svg")
                        continue;
                }

                string? link = null;
                if (item.TryGetProperty("link", out var linkEl))
                    link = linkEl.GetString();

                results.Add(new DestinationImage(
                    Url: src,
                    ThumbUrl: null,
                    SourceUrl: link,
                    Attribution: "SerpApi",
                    Width: null,
                    Height: null));
            }

            return results;
        }
        catch
        {
            return [];
        }
    }

    private static string? ParseOpenSearchTitle(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() < 2)
            return null;

        var titles = doc.RootElement[1];
        if (titles.ValueKind != JsonValueKind.Array || titles.GetArrayLength() == 0)
            return null;

        return titles[0].GetString();
    }

    private static TimeSpan? TryGetRetryAfterDelay(HttpResponseMessage resp)
    {
        if (!resp.Headers.TryGetValues("Retry-After", out var values))
            return null;

        var raw = values.FirstOrDefault();
        if (raw == null)
            return null;

        if (int.TryParse(raw, out var seconds))
            return TimeSpan.FromSeconds(Math.Clamp(seconds, 1, 10));

        return null;
    }

    // DTOs
    public sealed record DestinationImagesResponse(string City, string? Country, List<DestinationImage> Images);

    public sealed record DestinationImage(
        string Url,
        string? ThumbUrl,
        string? SourceUrl,
        string? Attribution,
        int? Width,
        int? Height);

    // (Wikipedia JSON models removed; we parse with JsonDocument for reliability.)
}

