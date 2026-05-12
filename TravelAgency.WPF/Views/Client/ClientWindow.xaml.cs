using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using TravelAgency.Core.Models.TripPkg.Package;
using TravelAgency.WPF.ViewModels.ClientVM;
using TravelAgency.WPF.Views.Common;

namespace TravelAgency.WPF.Views
{
    public partial class ClientWindow : Window
    {
        public ClientWindow()
        {
            InitializeComponent();
            DataContext = ActivatorUtilities.CreateInstance<ClientViewModel>(App.Services);
        }

        private void ViewDetails_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ClientViewModel vm &&
                sender is Button button &&
                button.Tag is TripPackage trip)
            {
                if (trip.AvailableSeats <= 0 && vm.FavoritesVisibility != Visibility.Visible)
                {
                    MessageBox.Show(
                        "Nu mai sunt disponibile locuri la acest pachet.",
                        "Sold out",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                vm.SelectedPackage = trip;
            }
        }

        private void ClientThumb_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not ClientViewModel vm ||
                sender is not FrameworkElement fe ||
                fe.Tag is not string url ||
                string.IsNullOrWhiteSpace(url))
                return;

            var trimmed = url.Trim();

            var destIndex = IndexOfUrl(vm.DestinationImageUrls, trimmed);
            if (destIndex >= 0)
            {
                var list = vm.DestinationImageUrls.ToList();
                ImagePreviewWindow.ShowForUrls(this, "Destinație", list, destIndex);
                e.Handled = true;
                return;
            }

            var hotelIndex = IndexOfUrl(vm.HotelImageUrls, trimmed);
            if (hotelIndex >= 0)
            {
                var list = vm.HotelImageUrls.ToList();
                ImagePreviewWindow.ShowForUrls(this, "Cazare", list, hotelIndex);
                e.Handled = true;
                return;
            }

            ImagePreviewWindow.ShowForUrl(this, "Image", trimmed);
            e.Handled = true;
        }

        private static int IndexOfUrl(IReadOnlyList<string> urls, string trimmed)
        {
            for (var i = 0; i < urls.Count; i++)
            {
                var u = (urls[i] ?? "").Trim();
                if (u.Length == 0)
                    continue;
                if (UrlsMatch(u, trimmed))
                    return i;
            }

            return -1;
        }

        private static bool UrlsMatch(string listUrl, string clicked)
        {
            if (string.Equals(listUrl, clicked, StringComparison.OrdinalIgnoreCase))
                return true;

            var innerClick = TryUnwrapImageProxyUrl(clicked);
            var innerList = TryUnwrapImageProxyUrl(listUrl);

            if (innerClick != null &&
                string.Equals(listUrl, innerClick, StringComparison.OrdinalIgnoreCase))
                return true;
            if (innerList != null &&
                string.Equals(clicked, innerList, StringComparison.OrdinalIgnoreCase))
                return true;
            if (innerClick != null && innerList != null &&
                string.Equals(innerClick, innerList, StringComparison.OrdinalIgnoreCase))
                return true;

            var a = innerList ?? listUrl;
            var b = innerClick ?? clicked;
            return SameResourceIgnoringQuery(a, b);
        }

        /// <summary>Extrage URL-ul original din <c>/api/images/proxy?url=...</c> (dacă e cazul).</summary>
        private static string? TryUnwrapImageProxyUrl(string url)
        {
            if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
                return null;

            if (!uri.AbsolutePath.Contains("images/proxy", StringComparison.OrdinalIgnoreCase))
                return null;

            var q = uri.Query;
            if (string.IsNullOrEmpty(q) || q.Length < 2)
                return null;

            foreach (var part in q.TrimStart('?').Split('&'))
            {
                var eq = part.IndexOf('=');
                if (eq <= 0)
                    continue;
                var name = part[..eq];
                if (!string.Equals(name, "url", StringComparison.OrdinalIgnoreCase))
                    continue;
                var value = part[(eq + 1)..];
                if (value.Length == 0)
                    return null;
                return Uri.UnescapeDataString(value);
            }

            return null;
        }

        private static bool SameResourceIgnoringQuery(string a, string b)
        {
            if (!Uri.TryCreate(a, UriKind.Absolute, out var ua) || !Uri.TryCreate(b, UriKind.Absolute, out var ub))
                return false;

            if (!string.Equals(ua.Host, ub.Host, StringComparison.OrdinalIgnoreCase))
                return false;

            var pa = ua.GetLeftPart(UriPartial.Path).TrimEnd('/');
            var pb = ub.GetLeftPart(UriPartial.Path).TrimEnd('/');
            return string.Equals(pa, pb, StringComparison.OrdinalIgnoreCase);
        }

        private void ClientDetailsImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is ClientViewModel vm &&
                vm.SelectedPackage?.CoverImageUrl is string url &&
                !string.IsNullOrWhiteSpace(url))
            {
                ImagePreviewWindow.ShowForUrl(this, "Package cover", url);
                e.Handled = true;
            }
        }
    }
}