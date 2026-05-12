using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TravelAgency.Core.Models.TripPkg.Package;

namespace TravelAgency.WPF.Converters
{
    public class TripPackageToImageSourceConverter : IValueConverter
    {
        // Local dev API (image proxy) for reliable hotlink-free rendering in WPF.
        private const string ApiBaseUrl = "http://localhost:5280";
        private static readonly string[] FallbackPackUris =
        [
            "pack://application:,,,/TravelAgency.WPF;component/Assets/agent-bg-marseille.jpg",
            "pack://application:,,,/TravelAgency.WPF;component/Assets/start-hero.png",
            "pack://application:,,,/TravelAgency.WPF;component/Assets/agent-bg2.png",
            "pack://application:,,,/TravelAgency.WPF;component/Assets/agent-bg.png",
            "pack://application:,,,/TravelAgency.WPF;component/Assets/login-bg.png",
            "pack://application:,,,/TravelAgency.WPF;component/Assets/register-bg.png"
        ];

        private static readonly ImageSource CoverLoadPlaceholder = CreateCoverPlaceholder();

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not TripPackage trip)
                return null;

            // 0) Persisted cover image URL (preferred for stability)
            if (!string.IsNullOrWhiteSpace(trip.CoverImageUrl) &&
                Uri.TryCreate(trip.CoverImageUrl.Trim(), UriKind.Absolute, out var coverUri))
            {
                try
                {
                    // If the stored URL is already our local proxy, don't proxy it again (would recurse).
                    var isAlreadyProxy =
                        string.Equals(coverUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(coverUri.Host, "localhost", StringComparison.OrdinalIgnoreCase) &&
                        coverUri.AbsolutePath.Equals("/api/images/proxy", StringComparison.OrdinalIgnoreCase);

                    // Route external URLs through API proxy to avoid remote host blocks / odd formats.
                    var optimized = isAlreadyProxy ? coverUri : OptimizeCoverUrlForSpeed(coverUri);
                    var proxy = isAlreadyProxy
                        ? optimized
                        : new Uri($"{ApiBaseUrl}/api/images/proxy?url={Uri.EscapeDataString(optimized.ToString())}");
                    var bi = new BitmapImage();
                    bi.BeginInit();
                    bi.UriSource = proxy;
                    // Use OnLoad so network/proxy failures fall back to placeholder (avoids blank banners).
                    // Kept small (360px) so it still loads fast.
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                    bi.DecodePixelWidth = 360;
                    bi.EndInit();
                    bi.Freeze();
                    return bi;
                }
                catch
                {
                    // If proxy isn't reachable (API not running), try loading the optimized URL directly.
                    // This keeps Client/Agent banners working even without the local API.
                    try
                    {
                        var direct = OptimizeCoverUrlForSpeed(coverUri);
                        var bi2 = new BitmapImage();
                        bi2.BeginInit();
                        bi2.UriSource = direct;
                        bi2.CacheOption = BitmapCacheOption.OnDemand;
                        bi2.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                        bi2.DecodePixelWidth = 360;
                        bi2.EndInit();
                        return bi2;
                    }
                    catch
                    {
                        // If a cover exists but cannot be loaded/decoded, do NOT switch to local Assets fallback.
                        // Show a neutral placeholder instead (prevents "wrong cover" perception).
                        return CoverLoadPlaceholder;
                    }
                }
            }

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // 1) Per-package files (wins over shared destination art — avoids „same photo for every Paris card”).
            //    Drop files next to the exe: Assets/Packages/<TripId>.jpg|png|jpeg|webp
            var packagesDir = Path.Combine(baseDir, "Assets", "Packages");
            if (Directory.Exists(packagesDir))
            {
                var candidates = new[]
                {
                    Path.Combine(packagesDir, $"{trip.Id}.jpg"),
                    Path.Combine(packagesDir, $"{trip.Id}.png"),
                    Path.Combine(packagesDir, $"{trip.Id}.jpeg"),
                    Path.Combine(packagesDir, $"{trip.Id}.webp")
                };

                var existing = candidates.FirstOrDefault(File.Exists);
                if (!string.IsNullOrWhiteSpace(existing))
                    return LoadFromFile(existing);
            }

            // 2) Destination-based image (shared by all packages with that destination — intentional).
            //    Assets/Destinations/<DestinationSlug>.jpg|png|jpeg|webp
            var dest = (trip.Destination ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(dest))
            {
                var destDir = Path.Combine(baseDir, "Assets", "Destinations");
                var slug = Slugify(dest);
                if (slug.Length > 0)
                {
                    var existing = FirstExistingBySlug(destDir, slug);
                    if (!string.IsNullOrWhiteSpace(existing))
                        return LoadFromFile(existing);

                    var country = (trip.Country ?? "").Trim();
                    if (!string.IsNullOrWhiteSpace(country))
                    {
                        var slug2 = Slugify($"{country}-{dest}");
                        if (slug2.Length > 0)
                        {
                            var existing2 = FirstExistingBySlug(destDir, slug2);
                            if (!string.IsNullOrWhiteSpace(existing2))
                                return LoadFromFile(existing2);
                        }
                    }
                }
            }

            // 3) Fallback pack art: spread by more than Id so clones / sequential ids don’t cluster on the same image.
            var mix = HashCode.Combine(
                trip.Id,
                trip.Name ?? "",
                trip.Destination ?? "",
                trip.Country ?? "");
            var idx = Math.Abs(mix) % FallbackPackUris.Length;
            return LoadFromPackUri(FallbackPackUris[idx]);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            Binding.DoNothing;

        private static ImageSource LoadFromPackUri(string uri)
        {
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = new Uri(uri, UriKind.Absolute);
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.EndInit();
            bi.Freeze();
            return bi;
        }

        private static ImageSource LoadFromFile(string path)
        {
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = new Uri(path, UriKind.Absolute);
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.EndInit();
            bi.Freeze();
            return bi;
        }

        private static Uri OptimizeCoverUrlForSpeed(Uri uri)
        {
            try
            {
                // Unsplash supports resizing via query param `w`.
                // Some URLs use `plus.unsplash.com` as host.
                if (!uri.Host.EndsWith("unsplash.com", StringComparison.OrdinalIgnoreCase))
                    return uri;

                var s = uri.ToString();

                // Force a WPF-decodable format (avoid webp/avif).
                // Unsplash supports `fm=jpg`. Also remove `auto=format` (which may pick webp/avif).
                s = s.Replace("auto=format", "auto=compress", StringComparison.OrdinalIgnoreCase);
                if (s.Contains("fm=webp", StringComparison.OrdinalIgnoreCase))
                    s = s.Replace("fm=webp", "fm=jpg", StringComparison.OrdinalIgnoreCase);
                if (s.Contains("fm=avif", StringComparison.OrdinalIgnoreCase))
                    s = s.Replace("fm=avif", "fm=jpg", StringComparison.OrdinalIgnoreCase);
                if (!s.Contains("fm=", StringComparison.OrdinalIgnoreCase))
                {
                    var sep0 = s.Contains('?', StringComparison.Ordinal) ? "&" : "?";
                    s += sep0 + "fm=jpg";
                }

                // Replace existing w=... or append w=360
                var wIdx = s.IndexOf("w=", StringComparison.OrdinalIgnoreCase);
                if (wIdx >= 0)
                {
                    var start = wIdx + 2;
                    var end = start;
                    while (end < s.Length && char.IsDigit(s[end])) end++;
                    s = s[..start] + "360" + s[end..];
                    return new Uri(s, UriKind.Absolute);
                }

                var sep = s.Contains('?', StringComparison.Ordinal) ? "&" : "?";
                return new Uri(s + sep + "w=360", UriKind.Absolute);
            }
            catch
            {
                return uri;
            }
        }

        private static string? FirstExistingBySlug(string dir, string slug)
        {
            if (!Directory.Exists(dir))
                return null;

            var candidates = new[]
            {
                Path.Combine(dir, $"{slug}.jpg"),
                Path.Combine(dir, $"{slug}.png"),
                Path.Combine(dir, $"{slug}.jpeg"),
                Path.Combine(dir, $"{slug}.webp")
            };

            return candidates.FirstOrDefault(File.Exists);
        }

        private static string Slugify(string s)
        {
            var chars = s
                .Trim()
                .ToLowerInvariant()
                .Select(ch =>
                    char.IsLetterOrDigit(ch) ? ch :
                    ch is ' ' or '-' or '_' ? '-' :
                    '-')
                .ToArray();

            var slug = new string(chars);
            while (slug.Contains("--", StringComparison.Ordinal))
                slug = slug.Replace("--", "-", StringComparison.Ordinal);

            return slug.Trim('-');
        }

        private static ImageSource CreateCoverPlaceholder()
        {
            // A small neutral gradient placeholder, generated in code (not from Assets).
            var bg = new LinearGradientBrush(
                Color.FromRgb(0xE2, 0xE8, 0xF0),
                Color.FromRgb(0xF8, 0xFA, 0xFC),
                new Point(0, 0),
                new Point(1, 1));
            bg.Freeze();

            var pen = new Pen(new SolidColorBrush(Color.FromRgb(0xCB, 0xD5, 0xE1)), 1);
            pen.Freeze();

            var rect = new RectangleGeometry(new System.Windows.Rect(0, 0, 520, 240), 22, 22);
            rect.Freeze();

            var drawing = new GeometryDrawing(bg, pen, rect);
            drawing.Freeze();

            var img = new DrawingImage(drawing);
            img.Freeze();
            return img;
        }
    }
}

