using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace TravelAgency.WPF.Converters
{
    public sealed class UrlToProxyImageSourceConverter : IValueConverter
    {
        private const string ApiBaseUrl = "http://localhost:5280";

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var url = (value as string ?? "").Trim();
            if (url.Length == 0)
                return null;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return null;

            var isAlreadyProxy =
                string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase) &&
                uri.AbsolutePath.Equals("/api/images/proxy", StringComparison.OrdinalIgnoreCase);

            var final = isAlreadyProxy
                ? uri
                : new Uri($"{ApiBaseUrl}/api/images/proxy?url={Uri.EscapeDataString(uri.ToString())}");

            var bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = final;
            bi.CacheOption = BitmapCacheOption.OnDemand;
            bi.DecodePixelWidth = 160;
            bi.EndInit();
            return bi;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            Binding.DoNothing;
    }
}

