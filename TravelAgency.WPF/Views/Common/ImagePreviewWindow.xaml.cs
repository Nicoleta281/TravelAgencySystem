using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace TravelAgency.WPF.Views.Common
{
    public partial class ImagePreviewWindow : Window
    {
        private const string ApiBaseUrl = "http://localhost:5280";

        private List<string> _galleryUrls = new();
        private int _galleryIndex;
        private string? _galleryHeaderTitle;

        public ImagePreviewWindow()
        {
            InitializeComponent();
            ZoomSlider.Value = 1.0;
            Loaded += (_, __) => UpdateImageViewportConstraint();
            Scroller.SizeChanged += (_, __) => UpdateImageViewportConstraint();
            Scroller.PreviewMouseLeftButtonDown += Scroller_OnPreviewMouseLeftButtonDown;
        }

        /// <summary>
        /// Opens a single image (no gallery chrome).
        /// </summary>
        public static void ShowForUrl(Window owner, string? title, string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return;
            ShowForUrls(owner, title, new[] { url.Trim() }, 0);
        }

        /// <summary>
        /// Opens preview with optional prev/next between URLs (e.g. destination image strip).
        /// </summary>
        public static void ShowForUrls(Window owner, string? title, IReadOnlyList<string>? urls, int initialIndex = 0)
        {
            var ordered = new List<string>();
            if (urls != null)
            {
                foreach (var u in urls)
                {
                    var t = (u ?? "").Trim();
                    if (t.Length == 0)
                        continue;
                    if (ordered.Exists(x => string.Equals(x, t, StringComparison.OrdinalIgnoreCase)))
                        continue;
                    ordered.Add(t);
                }
            }

            if (ordered.Count == 0)
                return;

            var idx = Math.Clamp(initialIndex, 0, ordered.Count - 1);
            var w = new ImagePreviewWindow { Owner = owner };
            w.AttachGallery(ordered, idx, title);
            w.ShowDialog();
        }

        private void AttachGallery(List<string> urls, int index, string? title)
        {
            _galleryUrls = urls;
            _galleryIndex = index;
            _galleryHeaderTitle = title;
            ApplyGalleryChrome();
            SetTitle(title, urls[index]);
            ZoomSlider.Value = 1.0;
            LoadFromUrl(urls[index]);
        }

        private void ApplyGalleryChrome()
        {
            var multi = _galleryUrls.Count > 1;
            if (NavPanel != null)
                NavPanel.Visibility = multi ? Visibility.Visible : Visibility.Collapsed;

            if (GalleryOverlayNav != null)
                GalleryOverlayNav.Visibility = multi ? Visibility.Visible : Visibility.Collapsed;

            if (PositionText != null)
                PositionText.Text = multi ? $"{_galleryIndex + 1} / {_galleryUrls.Count}" : "";

            var canPrev = multi && _galleryIndex > 0;
            var canNext = multi && _galleryIndex < _galleryUrls.Count - 1;

            if (PrevNavButton != null)
                PrevNavButton.IsEnabled = canPrev;
            if (NextNavButton != null)
                NextNavButton.IsEnabled = canNext;
            if (OverlayPrevButton != null)
                OverlayPrevButton.IsEnabled = canPrev;
            if (OverlayNextButton != null)
                OverlayNextButton.IsEnabled = canNext;

            if (KeyboardHintsLine2 != null)
                KeyboardHintsLine2.Visibility = multi ? Visibility.Visible : Visibility.Collapsed;
        }

        private void NavigateToGalleryIndex(int newIndex)
        {
            if (_galleryUrls.Count <= 1)
                return;

            var idx = Math.Clamp(newIndex, 0, _galleryUrls.Count - 1);
            if (idx == _galleryIndex)
                return;

            _galleryIndex = idx;
            ApplyGalleryChrome();
            SetTitle(_galleryHeaderTitle, _galleryUrls[_galleryIndex]);
            ZoomSlider.Value = 1.0;
            LoadFromUrl(_galleryUrls[_galleryIndex]);
        }

        private void NavigateGallery(int delta) => NavigateToGalleryIndex(_galleryIndex + delta);

        private void PrevNav_Click(object sender, RoutedEventArgs e) => NavigateGallery(-1);

        private void NextNav_Click(object sender, RoutedEventArgs e) => NavigateGallery(1);

        /// <summary>
        /// Without MaxWidth/MaxHeight, <see cref="Image"/> uses the bitmap's intrinsic pixel size (often huge),
        /// so the preview opens mostly off-screen. Clamp to the scroll viewer viewport so zoom 1 ≈ "fits window".
        /// </summary>
        private void UpdateImageViewportConstraint()
        {
            if (Scroller == null || PreviewImage == null)
                return;

            // Padding: outer Border 18 + inner card 14 each side (approx).
            const double pad = 68;
            var vw = Scroller.ViewportWidth;
            var vh = Scroller.ViewportHeight;
            if (double.IsNaN(vw) || double.IsInfinity(vw) || vw <= 0)
                vw = 800;
            if (double.IsNaN(vh) || double.IsInfinity(vh) || vh <= 0)
                vh = 600;

            var w = Math.Max(120, vw - pad);
            var h = Math.Max(120, vh - pad);
            PreviewImage.MaxWidth = w;
            PreviewImage.MaxHeight = h;
        }

        private void SetTitle(string? title, string url)
        {
            if (!string.IsNullOrWhiteSpace(title))
                TitleText.Text = title.Trim();
            SubtitleText.Text = url;
        }

        private void LoadFromUrl(string url)
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            PreviewImage.Source = null;

            try
            {
                var sourceUrl = url?.Trim() ?? "";
                if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri))
                {
                    MessageBox.Show("Invalid image url.", "Preview", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // If it's already proxy, use directly; otherwise proxy it.
                var isAlreadyProxy =
                    string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase) &&
                    uri.AbsolutePath.Equals("/api/images/proxy", StringComparison.OrdinalIgnoreCase);

                var final = isAlreadyProxy
                    ? uri
                    : new Uri($"{ApiBaseUrl}/api/images/proxy?url={Uri.EscapeDataString(uri.ToString())}");

                // Same strategy as TripPackageToImageSourceConverter: OnDemand + async events is unreliable
                // for localhost proxy / some CDNs; OnLoad decodes during EndInit on the UI thread.
                try
                {
                    PreviewImage.Source = CreateBitmapFromUri(final);
                }
                catch
                {
                    PreviewImage.Source = CreateBitmapFromUriViaHttp(final);
                }

                Dispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        UpdateImageViewportConstraint();
                        Scroller?.ScrollToHorizontalOffset(0);
                        Scroller?.ScrollToVerticalOffset(0);
                    }),
                    DispatcherPriority.Loaded);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not load image.\n{ex.Message}",
                    "Preview",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private static BitmapImage CreateBitmapFromUri(Uri final)
        {
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = final;
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            bi.EndInit();
            if (bi.CanFreeze)
                bi.Freeze();
            return bi;
        }

        private static BitmapImage CreateBitmapFromUriViaHttp(Uri final)
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            var bytes = http.GetByteArrayAsync(final).GetAwaiter().GetResult();
            if (bytes.Length == 0)
                throw new InvalidOperationException("Empty image response.");

            BitmapImage bi;
            using (var ms = new MemoryStream(bytes, writable: false))
            {
                bi = new BitmapImage();
                bi.BeginInit();
                bi.StreamSource = ms;
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                bi.EndInit();
            }

            if (bi.CanFreeze)
                bi.Freeze();
            return bi;
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void Scroller_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;
            if ((Keyboard.Modifiers & ModifierKeys.Alt) == 0)
                return;

            Close();
            e.Handled = true;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
                return;
            }

            if (_galleryUrls.Count <= 1)
                return;

            switch (e.Key)
            {
                case Key.Left:
                    NavigateGallery(-1);
                    e.Handled = true;
                    break;
                case Key.Right:
                    NavigateGallery(1);
                    e.Handled = true;
                    break;
                case Key.PageUp:
                    NavigateGallery(-1);
                    e.Handled = true;
                    break;
                case Key.PageDown:
                    NavigateGallery(1);
                    e.Handled = true;
                    break;
                case Key.Home:
                    NavigateToGalleryIndex(0);
                    e.Handled = true;
                    break;
                case Key.End:
                    NavigateToGalleryIndex(_galleryUrls.Count - 1);
                    e.Handled = true;
                    break;
            }
        }

        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // During XAML load, ValueChanged can fire before named elements are wired up.
            if (Scale == null || ZoomSlider == null)
                return;

            var v = ZoomSlider.Value;
            Scale.ScaleX = v;
            Scale.ScaleY = v;
        }

        private void Scroller_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
                return;

            var delta = e.Delta > 0 ? 0.12 : -0.12;
            var next = Math.Clamp(ZoomSlider.Value + delta, ZoomSlider.Minimum, ZoomSlider.Maximum);
            ZoomSlider.Value = next;
            e.Handled = true;
        }
    }
}
