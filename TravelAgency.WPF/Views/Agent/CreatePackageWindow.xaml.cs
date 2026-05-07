using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TravelAgency.Core.Models;
using TravelAgency.Core.Models.Locations;
using TravelAgency.Core.Models.TripPkg.Package;
using TravelAgency.Core.Validators;
using FluentValidation;
using System.Windows.Input;
using TravelAgency.Core.Patterns.Facades;
using TravelAgency.Core.Patterns.Strategy;
using System.Net.Http;
using System.Text.Json;
using System.Diagnostics;
using System.IO;
using System.Windows.Media.Effects;
using System.Windows.Media.Animation;
using TravelAgency.Core.Data.Repositories;
using System.Windows.Threading;
using TravelAgency.Core.Patterns.Flyweight;

namespace TravelAgency.WPF.Views
{
    public partial class CreatePackageWindow : Window
    {
        /// <summary>Package stay category when the wizard no longer asks for hotel vs apartment etc.</summary>
        private const string DefaultStayCategory = "Lodging";

        private TripPackage? _editingTrip;

        private List<HotelSearchOption> _hotelResults = new();
        private string? _selectedHotelThumbnailUrl;
        private readonly TravelPackageFacade _facade;
        private List<LocationOption> _locationResults = new();
        private CancellationTokenSource? _locationSearchCts;
        private CancellationTokenSource? _countrySearchCts;
        private CountryOption? _selectedCountry;
        private List<LocationOption> _countryCities = new();
        private bool _suppressCountrySearch;
        private int currentStep = 1;
        private bool _isLoading;

        private bool _suppressHotelSearchLocationSync;
        private bool _hotelSearchLocationDirty;
        private bool _suppressHotelFilterTextChanged;

        /// <summary>Used to cancel in-flight Geo lookups when leaving Step 2 (their continuations would reopen popups over later steps).</summary>
        private int _lastWizardStep = 1;

        // In DEBUG, `App.TryStartApiForDev()` runs API with launch-profile "https" on https://localhost:7210.
        // Keep this aligned so destination media requests actually reach the running API.
        // Use HTTP for local dev to avoid WPF failing on untrusted dev HTTPS certs.
        private const string ApiBaseUrl = "http://localhost:5280";
        private readonly HttpClient _apiHttp = new() { Timeout = TimeSpan.FromSeconds(30) };
        private readonly ITripPackageRepository _tripRepo;
        private CancellationTokenSource? _destinationMediaCts;
        private string? _selectedDestinationCoverUrl;
        private string? _selectedDestinationCoverPreviewUrl;
        private string? _lastPreviewRequestedUrl;
        private bool _coverPickedByUser;
        private Border? _selectedDestinationThumbBorder;
        private Border? _selectedDestinationThumbBadge;
        private readonly SemaphoreSlim _thumbLoadGate = new(4, 4);
        private readonly SemaphoreSlim _proxyLoadGate = new(4, 4);

        private readonly DispatcherTimer _toastTimer = new() { Interval = TimeSpan.FromSeconds(2.2) };
        private readonly DispatcherTimer _autoSaveTimer = new() { Interval = TimeSpan.FromSeconds(1.3) };
        private bool _autoSavePending;
        private int _lastAutoSavedHash;
        private bool _autoSaveInputsHooked;

        private static string NormalizeCoverUrlForWpf(string url)
        {
            try
            {
                var s = (url ?? "").Trim();
                if (s.Length == 0)
                    return s;

                if (!Uri.TryCreate(s, UriKind.Absolute, out var uri))
                    return s;

                // For Unsplash (including plus.unsplash.com), force jpg output to avoid WPF decode failures.
                if (uri.Host.EndsWith("unsplash.com", StringComparison.OrdinalIgnoreCase))
                {
                    s = s.Replace("auto=format", "auto=compress", StringComparison.OrdinalIgnoreCase);
                    if (s.Contains("fm=webp", StringComparison.OrdinalIgnoreCase))
                        s = s.Replace("fm=webp", "fm=jpg", StringComparison.OrdinalIgnoreCase);
                    if (s.Contains("fm=avif", StringComparison.OrdinalIgnoreCase))
                        s = s.Replace("fm=avif", "fm=jpg", StringComparison.OrdinalIgnoreCase);

                    if (!s.Contains("fm=", StringComparison.OrdinalIgnoreCase))
                        s += (s.Contains('?', StringComparison.Ordinal) ? "&" : "?") + "fm=jpg";
                }

                return s;
            }
            catch
            {
                return (url ?? "").Trim();
            }
        }

        public CreatePackageWindow(
            TravelPackageFacade facade,
            ITripPackageRepository tripRepo,
            TripPackage? tripToEdit = null,
            int initialStep = 1)
        {
            _facade = facade ?? throw new System.ArgumentNullException(nameof(facade));
            _tripRepo = tripRepo ?? throw new ArgumentNullException(nameof(tripRepo));
            InitializeComponent();

            BasePriceTextBox.TextChanged += (s, e) => RecalculatePrice();
            DiscountTextBox.TextChanged += (s, e) => RecalculatePrice();
            VatTextBox.TextChanged += (s, e) => RecalculatePrice();
            ExtraChargesTextBox.TextChanged += (s, e) => RecalculatePrice();

            AirportTransferCheckBox.Checked += (s, e) => RecalculatePrice();
            AirportTransferCheckBox.Unchecked += (s, e) => RecalculatePrice();

            TravelInsuranceCheckBox.Checked += (s, e) => RecalculatePrice();
            TravelInsuranceCheckBox.Unchecked += (s, e) => RecalculatePrice();

            TourGuideCheckBox.Checked += (s, e) => RecalculatePrice();
            TourGuideCheckBox.Unchecked += (s, e) => RecalculatePrice();

            FreeCancellationCheckBox.Checked += (s, e) => RecalculatePrice();
            FreeCancellationCheckBox.Unchecked += (s, e) => RecalculatePrice();

            _editingTrip = tripToEdit;
            _coverPickedByUser = !string.IsNullOrWhiteSpace(_editingTrip?.CoverImageUrl);

            if (_editingTrip != null)
            {
                LoadTripIntoForm();

                if (initialStep >= 1 && initialStep <= 5)
                    currentStep = initialStep;
            }

            UpdateWizardUI();
            UpdateLeftPreview();

            SyncHotelSearchLocationFromDestination(resetDirty: true);
            UpdateHotelSearchUiState();

            // Destination media is loaded on selection/date changes.

            Loaded += async (_, __) =>
            {
                // Ensure we fetch images/hotels even when opening directly on Step 3 (edit mode),
                // where SelectionChanged might not fire.
                await LoadDestinationMediaAsync();

                // Best-effort: reselect hotel in list based on current accommodation name.
                RestoreHotelSelectionFromAccommodationName();
            };

            // Non-blocking toast hide
            _toastTimer.Tick += (_, __) =>
            {
                _toastTimer.Stop();
                HideToast();
            };

            // Draft saving currently only supported for Edit (existing package id).
            if (SaveDraftButton != null)
                SaveDraftButton.IsEnabled = true;

            // Autosave: only in edit mode (we have an id to update).
            _autoSaveTimer.Tick += async (_, __) =>
            {
                _autoSaveTimer.Stop();
                if (_autoSavePending)
                {
                    _autoSavePending = false;
                    await TrySaveDraftAsync(showToast: false);
                }
            };

            HookAutoSaveInputsOnce();
        }

        private void HookAutoSaveInputsOnce()
        {
            if (_autoSaveInputsHooked)
                return;
            _autoSaveInputsHooked = true;

            void onText(object? _, TextChangedEventArgs __) => ScheduleAutoSave();
            void onSel(object? _, SelectionChangedEventArgs __) => ScheduleAutoSave();

            PackageNameTextBox.TextChanged += onText;
            ShortDescriptionTextBox.TextChanged += onText;
            TripTypeComboBox.SelectionChanged += onSel;
            CategoryComboBox.SelectionChanged += onSel;
            DestinationComboBox.SelectionChanged += onSel;
            CountryComboBox.SelectionChanged += onSel;
            StartDatePicker.SelectedDateChanged += (_, __) => ScheduleAutoSave();
            EndDatePicker.SelectedDateChanged += (_, __) => ScheduleAutoSave();
            AccommodationNameTextBox.TextChanged += onText;
            AvailableSeatsTextBox.TextChanged += onText;
            BasePriceTextBox.TextChanged += onText;
            DiscountTextBox.TextChanged += onText;
            VatTextBox.TextChanged += onText;
            ExtraChargesTextBox.TextChanged += onText;
        }

        private void ScheduleAutoSave()
        {
            if (_isLoading)
                return;

            // Autosave only after a draft exists (Id > 0). Until then, user can click "Save Draft" explicitly.
            if (_editingTrip == null || _editingTrip.Id <= 0)
                return;

            _autoSavePending = true;
            _autoSaveTimer.Stop();
            _autoSaveTimer.Start();
        }

        private void ShowToast(string message, bool isError = false)
        {
            if (ToastHost == null || ToastText == null || ToastIcon == null)
                return;

            ToastText.Text = message;
            ToastHost.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isError ? "#7F1D1D" : "#0F172A"));
            ToastIcon.Text = isError ? "\uEA39" : "\uE73E"; // error / check

            var fade = new DoubleAnimation
            {
                To = 1,
                Duration = TimeSpan.FromMilliseconds(140),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            ToastHost.BeginAnimation(OpacityProperty, fade);

            if (ToastTranslate != null)
            {
                var slide = new DoubleAnimation
                {
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(160),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                ToastTranslate.BeginAnimation(TranslateTransform.YProperty, slide);
            }

            _toastTimer.Stop();
            _toastTimer.Start();
        }

        private void HideToast()
        {
            if (ToastHost == null)
                return;

            var fade = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(180),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            ToastHost.BeginAnimation(OpacityProperty, fade);

            if (ToastTranslate != null)
            {
                var slide = new DoubleAnimation
                {
                    To = 16,
                    Duration = TimeSpan.FromMilliseconds(180),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                ToastTranslate.BeginAnimation(TranslateTransform.YProperty, slide);
            }
        }

        private Task<bool> TrySaveDraftAsync(bool showToast)
        {
            try
            {
                // Only save when we can build a valid request (keeps DB consistent).
                var request = BuildTripRequestFromForm();
                var validator = new TripRequestValidator();
                validator.ValidateAndThrow(request);

                // Avoid spamming DB with identical autosaves.
                var hash = HashRequest(request);
                if (!showToast && hash == _lastAutoSavedHash)
                    return Task.FromResult(true);

                if (_editingTrip != null && _editingTrip.Id > 0)
                {
                    _facade.CreateAndUpdatePackage(request, _editingTrip.Id);
                    _lastAutoSavedHash = hash;

                    if (showToast)
                        ShowToast("Draft saved.");

                    return Task.FromResult(true);
                }

                // Create-mode: first valid draft creates the package, then it behaves like edit mode.
                var created = _facade.CreateAndSavePackage(request);
                _editingTrip = created;
                _lastAutoSavedHash = hash;

                try
                {
                    Title = _editingTrip.Id > 0 ? "Edit Package" : "Create New Package";
                }
                catch
                {
                    // ignore
                }

                if (showToast)
                    ShowToast("Draft created.");

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                if (showToast)
                    ShowToast("Draft not saved: " + ex.Message, isError: true);
                return Task.FromResult(false);
            }
        }

        private bool TrySavePartialDraft(bool showToast)
        {
            try
            {
                // Minimal persistence for incomplete forms: store what we have with safe defaults.
                var name = (PackageNameTextBox?.Text ?? "").Trim();
                if (string.IsNullOrWhiteSpace(name))
                    name = "Draft package";

                var tripType = GetComboBoxText(TripTypeComboBox);
                if (string.IsNullOrWhiteSpace(tripType))
                    tripType = "City Break";

                var category = GetComboBoxText(CategoryComboBox);
                if (string.IsNullOrWhiteSpace(category))
                    category = "Standard";

                var shortDesc = (ShortDescriptionTextBox?.Text ?? "").Trim();
                if (shortDesc.Length == 0)
                    shortDesc = "Draft (incomplete).";

                var destination = GetDestinationCityQuery(DestinationComboBox?.Text ?? "");
                var country = (CountryComboBox?.Text ?? "").Trim();

                var start = StartDatePicker?.SelectedDate ?? DateTime.Today.AddDays(14);
                var end = EndDatePicker?.SelectedDate ?? start.AddDays(5);
                if (end < start) end = start.AddDays(3);

                var transport = GetComboBoxText(TransportTypeComboBox);
                if (string.IsNullOrWhiteSpace(transport))
                    transport = "Train";

                var departureCity = (DepartureCityTextBox?.Text ?? "").Trim();
                var accommodationName = (AccommodationNameTextBox?.Text ?? "").Trim();
                var mealPlan = GetComboBoxText(MealPlanComboBox);

                var seats = 0;
                int.TryParse((AvailableSeatsTextBox?.Text ?? "").Trim(), out seats);
                if (seats < 0) seats = 0;

                var basePrice = ParseDoubleOrZero((BasePriceTextBox?.Text ?? "").Trim());
                var discount = ParseDoubleOrZero((DiscountTextBox?.Text ?? "").Trim());
                var vat = ParseDoubleOrZero((VatTextBox?.Text ?? "").Trim());
                var extra = ParseDoubleOrZero((ExtraChargesTextBox?.Text ?? "").Trim());

                var sharedInfo = PackageSharedInfoFactorySingleton.Instance.GetOrCreate(
                    destination ?? "",
                    country ?? "",
                    departureCity,
                    accommodationName,
                    mealPlan,
                    transport,
                    DefaultStayCategory);

                if (_editingTrip == null || _editingTrip.Id <= 0)
                {
                    var draft = new TripPackage
                    {
                        Name = name,
                        TripType = tripType,
                        Category = category,
                        ShortDescription = shortDesc,
                        PricingNotes = "DRAFT",
                        BasePrice = basePrice,
                        Price = 0,
                        CoverImageUrl = _selectedDestinationCoverUrl,
                        SharedInfo = sharedInfo,
                        Season = new Season
                        {
                            Name = $"{(string.IsNullOrWhiteSpace(destination) ? "Draft" : destination)} trip",
                            StartDate = start.Date,
                            EndDate = end.Date
                        },
                        AvailableSeats = seats,
                        DiscountPercent = discount,
                        VatPercent = vat,
                        ExtraCharges = extra,
                        TransportDisplayName = transport,
                        StayDisplayName = DefaultStayCategory
                    };

                    _tripRepo.Add(draft);
                    _editingTrip = draft;
                    _lastAutoSavedHash = 0; // force next autosave to run once

                    if (showToast)
                        ShowToast("Partial draft created.");
                }
                else
                {
                    _editingTrip.Name = name;
                    _editingTrip.TripType = tripType;
                    _editingTrip.Category = category;
                    _editingTrip.ShortDescription = shortDesc;
                    _editingTrip.PricingNotes = "DRAFT";
                    _editingTrip.BasePrice = basePrice;
                    _editingTrip.CoverImageUrl = _selectedDestinationCoverUrl ?? _editingTrip.CoverImageUrl;
                    _editingTrip.SharedInfo = sharedInfo;
                    _editingTrip.Season = new Season
                    {
                        Name = $"{(string.IsNullOrWhiteSpace(destination) ? "Draft" : destination)} trip",
                        StartDate = start.Date,
                        EndDate = end.Date
                    };
                    _editingTrip.AvailableSeats = seats;
                    _editingTrip.DiscountPercent = discount;
                    _editingTrip.VatPercent = vat;
                    _editingTrip.ExtraCharges = extra;
                    _editingTrip.TransportDisplayName = transport;
                    _editingTrip.StayDisplayName = DefaultStayCategory;

                    _tripRepo.Update(_editingTrip);

                    if (showToast)
                        ShowToast("Partial draft saved.");
                }

                // Enable autosave once we have an Id.
                if (_editingTrip != null && _editingTrip.Id > 0)
                    ScheduleAutoSave();

                return true;
            }
            catch (Exception ex)
            {
                if (showToast)
                    ShowToast("Draft not saved: " + ex.Message, isError: true);
                return false;
            }
        }

        private static int HashRequest(TripRequest r)
        {
            unchecked
            {
                var h = 17;
                h = h * 23 + (r.PackageName ?? "").GetHashCode();
                h = h * 23 + (r.TripType ?? "").GetHashCode();
                h = h * 23 + (r.Category ?? "").GetHashCode();
                h = h * 23 + (r.ShortDescription ?? "").GetHashCode();
                h = h * 23 + (r.Destination ?? "").GetHashCode();
                h = h * 23 + (r.Country ?? "").GetHashCode();
                h = h * 23 + (r.CoverImageUrl ?? "").GetHashCode();
                h = h * 23 + (r.StartDate?.Date.GetHashCode() ?? 0);
                h = h * 23 + (r.EndDate?.Date.GetHashCode() ?? 0);
                h = h * 23 + r.AvailableSeats.GetHashCode();
                h = h * 23 + r.BasePrice.GetHashCode();
                h = h * 23 + r.DiscountPercent.GetHashCode();
                h = h * 23 + r.VatPercent.GetHashCode();
                h = h * 23 + r.ExtraCharges.GetHashCode();
                h = h * 23 + r.FinalPrice.GetHashCode();
                return h;
            }
        }

        private void RestoreHotelSelectionFromAccommodationName()
        {
            try
            {
                if (HotelsListBox == null || HotelsListBox.ItemsSource == null)
                    return;

                var target = (AccommodationNameTextBox?.Text ?? "").Trim();
                if (string.IsNullOrWhiteSpace(target))
                    return;

                // try exact then contains
                var items = HotelsListBox.ItemsSource.Cast<HotelSearchOption>().ToList();
                var match = items.FirstOrDefault(h =>
                                string.Equals((h.Name ?? "").Trim(), target, StringComparison.OrdinalIgnoreCase))
                            ?? items.FirstOrDefault(h =>
                                target.Contains((h.Name ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
                            ?? items.FirstOrDefault(h =>
                                (h.Name ?? "").Trim().Contains(target, StringComparison.OrdinalIgnoreCase));

                if (match == null)
                    return;

                HotelsListBox.SelectedItem = match;
                _selectedHotelThumbnailUrl = string.IsNullOrWhiteSpace(match.ThumbnailUrl) ? null : match.ThumbnailUrl;
                UpdateLeftPreview();
            }
            catch
            {
                // ignore
            }
        }

        private static string GetDestinationCityQuery(string raw)
        {
            var text = raw?.Trim() ?? string.Empty;
            if (text.Length == 0)
                return string.Empty;

            var commaIdx = text.IndexOf(',', StringComparison.Ordinal);
            if (commaIdx > 0)
                return text[..commaIdx].Trim();

            return text;
        }

        private string BuildDefaultHotelLocationQuery()
        {
            var city = GetDestinationCityQuery(DestinationComboBox.Text);
            var country = CountryComboBox.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(city))
                return string.Empty;

            if (string.IsNullOrWhiteSpace(country))
                return city;

            return $"{city}, {country}";
        }

        private void SyncHotelSearchLocationFromDestination(bool resetDirty)
        {
            if (HotelSearchLocationTextBox == null)
                return;

            if (resetDirty)
                _hotelSearchLocationDirty = false;

            if (_hotelSearchLocationDirty)
                return;

            var next = BuildDefaultHotelLocationQuery();
            if (string.Equals(HotelSearchLocationTextBox.Text?.Trim(), next, StringComparison.Ordinal))
                return;

            try
            {
                _suppressHotelSearchLocationSync = true;
                HotelSearchLocationTextBox.Text = next;
            }
            finally
            {
                _suppressHotelSearchLocationSync = false;
            }
        }

        private async Task LoadDestinationMediaAsync()
        {
            if (DestinationComboBox == null)
                return;

            var city = GetDestinationCityQuery(DestinationComboBox.Text);
            var country = CountryComboBox?.Text?.Trim();

            if (string.IsNullOrWhiteSpace(city))
            {
                // Still show a one-item strip when editing / a cover is already stored (Step 1 has no API city yet).
                TryRenderPersistedCoverStrip();
                if (DestinationImagesStrip?.Visibility != Visibility.Visible &&
                    !_coverPickedByUser &&
                    string.IsNullOrWhiteSpace(_editingTrip?.CoverImageUrl))
                {
                    _selectedDestinationCoverUrl = null;
                    _selectedDestinationCoverPreviewUrl = null;
                    PreviewImageOverlay.ClearValue(BackgroundProperty);
                }
                return;
            }

            _destinationMediaCts?.Cancel();
            _destinationMediaCts = new CancellationTokenSource();
            var ct = _destinationMediaCts.Token;

            try
            {
                // If we already have a persisted cover (edit mode), keep it visible even if API calls fail.
                if (string.IsNullOrWhiteSpace(_selectedDestinationCoverUrl) &&
                    !string.IsNullOrWhiteSpace(_editingTrip?.CoverImageUrl))
                {
                    _selectedDestinationCoverUrl = _editingTrip.CoverImageUrl.Trim();
                    _coverPickedByUser = true;
                    ApplyPreviewDestinationImage(_selectedDestinationCoverUrl);
                }

                var imagesUrl =
                    $"{ApiBaseUrl}/api/destinations/images" +
                    $"?city={Uri.EscapeDataString(city)}" +
                    (string.IsNullOrWhiteSpace(country) ? "" : $"&country={Uri.EscapeDataString(country)}") +
                    $"&limit=8";

                using var imgResp = await _apiHttp.GetAsync(imagesUrl, ct);
                var imgJson = await imgResp.Content.ReadAsStringAsync(ct);

                if (imgResp.IsSuccessStatusCode)
                {
                    var imgData = JsonSerializer.Deserialize<DestinationImagesApiResponse>(
                        imgJson,
                        new JsonSerializerOptions(JsonSerializerDefaults.Web));

                    var images = imgData?.images?
                        .Select(x => new DestinationImageOption(
                            Url: (x.url ?? "").Trim(),
                            ThumbUrl: string.IsNullOrWhiteSpace(x.thumbUrl) ? null : x.thumbUrl.Trim()))
                        .Where(x => x.Url.Length > 0)
                        .GroupBy(x => x.Url, StringComparer.OrdinalIgnoreCase)
                        .Select(g => g.First())
                        .Take(8)
                        .ToList() ?? new List<DestinationImageOption>();

                    // Keep the DB cover in the strip when API results don't include it (older hosts, seed variance).
                    if (!string.IsNullOrWhiteSpace(_selectedDestinationCoverUrl))
                    {
                        var persisted = _selectedDestinationCoverUrl.Trim();
                        var normP = NormalizeCoverUrlForWpf(persisted);
                        var has = images.Any(i =>
                            string.Equals(NormalizeCoverUrlForWpf(i.Url), normP, StringComparison.OrdinalIgnoreCase));
                        if (!has)
                            images.Insert(0, new DestinationImageOption(persisted, persisted));
                    }

                    // If the user already picked a cover, never override it on refresh (dates/destination reload).
                    if (_coverPickedByUser && !string.IsNullOrWhiteSpace(_selectedDestinationCoverUrl))
                    {
                        var match = images.FirstOrDefault(i =>
                            string.Equals(NormalizeCoverUrlForWpf(i.Url), _selectedDestinationCoverUrl, StringComparison.OrdinalIgnoreCase));
                        var preview = (match?.ThumbUrl ?? match?.Url ?? _selectedDestinationCoverPreviewUrl ?? _selectedDestinationCoverUrl)?.Trim();
                        _selectedDestinationCoverPreviewUrl = preview;
                        ApplyPreviewDestinationImage(preview);
                    }
                    else
                    {
                        // IMPORTANT: do NOT auto-pick/persist a cover.
                        // The cover should be set only by an explicit user click on a thumbnail.
                        // We only set a preview so the UI looks nice while prompting the user to choose.
                        _selectedDestinationCoverUrl = null;
                        _selectedDestinationCoverPreviewUrl =
                            PickFirstSupportedImageUrl(images.Select(i => i.ThumbUrl ?? i.Url).ToList());
                        ApplyPreviewDestinationImage(_selectedDestinationCoverPreviewUrl);
                    }

                    RenderDestinationThumbnails(images);
                }
                else
                {
                    if (!_coverPickedByUser)
                    {
                        _selectedDestinationCoverUrl = null;
                        _selectedDestinationCoverPreviewUrl = null;
                        ApplyPreviewDestinationImage(null);
                    }

                    TryRenderPersistedCoverStrip();
                }

                // Hotels (auto-populate using selected dates)
                var checkIn = StartDatePicker?.SelectedDate ?? DateTime.Today.AddDays(14);
                var checkOut = EndDatePicker?.SelectedDate ?? checkIn.AddDays(5);

                // SerpApi hotels rejects past check-in dates.
                if (checkIn.Date < DateTime.Today)
                    checkIn = DateTime.Today.AddDays(1);

                if (checkOut <= checkIn)
                    checkOut = checkIn.AddDays(3);

                var hotelsUrl =
                    $"{ApiBaseUrl}/api/destinations/hotels" +
                    $"?city={Uri.EscapeDataString(city)}" +
                    (string.IsNullOrWhiteSpace(country) ? "" : $"&country={Uri.EscapeDataString(country)}") +
                    $"&checkIn={Uri.EscapeDataString(checkIn.ToString("yyyy-MM-dd"))}" +
                    $"&checkOut={Uri.EscapeDataString(checkOut.ToString("yyyy-MM-dd"))}" +
                    $"&adults=2&limit=10";

                using var hResp = await _apiHttp.GetAsync(hotelsUrl, ct);
                var hJson = await hResp.Content.ReadAsStringAsync(ct);

                if (hResp.IsSuccessStatusCode)
                {
                    var hData = JsonSerializer.Deserialize<DestinationHotelsApiResponse>(
                        hJson,
                        new JsonSerializerOptions(JsonSerializerDefaults.Web));

                    var hotels = hData?.hotels?.Select(h => new HotelSearchOption
                    {
                        Name = h.name ?? "",
                        Description = h.description ?? "",
                        Link = h.link ?? "",
                        ThumbnailUrl = h.thumbnailUrl ?? "",
                        HotelClass = h.hotelClass,
                        PricePerNight = h.pricePerNight,
                        TotalPrice = h.totalPrice
                    }).ToList() ?? new List<HotelSearchOption>();

                    _hotelResults = hotels;
                    ApplyHotelResultsFilter();

                    // In edit mode, keep the chosen hotel thumbnail stable.
                    RestoreHotelSelectionFromAccommodationName();
                }
                else
                {
                    await TryFallbackHotelSearchAsync(ct);
                }
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            catch
            {
                // Non-fatal: API might not be running. Unsplash-only: do not fallback to Wikipedia for covers.
                // If we're editing, try to restore hotel thumbnail by running a best-effort hotel search
                // via the existing facade (SerpApi) and then reselecting by accommodation name.
                await TryFallbackHotelSearchAsync(ct);
                TryRenderPersistedCoverStrip();
            }
        }

        /// <summary>
        /// When the destinations API is down or returns nothing, still show the persisted cover as a thumbnail row.
        /// </summary>
        private void TryRenderPersistedCoverStrip()
        {
            if (DestinationImagesStrip == null)
                return;

            var url = (_selectedDestinationCoverUrl ?? _editingTrip?.CoverImageUrl)?.Trim();
            if (string.IsNullOrWhiteSpace(url))
            {
                DestinationImagesStrip.Visibility = Visibility.Collapsed;
                return;
            }

            RenderDestinationThumbnails(new List<DestinationImageOption> { new DestinationImageOption(url, url) });
        }

        private async Task TryFallbackWikipediaCoverAsync(string city, string? country, CancellationToken ct)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_selectedDestinationCoverUrl))
                    return;

                // Wikipedia summary needs a *page title*, not an arbitrary "city,country" query.
                // We resolve a title first via OpenSearch, then fetch the summary thumbnail.
                var query = string.IsNullOrWhiteSpace(country) ? city : $"{city}, {country}";
                var title = await ResolveWikipediaTitleAsync(query, ct);
                if (string.IsNullOrWhiteSpace(title))
                    return;

                var src = await FetchWikipediaSummaryThumbnailAsync(title, ct);
                if (string.IsNullOrWhiteSpace(src))
                    return;

                _selectedDestinationCoverUrl = NormalizeCoverUrlForWpf(src);
                ApplyPreviewDestinationImage(_selectedDestinationCoverUrl);
            }
            catch
            {
                // ignore
            }
        }

        private async Task<string?> ResolveWikipediaTitleAsync(string query, CancellationToken ct)
        {
            try
            {
                var url =
                    "https://en.wikipedia.org/w/api.php" +
                    "?action=opensearch" +
                    "&limit=1" +
                    "&namespace=0" +
                    "&format=json" +
                    "&search=" + Uri.EscapeDataString(query);

                using var resp = await _apiHttp.GetAsync(url, ct);
                if (!resp.IsSuccessStatusCode)
                    return null;

                var json = await resp.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() < 2)
                    return null;

                var titles = doc.RootElement[1];
                if (titles.ValueKind != JsonValueKind.Array || titles.GetArrayLength() == 0)
                    return null;

                return titles[0].GetString();
            }
            catch
            {
                return null;
            }
        }

        private async Task<string?> FetchWikipediaSummaryThumbnailAsync(string title, CancellationToken ct)
        {
            try
            {
                var url = "https://en.wikipedia.org/api/rest_v1/page/summary/" + Uri.EscapeDataString(title);
                using var resp = await _apiHttp.GetAsync(url, ct);
                if (!resp.IsSuccessStatusCode)
                    return null;

                var json = await resp.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("thumbnail", out var thumb))
                    return null;
                if (!thumb.TryGetProperty("source", out var srcEl))
                    return null;

                var src = srcEl.GetString();
                return string.IsNullOrWhiteSpace(src) ? null : src;
            }
            catch
            {
                return null;
            }
        }

        private async Task TryFallbackHotelSearchAsync(CancellationToken ct)
        {
            try
            {
                var destination = BuildDefaultHotelLocationQuery();
                if (string.IsNullOrWhiteSpace(destination))
                    return;

                var checkIn = StartDatePicker?.SelectedDate;
                var checkOut = EndDatePicker?.SelectedDate;
                if (!checkIn.HasValue || !checkOut.HasValue || checkOut <= checkIn)
                    return;

                // Avoid spamming: only run fallback if we have no results yet.
                if (_hotelResults.Count > 0)
                    return;

                _hotelResults = await _facade.SearchHotelsAsync(
                    destination,
                    checkIn.Value,
                    checkOut.Value,
                    2);

                ApplyHotelResultsFilter();
                RestoreHotelSelectionFromAccommodationName();
            }
            catch
            {
                // ignore
            }
        }

        private void ApplyPreviewDestinationImage(string? url)
        {
            if (PreviewImageOverlay == null)
                return;

            if (string.IsNullOrWhiteSpace(url))
            {
                PreviewImageOverlay.Background = new SolidColorBrush(Color.FromRgb(0xDC, 0xEB, 0xFA));
                if (PreviewDestinationCodeText != null)
                    PreviewDestinationCodeText.Opacity = 1;
                if (PreviewImageDimmer != null)
                    PreviewImageDimmer.Opacity = 0;
                return;
            }

            // Fire-and-forget async load (keeps UI responsive).
            _ = ApplyPreviewDestinationImageAsync(url);
        }

        private async Task ApplyPreviewDestinationImageAsync(string url)
        {
            try
            {
                if (PreviewImageOverlay == null)
                    return;

                // Do not bind preview loading to the destination CTS.
                // That CTS gets cancelled when navigating steps / editing fields, which would cancel image downloads mid-flight.
                _lastPreviewRequestedUrl = url;
                var bitmap = await LoadBitmapFromProxyAsync(url, decodePixelWidth: 900, CancellationToken.None);
                if (bitmap == null)
                {
                    // Transient failure (429 / timeout). Keep current preview instead of hard-failing.
                    // If there was no previous image, fall back to the placeholder state.
                    if (PreviewImageOverlay.Background == null)
                        ApplyPreviewDestinationImage(null);
                    return;
                }

                // Last-request-wins guard (ignore slow older downloads).
                if (!string.Equals(_lastPreviewRequestedUrl, url, StringComparison.Ordinal))
                    return;

                // Fade out old, swap, fade in new
                PreviewImageOverlay.BeginAnimation(OpacityProperty, null);
                PreviewImageOverlay.Opacity = 0;
                PreviewImageOverlay.Background = new ImageBrush(bitmap)
                {
                    Stretch = Stretch.UniformToFill
                };
                PreviewImageOverlay.BeginAnimation(
                    OpacityProperty,
                    new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180))
                    {
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    });

                // When an image is present, fade out the big destination text
                // and show a subtle gradient dimmer for readability.
                if (PreviewDestinationCodeText != null)
                    PreviewDestinationCodeText.Opacity = 0;
                if (PreviewImageDimmer != null)
                    PreviewImageDimmer.Opacity = 1;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Preview image failed: " + ex);
                if (PreviewImageOverlay != null)
                    PreviewImageOverlay.Background = new SolidColorBrush(Color.FromRgb(0xDC, 0xEB, 0xFA));
                if (PreviewDestinationCodeText != null)
                    PreviewDestinationCodeText.Opacity = 1;
                if (PreviewImageDimmer != null)
                    PreviewImageDimmer.Opacity = 0;
            }
        }

        private async Task<BitmapImage?> LoadBitmapFromProxyAsync(string sourceUrl, int decodePixelWidth, CancellationToken ct)
        {
            await _proxyLoadGate.WaitAsync(ct);
            try
            {
                var finalUrl = $"{ApiBaseUrl}/api/images/proxy?url={Uri.EscapeDataString(sourceUrl)}";
                for (var attempt = 0; attempt < 4; attempt++)
                {
                    // Separate "user cancelled" from "slow network":
                    // we still allow enough time for image bytes to arrive.
                    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(45));
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

                    using var resp = await _apiHttp.GetAsync(finalUrl, HttpCompletionOption.ResponseHeadersRead, linkedCts.Token);
                    if (!resp.IsSuccessStatusCode)
                    {
                        Debug.WriteLine($"Proxy bitmap HTTP {(int)resp.StatusCode} {resp.ReasonPhrase} for {finalUrl}");
                        if ((int)resp.StatusCode == 429 && attempt < 3)
                        {
                            // Respect Retry-After when present, otherwise exponential backoff.
                            var delay = resp.Headers.RetryAfter?.Delta ?? TimeSpan.FromMilliseconds(600 * (attempt + 1));
                            if (delay < TimeSpan.FromMilliseconds(450)) delay = TimeSpan.FromMilliseconds(450);
                            if (delay > TimeSpan.FromSeconds(6)) delay = TimeSpan.FromSeconds(6);
                            await Task.Delay(delay, ct);
                            continue;
                        }
                        return null;
                    }

                    var bytes = await resp.Content.ReadAsByteArrayAsync(linkedCts.Token);
                    if (bytes == null || bytes.Length == 0)
                    {
                        Debug.WriteLine($"Proxy bitmap empty body for {finalUrl}");
                        return null;
                    }

                    await using var ms = new MemoryStream(bytes);
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    // With StreamSource, IgnoreImageCache can trigger internal cache operations that expect a Uri key.
                    // Keeping default CreateOptions avoids ArgumentNullException in BitmapImage.FinalizeCreation().
                    bitmap.DecodePixelWidth = decodePixelWidth;
                    bitmap.StreamSource = ms;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return bitmap;
                }

                return null;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // User changed destination / closed window; not an error.
                return null;
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                // Timeout / connection aborted (often happens under load). Treat as transient failure.
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Proxy bitmap failed: " + ex);
                return null;
            }
            finally
            {
                _proxyLoadGate.Release();
            }
        }

        private static string? PickFirstSupportedImageUrl(List<string> urls)
        {
            if (urls == null || urls.Count == 0)
                return null;

            static bool LooksSupported(string u)
            {
                var x = (u ?? "").Trim();
                if (x.Length == 0) return false;
                if (Uri.TryCreate(x, UriKind.Absolute, out var uri))
                {
                    var ext = System.IO.Path.GetExtension(uri.AbsolutePath).ToLowerInvariant();
                    return ext is not ".webp" && ext is not ".avif" && ext is not ".svg";
                }
                return true;
            }

            return urls.FirstOrDefault(LooksSupported) ?? urls.FirstOrDefault();
        }

        private void RenderDestinationThumbnails(List<DestinationImageOption> images)
        {
            if (DestinationImagesStrip == null)
                return;

            DestinationImagesStrip.Items.Clear();
            _selectedDestinationThumbBorder = null;
            _selectedDestinationThumbBadge = null;

            if (images.Count == 0)
            {
                DestinationImagesStrip.Visibility = Visibility.Collapsed;
                return;
            }

            DestinationImagesStrip.Visibility = Visibility.Visible;

            foreach (var imgOpt in images.Take(6))
            {
                var fullUrl = imgOpt.Url;
                var thumbUrl = imgOpt.ThumbUrl ?? imgOpt.Url;
                var normalizedThumbUrl = NormalizeCoverUrlForWpf(thumbUrl);

                var b = new Border
                {
                    Width = 48,
                    Height = 48,
                    CornerRadius = new CornerRadius(14),
                    Margin = new Thickness(0, 0, 8, 0),
                    Background = new SolidColorBrush(Color.FromRgb(0xEE, 0xF2, 0xFF)),
                    BorderBrush = new SolidColorBrush(Colors.Transparent),
                    BorderThickness = new Thickness(1),
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    RenderTransform = new ScaleTransform(1, 1),
                    Cursor = Cursors.Hand,
                    ToolTip = "Set as cover"
                };

                var grid = new Grid();

                var img = new Image { Stretch = Stretch.UniformToFill };
                _ = LoadThumbAsync(img, normalizedThumbUrl);

                grid.Children.Add(img);

                var badge = new Border
                {
                    Width = 16,
                    Height = 16,
                    CornerRadius = new CornerRadius(8),
                    Background = new SolidColorBrush(Color.FromRgb(0x2F, 0x80, 0xED)),
                    BorderBrush = new SolidColorBrush(Colors.White),
                    BorderThickness = new Thickness(1),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 3, 3, 0),
                    Opacity = 0
                };
                badge.Child = new TextBlock
                {
                    // Segoe MDL2 Assets checkmark
                    Text = "\uE73E",
                    Foreground = new SolidColorBrush(Colors.White),
                    FontSize = 10,
                    FontFamily = new FontFamily("Segoe MDL2 Assets"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center
                };

                grid.Children.Add(badge);

                b.Child = grid;
                b.MouseLeftButtonUp += (_, __) =>
                {
                    _selectedDestinationCoverUrl = NormalizeCoverUrlForWpf(fullUrl); // persist full (normalized)
                    _selectedDestinationCoverPreviewUrl = normalizedThumbUrl; // render thumb (normalized)
                    _coverPickedByUser = true;
                    ApplyPreviewDestinationImage(_selectedDestinationCoverPreviewUrl);
                    SetSelectedDestinationThumbnail(b, badge);

                    // If we're editing an existing package, persist the cover immediately so the dashboard
                    // doesn't "revert" on refresh/search even if the user navigates away before Step 5.
                    try
                    {
                        if (_editingTrip != null && _editingTrip.Id > 0)
                        {
                            _editingTrip.CoverImageUrl = _selectedDestinationCoverUrl;
                            _tripRepo.Update(_editingTrip);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Persist cover-on-click failed: " + ex);
                    }
                };

                b.MouseEnter += (_, __) => ApplyDestinationThumbHover(b, isHover: true);
                b.MouseLeave += (_, __) => ApplyDestinationThumbHover(b, isHover: false);

                DestinationImagesStrip.Items.Add(b);

                // Auto-select the current cover on first render (compare normalized URLs on both sides).
                if (_selectedDestinationThumbBorder == null &&
                    string.Equals(
                        NormalizeCoverUrlForWpf(_selectedDestinationCoverUrl ?? ""),
                        NormalizeCoverUrlForWpf(fullUrl),
                        StringComparison.OrdinalIgnoreCase))
                {
                    SetSelectedDestinationThumbnail(b, badge);
                }
            }

            // If we didn't match (e.g. first load), select the first thumbnail.
            if (_selectedDestinationThumbBorder == null && DestinationImagesStrip.Items.Count > 0)
            {
                if (DestinationImagesStrip.Items[0] is Border first &&
                    first.Child is Grid g &&
                    g.Children.OfType<Border>().FirstOrDefault() is Border firstBadge)
                {
                    SetSelectedDestinationThumbnail(first, firstBadge);
                }
            }
        }

        private void SetSelectedDestinationThumbnail(Border selected, Border badge)
        {
            if (_selectedDestinationThumbBorder != null)
            {
                _selectedDestinationThumbBorder.BorderBrush = new SolidColorBrush(Colors.Transparent);
                _selectedDestinationThumbBorder.BorderThickness = new Thickness(1);
                _selectedDestinationThumbBorder.Effect = null;
                ApplyDestinationThumbHover(_selectedDestinationThumbBorder, isHover: false);
                _selectedDestinationThumbBorder.ToolTip = "Set as cover";
            }

            if (_selectedDestinationThumbBadge != null)
                _selectedDestinationThumbBadge.Opacity = 0;

            _selectedDestinationThumbBorder = selected;
            _selectedDestinationThumbBadge = badge;

            selected.BorderBrush = new SolidColorBrush(Color.FromRgb(0x3B, 0x82, 0xF6));
            selected.BorderThickness = new Thickness(2.2);
            selected.Effect = new DropShadowEffect
            {
                Color = Color.FromRgb(0x3B, 0x82, 0xF6),
                Opacity = 0.22,
                BlurRadius = 7,
                ShadowDepth = 0
            };
            selected.ToolTip = "Cover selected";

            badge.Opacity = 1;
        }

        private void ApplyDestinationThumbHover(Border b, bool isHover)
        {
            if (b.RenderTransform is not ScaleTransform st)
                return;

            // Don't “fight” the selected styling; just add a tiny lift.
            var isSelected = ReferenceEquals(b, _selectedDestinationThumbBorder);
            var target = isHover ? (isSelected ? 1.04 : 1.07) : 1.0;
            st.ScaleX = target;
            st.ScaleY = target;

            if (!isSelected)
            {
                b.BorderBrush = isHover
                    ? new SolidColorBrush(Color.FromRgb(0xBF, 0xDB, 0xFE)) // light blue
                    : new SolidColorBrush(Colors.Transparent);
                b.BorderThickness = isHover ? new Thickness(1.5) : new Thickness(1);
                b.Effect = isHover
                    ? new DropShadowEffect { Color = Colors.Black, Opacity = 0.18, BlurRadius = 12, ShadowDepth = 0 }
                    : null;
            }
        }

        private async Task LoadThumbAsync(Image target, string thumbUrl)
        {
            try
            {
                await _thumbLoadGate.WaitAsync();
                var bitmap = await LoadBitmapFromProxyAsync(thumbUrl, decodePixelWidth: 120, CancellationToken.None);
                if (bitmap != null)
                    target.Source = bitmap;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Thumb load failed: " + ex);
            }
            finally
            {
                _thumbLoadGate.Release();
            }
        }

        // API DTOs
        private sealed class DestinationImagesApiResponse
        {
            public List<DestinationImageApiItem>? images { get; set; }
        }
        private sealed class DestinationImageApiItem
        {
            public string? url { get; set; }
            public string? thumbUrl { get; set; }
        }
        private sealed record DestinationImageOption(string Url, string? ThumbUrl);
        private sealed class DestinationHotelsApiResponse
        {
            public List<DestinationHotelApiItem>? hotels { get; set; }
        }
        private sealed class DestinationHotelApiItem
        {
            public string? name { get; set; }
            public string? description { get; set; }
            public string? link { get; set; }
            public string? thumbnailUrl { get; set; }
            public int? hotelClass { get; set; }
            public decimal? pricePerNight { get; set; }
            public decimal? totalPrice { get; set; }
        }

        private void ApplyHotelResultsFilter()
        {
            if (HotelsListBox == null)
                return;

            var previousSelection = HotelsListBox.SelectedItem as HotelSearchOption;

            if (_hotelResults.Count == 0)
            {
                HotelsListBox.ItemsSource = null;
                return;
            }

            var filter = HotelResultsFilterTextBox?.Text?.Trim() ?? string.Empty;
            if (filter.Length == 0)
            {
                HotelsListBox.ItemsSource = _hotelResults.Take(10).ToList();
                RestoreHotelListSelection(previousSelection);
                return;
            }

            var filtered = _hotelResults
                .Where(h =>
                {
                    var name = h.Name ?? string.Empty;
                    var desc = h.Description ?? string.Empty;
                    return name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                           desc.Contains(filter, StringComparison.OrdinalIgnoreCase);
                })
                .Take(10)
                .ToList();

            HotelsListBox.ItemsSource = filtered;
            RestoreHotelListSelection(previousSelection);
        }

        private void RestoreHotelListSelection(HotelSearchOption? previous)
        {
            if (previous == null || HotelsListBox.Items.Count == 0)
                return;

            foreach (var item in HotelsListBox.Items)
            {
                if (ReferenceEquals(item, previous))
                {
                    HotelsListBox.SelectedItem = item;
                    return;
                }
            }
        }

        private static LocationOption? FindMatchingLocation(IEnumerable<LocationOption> items, LocationOption? prior)
        {
            if (prior == null)
                return null;

            foreach (var x in items)
            {
                if (ReferenceEquals(x, prior))
                    return x;
            }

            foreach (var x in items)
            {
                if (string.Equals(x.City, prior.City, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(x.Country, prior.Country, StringComparison.OrdinalIgnoreCase))
                    return x;
            }

            return null;
        }

        private void RestoreDestinationListSelection(IEnumerable<LocationOption> items)
        {
            var prior = DestinationComboBox.SelectedItem as LocationOption;
            var match = FindMatchingLocation(items, prior);
            if (match != null)
                DestinationComboBox.SelectedItem = match;
        }

        private void UpdateHotelSearchUiState()
        {
            if (SearchHotelsButton == null || HotelSearchLocationTextBox == null || HotelResultsFilterTextBox == null)
                return;

            SearchHotelsButton.IsEnabled = true;
            HotelSearchLocationTextBox.IsEnabled = true;
            HotelResultsFilterTextBox.IsEnabled = true;
        }

        private TripRequest BuildTripRequestFromForm()
        {
            string packageName = PackageNameTextBox.Text.Trim();
            string tripType = GetComboBoxText(TripTypeComboBox);
            string category = GetComboBoxText(CategoryComboBox);
            string shortDescription = ShortDescriptionTextBox.Text.Trim();
            string destination = GetDestinationCityQuery(DestinationComboBox.Text);

            string country = CountryComboBox.Text.Trim();
            DateTime? startDate = StartDatePicker.SelectedDate;
            DateTime? endDate = EndDatePicker.SelectedDate;

            if (string.IsNullOrWhiteSpace(packageName))
                throw new InvalidOperationException("Package Name is required.");

            if (string.IsNullOrWhiteSpace(tripType))
                throw new InvalidOperationException("Trip Type is required.");

            if (string.IsNullOrWhiteSpace(category))
                throw new InvalidOperationException("Category is required.");

            if (string.IsNullOrWhiteSpace(destination))
                throw new InvalidOperationException("Destination is required.");

            if (string.IsNullOrWhiteSpace(country))
                throw new InvalidOperationException("Country is required.");

            if (!startDate.HasValue || !endDate.HasValue)
                throw new InvalidOperationException("Start Date and End Date are required.");

            if (endDate.Value < startDate.Value)
                throw new InvalidOperationException("End Date must be after Start Date.");

            int numberOfDays = 0;

            if (startDate.HasValue && endDate.HasValue)
            {
                numberOfDays = (endDate.Value - startDate.Value).Days + 1;
            }

            string transportType = GetComboBoxText(TransportTypeComboBox);
            string departureCity = DepartureCityTextBox.Text.Trim();
            string accommodationType = DefaultStayCategory;
            string accommodationName = AccommodationNameTextBox.Text.Trim();
            string mealPlan = GetComboBoxText(MealPlanComboBox);
            int availableSeats = ParseInt(AvailableSeatsTextBox.Text, "Available Seats");

            double basePrice = ParseDouble(BasePriceTextBox.Text, "Base Price");
            double discount = ParseDouble(DiscountTextBox.Text, "Discount");
            double vat = ParseDouble(VatTextBox.Text, "VAT");
            double extraCharges = ParseDouble(ExtraChargesTextBox.Text, "Extra Charges");

            decimal compositePrice = GetCompositeServicesPrice();

            decimal final = CalculatePriceWithStrategy(
                (decimal)basePrice,
                (decimal)discount,
                (decimal)vat,
                (decimal)extraCharges,
                compositePrice);

            double finalPrice = (double)final;

            return new TripRequest
            {
                PackageName = packageName,
                TripType = tripType,
                Category = category,
                ShortDescription = shortDescription,

                Destination = destination,
                Country = country,
                CoverImageUrl = _selectedDestinationCoverUrl,
                StartDate = startDate,
                EndDate = endDate,
                NumberOfDays = numberOfDays,

                TransportType = transportType,
                DepartureCity = departureCity,
                AccommodationType = accommodationType,
                AccommodationName = accommodationName,
                MealPlan = mealPlan,
                AvailableSeats = availableSeats,

                AirportTransfer = AirportTransferCheckBox.IsChecked == true,
                TravelInsurance = TravelInsuranceCheckBox.IsChecked == true,
                TourGuide = TourGuideCheckBox.IsChecked == true,
                FreeCancellation = FreeCancellationCheckBox.IsChecked == true,

                BasePrice = basePrice,
                DiscountPercent = discount,
                VatPercent = vat,
                ExtraCharges = extraCharges,
                FinalPrice = finalPrice
            };
        }

        private decimal CalculatePriceWithStrategy(
            decimal basePrice,
            decimal discount,
            decimal vat,
            decimal extraCharges,
            decimal compositePrice)
        {
            IPricingStrategy strategy;

            if (discount > 0 || vat > 0)
            {
                strategy = new FullPricingStrategy(discount, vat);
            }
            else
            {
                strategy = new StandardPricingStrategy();
            }

            var context = new PricingContext(strategy);

            decimal finalPrice = context.CalculateFinalPrice(basePrice, extraCharges);
            finalPrice += compositePrice;

            return finalPrice;
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            // Step-level validation before moving forward
            if (currentStep == 1)
            {
                if (!ValidateStep1())
                    return;
            }

            if (currentStep < 5)
            {
                currentStep++;
                UpdateWizardUI();
                UpdateReviewPanel();
            }
            else
            {
                try
                {
                    // Require a cover image selection so Agent "Recent packages" always shows the chosen cover.
                    // (WPF cannot reliably decode webp/avif; we force the selection pipeline to pick a compatible url.)
                    if (string.IsNullOrWhiteSpace(_selectedDestinationCoverUrl))
                    {
                        MessageBox.Show(
                            "Te rog alege o imagine de copertă pentru pachet (click pe una din imaginile destinației).",
                            "Copertă lipsă",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    var request = BuildTripRequestFromForm();

                    var validator = new TripRequestValidator();
                    validator.ValidateAndThrow(request);

                    TripPackage trip;

                    if (_editingTrip != null)
                    {
                        trip = _facade.CreateAndUpdatePackage(request, _editingTrip.Id);
                        ShowToast("Package updated.");
                    }
                    else
                    {
                        trip = _facade.CreateAndSavePackage(request);
                        ShowToast("Package created.");
                    }

                    DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private async void SaveDraftButton_Click(object sender, RoutedEventArgs e)
        {
            var ok = await TrySaveDraftAsync(showToast: true);
            if (!ok)
                TrySavePartialDraft(showToast: true);
        }

        private bool ValidateStep1()
        {
            var name = (PackageNameTextBox?.Text ?? "").Trim();
            var shortDesc = (ShortDescriptionTextBox?.Text ?? "").Trim();
            var tripType = (TripTypeComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
            var category = (CategoryComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";

            bool ok = true;

            // reset basic visual state
            if (PackageNameTextBox != null)
                PackageNameTextBox.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1D5DB"));
            if (TripTypeComboBox != null)
                TripTypeComboBox.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1D5DB"));
            if (CategoryComboBox != null)
                CategoryComboBox.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1D5DB"));
            if (ShortDescriptionTextBox != null)
                ShortDescriptionTextBox.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1D5DB"));

            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(name))
            {
                ok = false;
                errors.Add("Package name is required.");
                if (PackageNameTextBox != null)
                    PackageNameTextBox.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
            }

            if (string.IsNullOrWhiteSpace(tripType))
            {
                ok = false;
                errors.Add("Trip type must be selected.");
                if (TripTypeComboBox != null)
                    TripTypeComboBox.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
            }

            if (string.IsNullOrWhiteSpace(category))
            {
                ok = false;
                errors.Add("Category must be selected.");
                if (CategoryComboBox != null)
                    CategoryComboBox.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
            }

            if (shortDesc.Length < 15)
            {
                ok = false;
                errors.Add("Short description should have at least 15 characters.");
                if (ShortDescriptionTextBox != null)
                    ShortDescriptionTextBox.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
            }

            if (!ok)
            {
                MessageBox.Show(
                    string.Join("\n", errors),
                    "Please complete the basic info",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            return ok;
        }

        private async void DestinationComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading)
                return;

            // Only treat this as an active user choice on Step 2.
            // In edit mode we set Destination/Country programmatically and ItemSource changes can fire SelectionChanged.
            if (currentStep != 2)
            {
                SyncHotelSearchLocationFromDestination(resetDirty: false);
                UpdateHotelSearchUiState();
                UpdateLeftPreview();
                return;
            }

            if (DestinationComboBox.SelectedItem is LocationOption selectedLocation)
            {
                DestinationComboBox.Text = selectedLocation.City;
                if (_selectedCountry == null)
                    CountryComboBox.Text = selectedLocation.Country;

                DestinationComboBox.IsDropDownOpen = false;
            }

            SyncHotelSearchLocationFromDestination(resetDirty: true);
            UpdateHotelSearchUiState();
            UpdateLeftPreview();
            await LoadDestinationMediaAsync();

            if (currentStep == 5)
                UpdateReviewPanel();
        }

        private async void DatesChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading)
                return;

            UpdateNumberOfDays();
            SyncHotelSearchLocationFromDestination(resetDirty: false);
            UpdateHotelSearchUiState();
            UpdateLeftPreview();
            await LoadDestinationMediaAsync();

            if (currentStep == 5)
                UpdateReviewPanel();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentStep > 1)
            {
                currentStep--;
                UpdateWizardUI();
            }
        }

        private static string GetComboBoxText(ComboBox comboBox)
        {
            if (comboBox.SelectedItem is ComboBoxItem item)
                return item.Content?.ToString() ?? "";

            return comboBox.Text?.Trim() ?? "";
        }

        private static int ParseInt(string text, string fieldName)
        {
            if (!int.TryParse(text, out int value))
                throw new InvalidOperationException($"{fieldName} must be a valid integer.");

            return value;
        }

        private static double ParseDouble(string text, string fieldName)
        {
            if (!double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double value) &&
                !double.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out value))
            {
                throw new InvalidOperationException($"{fieldName} must be a valid number.");
            }

            return value;
        }

        private static double ParseDoubleOrZero(string text)
        {
            if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
                return value;

            if (double.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out value))
                return value;

            return 0;
        }

        private static decimal ParseDecimal(string text)
        {
            if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal value))
                return value;

            if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out value))
                return value;

            return 0m;
        }

        private void CancelPendingLocationLookupsAndCloseDropdowns()
        {
            try
            {
                _locationSearchCts?.Cancel();
            }
            catch
            {
            }

            try
            {
                _countrySearchCts?.Cancel();
            }
            catch
            {
            }

            if (DestinationComboBox != null)
                DestinationComboBox.IsDropDownOpen = false;

            if (CountryComboBox != null)
                CountryComboBox.IsDropDownOpen = false;
        }

        private void UpdateWizardUI()
        {
            if (_lastWizardStep == 2 && currentStep != 2)
                CancelPendingLocationLookupsAndCloseDropdowns();

            Step1Panel.Visibility = currentStep == 1 ? Visibility.Visible : Visibility.Collapsed;
            Step2Panel.Visibility = currentStep == 2 ? Visibility.Visible : Visibility.Collapsed;
            Step3Panel.Visibility = currentStep == 3 ? Visibility.Visible : Visibility.Collapsed;
            Step4Panel.Visibility = currentStep == 4 ? Visibility.Visible : Visibility.Collapsed;
            Step5Panel.Visibility = currentStep == 5 ? Visibility.Visible : Visibility.Collapsed;

            BackButton.IsEnabled = currentStep > 1;
            NextButton.Content = currentStep == 5 ? "Finish" : "Next";

            if (currentStep == 1)
            {
                Step1Circle.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2F80ED"));
                Step1Label.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2F80ED"));
                Step1Label.FontWeight = FontWeights.SemiBold;
            }
            else
            {
                Step1Circle.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E7EB"));
                Step1Label.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280"));
                Step1Label.FontWeight = FontWeights.Normal;
            }

            if (currentStep == 2)
            {
                Step2Circle.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2F80ED"));
                Step2Label.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2F80ED"));
                Step2Label.FontWeight = FontWeights.SemiBold;
            }
            else
            {
                Step2Circle.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E7EB"));
                Step2Label.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280"));
                Step2Label.FontWeight = FontWeights.Normal;
            }

            if (currentStep == 3)
            {
                Step3Circle.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2F80ED"));
                Step3Label.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2F80ED"));
                Step3Label.FontWeight = FontWeights.SemiBold;
            }
            else
            {
                Step3Circle.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E7EB"));
                Step3Label.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280"));
                Step3Label.FontWeight = FontWeights.Normal;
            }

            if (currentStep == 4)
            {
                Step4Circle.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2F80ED"));
                Step4Label.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2F80ED"));
                Step4Label.FontWeight = FontWeights.SemiBold;
            }
            else
            {
                Step4Circle.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E7EB"));
                Step4Label.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280"));
                Step4Label.FontWeight = FontWeights.Normal;
            }

            if (currentStep == 5)
            {
                Step5Circle.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2F80ED"));
                Step5Label.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2F80ED"));
                Step5Label.FontWeight = FontWeights.SemiBold;
            }
            else
            {
                Step5Circle.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E7EB"));
                Step5Label.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280"));
                Step5Label.FontWeight = FontWeights.Normal;
            }

            if (currentStep == 3)
            {
                SyncHotelSearchLocationFromDestination(resetDirty: false);
                UpdateHotelSearchUiState();
            }

            _lastWizardStep = currentStep;
        }

        private void UpdateReviewPanel()
        {
            ReviewPackageNameText.Text = GetSafeText(PackageNameTextBox.Text);
            ReviewTripTypeText.Text = GetSafeText(GetComboBoxText(TripTypeComboBox));
            ReviewCategoryText.Text = GetSafeText(GetComboBoxText(CategoryComboBox));
            ReviewDescriptionText.Text = GetSafeText(ShortDescriptionTextBox.Text);

            ReviewDestinationText.Text = GetSafeText(DestinationComboBox.Text);
            ReviewCountryText.Text = GetSafeText(CountryComboBox.Text);
            ReviewStartDateText.Text = StartDatePicker.SelectedDate?.ToString("dd MMM yyyy") ?? "-";
            ReviewEndDateText.Text = EndDatePicker.SelectedDate?.ToString("dd MMM yyyy") ?? "-";

            if (!string.IsNullOrWhiteSpace(NumberOfDaysTextBox.Text))
            {
                ReviewNumberOfDaysText.Text = NumberOfDaysTextBox.Text.Trim();
            }
            else if (StartDatePicker.SelectedDate.HasValue && EndDatePicker.SelectedDate.HasValue)
            {
                ReviewNumberOfDaysText.Text =
                    ((EndDatePicker.SelectedDate.Value - StartDatePicker.SelectedDate.Value).Days + 1).ToString();
            }
            else
            {
                ReviewNumberOfDaysText.Text = "-";
            }

            ReviewTransportText.Text = GetSafeText(GetComboBoxText(TransportTypeComboBox));
            ReviewDepartureCityText.Text = GetSafeText(DepartureCityTextBox.Text);
            ReviewAccommodationTypeText.Text = GetSafeText(DefaultStayCategory);
            ReviewAccommodationNameText.Text = GetSafeText(AccommodationNameTextBox.Text);
            ReviewMealPlanText.Text = GetSafeText(GetComboBoxText(MealPlanComboBox));
            ReviewAvailableSeatsText.Text = GetSafeText(AvailableSeatsTextBox.Text);

            ReviewServicesText.Text = BuildIncludedServicesText();

            double basePrice = ParseDoubleOrZero(BasePriceTextBox.Text);
            double discount = ParseDoubleOrZero(DiscountTextBox.Text);
            double vat = ParseDoubleOrZero(VatTextBox.Text);
            double extraCharges = ParseDoubleOrZero(ExtraChargesTextBox.Text);

            decimal compositePrice = GetCompositeServicesPrice();

            decimal final = CalculatePriceWithStrategy(
                (decimal)basePrice,
                (decimal)discount,
                (decimal)vat,
                (decimal)extraCharges,
                compositePrice);

            double finalPrice = (double)final;

            ReviewBasePriceText.Text = $"{basePrice:F2}";
            ReviewDiscountText.Text = $"{discount:F2}%";
            ReviewVatText.Text = $"{vat:F2}%";
            ReviewExtraChargesText.Text = $"{extraCharges:F2}";
            ReviewFinalPriceText.Text = $"{finalPrice:F2}";
        }

        private static string GetSafeText(string? text)
        {
            return string.IsNullOrWhiteSpace(text) ? "-" : text.Trim();
        }

        private string BuildIncludedServicesText()
        {
            var services = new List<string>();

            if (AirportTransferCheckBox.IsChecked == true)
                services.Add("Airport Transfer");

            if (TravelInsuranceCheckBox.IsChecked == true)
                services.Add("Travel Insurance");

            if (TourGuideCheckBox.IsChecked == true)
                services.Add("Tour Guide");

            if (FreeCancellationCheckBox.IsChecked == true)
                services.Add("Free Cancellation");

            return services.Count == 0 ? "-" : string.Join(", ", services);
        }

        private void UpdateLeftPreview()
        {
            string destination = GetSafeText(DestinationComboBox.Text);
            string packageName = GetSafeText(PackageNameTextBox.Text);
            string description = GetSafeText(ShortDescriptionTextBox.Text);

            string transport = GetSafeText(GetComboBoxText(TransportTypeComboBox));

            decimal basePrice = ParseDecimal(BasePriceTextBox.Text);
            decimal discount = ParseDecimal(DiscountTextBox.Text);
            decimal vat = ParseDecimal(VatTextBox.Text);
            decimal extraCharges = ParseDecimal(ExtraChargesTextBox.Text);
            decimal compositePrice = GetCompositeServicesPrice();

            decimal finalPrice = CalculatePriceWithStrategy(
                basePrice,
                discount,
                vat,
                extraCharges,
                compositePrice);

            PreviewDestinationCodeText.Text = destination == "-" ? "TRIP" : destination.ToUpperInvariant();
            PreviewPackageNameText.Text = packageName;
            PreviewDescriptionText.Text = description == "-" ? "Package preview" : description;

            var stayName = AccommodationNameTextBox.Text.Trim();
            PreviewTransportStayText.Text =
                string.IsNullOrWhiteSpace(stayName)
                    ? $"{transport} + stay"
                    : $"{transport} + stay ({stayName})";

            PreviewPriceText.Text = $"{finalPrice:F2}";
            TryUpdatePreviewImage();
        }

        private void TryUpdatePreviewImage()
        {
            try
            {
                if (PreviewImageOverlay == null)
                    return;

                // Prefer destination cover if we have one
                if (!string.IsNullOrWhiteSpace(_selectedDestinationCoverUrl))
                {
                    ApplyPreviewDestinationImage(_selectedDestinationCoverUrl);
                    return;
                }

                if (string.IsNullOrWhiteSpace(_selectedHotelThumbnailUrl))
                {
                    PreviewImageOverlay.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DCEBFA"));
                    return;
                }

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(_selectedHotelThumbnailUrl, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                PreviewImageOverlay.Background = new ImageBrush(bitmap)
                {
                    Stretch = Stretch.UniformToFill,
                    Opacity = 0.95
                };
            }
            catch
            {
                if (PreviewImageOverlay != null)
                    PreviewImageOverlay.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DCEBFA"));
            }
        }

        private void LoadTripIntoForm()
        {
            if (_editingTrip == null)
                return;

            _isLoading = true;

            PackageNameTextBox.Text = _editingTrip.Name;

            var tripType = !string.IsNullOrWhiteSpace(_editingTrip.TripType)
                ? _editingTrip.TripType
                : InferTripTypeFromName(_editingTrip.Name);

            var category = !string.IsNullOrWhiteSpace(_editingTrip.Category)
                ? _editingTrip.Category
                : InferCategoryFromName(_editingTrip.Name);

            SetComboBoxByText(TripTypeComboBox, tripType);
            SetComboBoxByText(CategoryComboBox, category);

            ShortDescriptionTextBox.Text = _editingTrip.ShortDescription ?? "";

            DestinationComboBox.Text = _editingTrip.Destination;
            CountryComboBox.Text = _editingTrip.Country;

            if (string.IsNullOrWhiteSpace(DestinationComboBox.Text) || string.IsNullOrWhiteSpace(CountryComboBox.Text))
            {
                var (dest, country) = InferDestinationCountryFromSeason(_editingTrip.Season?.Name);
                if (string.IsNullOrWhiteSpace(DestinationComboBox.Text))
                    DestinationComboBox.Text = dest;
                if (string.IsNullOrWhiteSpace(CountryComboBox.Text))
                    CountryComboBox.Text = country;
            }

            StartDatePicker.SelectedDate = _editingTrip.Season?.StartDate;
            EndDatePicker.SelectedDate = _editingTrip.Season?.EndDate;

            if (_editingTrip.Days != null && _editingTrip.Days.Count > 0)
            {
                NumberOfDaysTextBox.Text = _editingTrip.Days.Count.ToString();
            }
            else if (_editingTrip.Season != null)
            {
                NumberOfDaysTextBox.Text =
                    ((_editingTrip.Season.EndDate - _editingTrip.Season.StartDate).Days + 1).ToString();
            }
            else
            {
                NumberOfDaysTextBox.Text = "";
            }

            SetComboBoxByText(TransportTypeComboBox, NormalizeTransportName(_editingTrip.TransportName));
            DepartureCityTextBox.Text = _editingTrip.DepartureCity;

            AccommodationNameTextBox.Text = _editingTrip.AccommodationName;

            // Restore persisted destination cover for stable preview
            _selectedDestinationCoverUrl = string.IsNullOrWhiteSpace(_editingTrip.CoverImageUrl)
                ? null
                : _editingTrip.CoverImageUrl.Trim();
            ApplyPreviewDestinationImage(_selectedDestinationCoverUrl);

            var mealPlan = _editingTrip.MealPlan;
            if (string.IsNullOrWhiteSpace(mealPlan) &&
                _editingTrip.ExtraServiceNames.Any(x => x.Contains("Breakfast", StringComparison.OrdinalIgnoreCase)))
            {
                mealPlan = "Breakfast";
            }
            SetComboBoxByText(MealPlanComboBox, mealPlan);
            AvailableSeatsTextBox.Text = _editingTrip.AvailableSeats.ToString();

            AirportTransferCheckBox.IsChecked =
                _editingTrip.ExtraServiceNames.Any(x => x.Contains("Transfer", StringComparison.OrdinalIgnoreCase));

            TravelInsuranceCheckBox.IsChecked =
                _editingTrip.ExtraServiceNames.Any(x => x.Contains("Insurance", StringComparison.OrdinalIgnoreCase));

            TourGuideCheckBox.IsChecked =
                _editingTrip.ExtraServiceNames.Any(x => x.Contains("Guide", StringComparison.OrdinalIgnoreCase));

            FreeCancellationCheckBox.IsChecked =
                _editingTrip.ExtraServiceNames.Any(x => x.Contains("Cancellation", StringComparison.OrdinalIgnoreCase));

            var basePrice = _editingTrip.BasePrice > 0 ? _editingTrip.BasePrice : _editingTrip.Price;
            BasePriceTextBox.Text = basePrice.ToString("F2", CultureInfo.InvariantCulture);
            DiscountTextBox.Text = _editingTrip.DiscountPercent.ToString("F2", CultureInfo.InvariantCulture);
            VatTextBox.Text = _editingTrip.VatPercent.ToString("F2", CultureInfo.InvariantCulture);
            ExtraChargesTextBox.Text = _editingTrip.ExtraCharges.ToString("F2", CultureInfo.InvariantCulture);

            PricingNotesTextBox.Text = _editingTrip.PricingNotes ?? "";

            _isLoading = false;

            RecalculatePrice();
            UpdateLeftPreview();
            UpdateReviewPanel();

            Title = _editingTrip.Id > 0 ? "Edit Package" : "Create New Package";

            SyncHotelSearchLocationFromDestination(resetDirty: true);
            UpdateHotelSearchUiState();
        }

        private static void SetComboBoxByText(ComboBox comboBox, string text)
        {
            foreach (var item in comboBox.Items)
            {
                if (item is ComboBoxItem comboBoxItem &&
                    string.Equals(comboBoxItem.Content?.ToString(), text, StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedItem = comboBoxItem;
                    return;
                }
            }
        }

        private static string InferCategoryFromName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Standard";

            if (name.Contains("Premium", StringComparison.OrdinalIgnoreCase))
                return "Premium";

            if (name.Contains("Luxury", StringComparison.OrdinalIgnoreCase))
                return "Luxury";

            return "Standard";
        }

        private static string InferTripTypeFromName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "City Break";

            if (name.Contains("Beach", StringComparison.OrdinalIgnoreCase))
                return "Beach Holiday";

            if (name.Contains("Adventure", StringComparison.OrdinalIgnoreCase))
                return "Adventure";

            if (name.Contains("Cultural", StringComparison.OrdinalIgnoreCase))
                return "Cultural Tour";

            return "City Break";
        }

        private static string NormalizeTransportName(string transportName)
        {
            if (transportName.Contains("Plane", StringComparison.OrdinalIgnoreCase) ||
                transportName.Contains("Flight", StringComparison.OrdinalIgnoreCase))
                return "Flight";

            if (transportName.Contains("Bus", StringComparison.OrdinalIgnoreCase))
                return "Bus";

            if (transportName.Contains("Train", StringComparison.OrdinalIgnoreCase))
                return "Train";

            return "Own Transport";
        }

        private static (string destination, string country) InferDestinationCountryFromSeason(string? seasonName)
        {
            if (string.IsNullOrWhiteSpace(seasonName))
                return ("", "");

            var name = seasonName.Trim();
            if (name.EndsWith("trip", StringComparison.OrdinalIgnoreCase))
                name = name[..^3].Trim();

            var parts = name.Split(',', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
                return ("", "");

            return (parts[0], parts[1]);
        }

        private void UpdateNumberOfDays()
        {
            if (StartDatePicker.SelectedDate.HasValue &&
                EndDatePicker.SelectedDate.HasValue &&
                EndDatePicker.SelectedDate.Value >= StartDatePicker.SelectedDate.Value)
            {
                int days = (EndDatePicker.SelectedDate.Value - StartDatePicker.SelectedDate.Value).Days + 1;
                NumberOfDaysTextBox.Text = days.ToString();
            }
            else
            {
                NumberOfDaysTextBox.Text = "";
            }

            UpdateLeftPreview();
            if (currentStep == 5)
                UpdateReviewPanel();
        }

        private async void SearchHotelsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SyncHotelSearchLocationFromDestination(resetDirty: false);

                string destination =
                    (HotelSearchLocationTextBox?.Text?.Trim().Length > 0
                        ? HotelSearchLocationTextBox.Text.Trim()
                        : BuildDefaultHotelLocationQuery());

                DateTime? checkIn = StartDatePicker.SelectedDate;
                DateTime? checkOut = EndDatePicker.SelectedDate;

                if (string.IsNullOrWhiteSpace(destination))
                {
                    MessageBox.Show("Select a destination first.", "Accommodation search", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!checkIn.HasValue || !checkOut.HasValue)
                {
                    MessageBox.Show("Select start and end dates first.", "Accommodation search", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (checkOut.Value <= checkIn.Value)
                {
                    MessageBox.Show("End Date must be after Start Date.", "Accommodation search", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                SearchHotelsButton.IsEnabled = false;
                SearchHotelsButton.Content = "Searching...";
                HotelsListBox.ItemsSource = null;

                _hotelResults = await _facade.SearchHotelsAsync(
                    destination,
                    checkIn.Value,
                    checkOut.Value,
                    2);

                try
                {
                    _suppressHotelFilterTextChanged = true;
                    if (HotelResultsFilterTextBox != null)
                        HotelResultsFilterTextBox.Text = string.Empty;
                }
                finally
                {
                    _suppressHotelFilterTextChanged = false;
                }

                ApplyHotelResultsFilter();

                if (_hotelResults.Count == 0)
                {
                    MessageBox.Show(
                        "No places found for the selected location and dates.",
                        "Accommodation search",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Accommodation search", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                SearchHotelsButton.Content = "Search accommodation";
                UpdateHotelSearchUiState();
            }
        }

        private void HotelsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HotelsListBox.SelectedItem is not HotelSearchOption selectedHotel)
                return;

            AccommodationNameTextBox.Text = selectedHotel.Name ?? "";

            _selectedHotelThumbnailUrl = string.IsNullOrWhiteSpace(selectedHotel.ThumbnailUrl)
                ? null
                : selectedHotel.ThumbnailUrl;

            int nights = 1;

            if (StartDatePicker.SelectedDate.HasValue && EndDatePicker.SelectedDate.HasValue)
            {
                nights = (EndDatePicker.SelectedDate.Value - StartDatePicker.SelectedDate.Value).Days + 1;
                if (nights <= 0)
                    nights = 1;
            }

            decimal total = 0m;

            if (selectedHotel.TotalPrice.HasValue && selectedHotel.TotalPrice.Value > 0)
            {
                total = (decimal)selectedHotel.TotalPrice.Value;
            }
            else if (selectedHotel.PricePerNight.HasValue && selectedHotel.PricePerNight.Value > 0)
            {
                total = (decimal)selectedHotel.PricePerNight.Value * nights;
            }

            BasePriceTextBox.Text = total.ToString("F2", CultureInfo.InvariantCulture);

            RecalculatePrice();
            UpdateLeftPreview();

            if (currentStep == 5)
                UpdateReviewPanel();
        }

        private void DestinationComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isLoading)
                return;

            SyncHotelSearchLocationFromDestination(resetDirty: false);
        }

        private void HotelSearchLocationTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressHotelSearchLocationSync)
                return;

            _hotelSearchLocationDirty = true;
        }

        private void HotelResultsFilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressHotelFilterTextChanged)
                return;

            ApplyHotelResultsFilter();
        }

        private async void DestinationComboBox_KeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                if (currentStep != 2)
                    return;

                string query = DestinationComboBox.Text?.Trim() ?? string.Empty;

                // If a country was chosen, filter within loaded cities (fast, no API).
                if (_selectedCountry != null && _countryCities.Count > 0)
                {
                    if (query.Length == 0)
                    {
                        DestinationComboBox.ItemsSource = _countryCities;
                        DestinationComboBox.IsDropDownOpen = _countryCities.Count > 0;
                        RestoreDestinationListSelection(_countryCities);
                        return;
                    }

                    var filtered = _countryCities
                        .Where(x => (x.City ?? string.Empty).StartsWith(query, StringComparison.OrdinalIgnoreCase))
                        .Take(30)
                        .ToList();

                    DestinationComboBox.ItemsSource = filtered;
                    DestinationComboBox.IsDropDownOpen = filtered.Count > 0;
                    RestoreDestinationListSelection(filtered);
                    return;
                }

                if (query.Length < 3)
                {
                    DestinationComboBox.ItemsSource = null;
                    DestinationComboBox.IsDropDownOpen = false;
                    return;
                }

                _locationSearchCts?.Cancel();
                _locationSearchCts = new CancellationTokenSource();
                var token = _locationSearchCts.Token;

                await Task.Delay(600, token);

                if (token.IsCancellationRequested)
                    return;

                if (currentStep != 2)
                    return;

                _locationResults = await _facade.SearchLocationsAsync(query, 10);

                if (token.IsCancellationRequested)
                    return;

                if (currentStep != 2)
                    return;

                DestinationComboBox.ItemsSource = _locationResults;
                DestinationComboBox.IsDropDownOpen = _locationResults.Count > 0;
                RestoreDestinationListSelection(_locationResults);
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Location Search Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // DestinationComboBox_SelectionChanged handled earlier (async) to also load destination media.

        private async void CountryComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (_suppressCountrySearch)
                    return;

                if (_isLoading)
                    return;

                if (currentStep != 2)
                    return;

                var query = CountryComboBox.Text?.Trim() ?? string.Empty;

                if (_selectedCountry != null &&
                    !string.Equals(query, _selectedCountry.Name.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    _selectedCountry = null;
                    _countryCities = new List<LocationOption>();
                    DestinationComboBox.ItemsSource = null;
                    DestinationComboBox.SelectedItem = null;
                }

                if (query.Length < 2)
                {
                    CountryComboBox.IsDropDownOpen = false;
                    return;
                }

                _countrySearchCts?.Cancel();
                _countrySearchCts = new CancellationTokenSource();
                var token = _countrySearchCts.Token;

                await Task.Delay(600, token);
                if (token.IsCancellationRequested)
                    return;

                if (currentStep != 2)
                    return;

                var results = await _facade.SearchCountriesAsync(query, 10);
                if (token.IsCancellationRequested)
                    return;

                if (currentStep != 2)
                    return;

                CountryComboBox.ItemsSource = results;
                CountryComboBox.DisplayMemberPath = nameof(CountryOption.Name);
                CountryComboBox.IsDropDownOpen = results.Count > 0;

                await TryAutoCommitCountryAsync(results, query, token, allowShortAutoPick: false);

                if (currentStep == 2)
                    SyncHotelSearchLocationFromDestination(resetDirty: false);
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Country Search Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void CountryComboBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            e.Handled = true;

            try
            {
                if (currentStep != 2)
                    return;

                var query = CountryComboBox.Text?.Trim() ?? string.Empty;
                if (query.Length < 2)
                    return;

                var list = (CountryComboBox.ItemsSource as IEnumerable<CountryOption>)?.ToList() ?? new List<CountryOption>();
                if (list.Count == 0)
                    list = await _facade.SearchCountriesAsync(query, 10);

                await TryAutoCommitCountryAsync(list, query, CancellationToken.None, allowShortAutoPick: true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Country Search Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void CountryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading)
                return;

            if (CountryComboBox.SelectedItem is not CountryOption country)
                return;

            if (currentStep != 2)
                return;

            await ApplySelectedCountryAsync(country);
        }

        private async Task TryAutoCommitCountryAsync(
            List<CountryOption> results,
            string query,
            CancellationToken token,
            bool allowShortAutoPick)
        {
            if (currentStep != 2)
                return;

            if (results.Count == 0)
                return;

            CountryOption? pick = null;

            if (results.Count == 1)
            {
                // Avoid auto-committing on very short prefixes (e.g. "Fr") which can spam GeoDB and hit HTTP 429.
                if (!allowShortAutoPick && query.Length < 3)
                    return;

                pick = results[0];
            }
            else
            {
                pick = results.FirstOrDefault(c =>
                    string.Equals(c.Name, query, StringComparison.OrdinalIgnoreCase));
            }

            if (pick == null)
                return;

            if (token.IsCancellationRequested)
                return;

            if (currentStep != 2)
                return;

            await ApplySelectedCountryAsync(pick);
        }

        private async Task ApplySelectedCountryAsync(CountryOption country)
        {
            if (currentStep != 2)
                return;

            // Auto-commit / repeated events can call this multiple times for the same country; avoid wiping destination.
            if (_selectedCountry != null &&
                string.Equals(_selectedCountry.Code, country.Code, StringComparison.OrdinalIgnoreCase) &&
                _countryCities.Count > 0)
            {
                return;
            }

            try
            {
                _suppressCountrySearch = true;
                _selectedCountry = country;
                CountryComboBox.SelectedItem = country;
                CountryComboBox.Text = country.Name;
                CountryComboBox.IsDropDownOpen = false;
            }
            finally
            {
                _suppressCountrySearch = false;
            }

            try
            {
                // RapidAPI GeoDB free tiers often cap "limit" to 10.
                _countryCities = await _facade.GetCitiesByCountryCodeAsync(country.Code, 10);

                DestinationComboBox.ItemsSource = _countryCities;
                DestinationComboBox.IsDropDownOpen = false;

                if (currentStep == 2)
                {
                    DestinationComboBox.SelectedItem = null;
                    DestinationComboBox.Text = "";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Destination Load Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                SyncHotelSearchLocationFromDestination(resetDirty: true);
                UpdateHotelSearchUiState();

                UpdateLeftPreview();
                if (currentStep == 5)
                    UpdateReviewPanel();
            }
        }

        private void RecalculatePrice()
        {
            if (_isLoading)
                return;

            decimal basePrice = ParseDecimal(BasePriceTextBox.Text);
            decimal discount = ParseDecimal(DiscountTextBox.Text);
            decimal extra = ParseDecimal(ExtraChargesTextBox.Text);
            decimal vat = ParseDecimal(VatTextBox.Text);

            decimal compositePrice = GetCompositeServicesPrice();

            decimal finalPrice = CalculatePriceWithStrategy(
                basePrice,
                discount,
                vat,
                extra,
                compositePrice);

            EstimatedFinalPriceText.Text = $"€ {finalPrice:F2}";

            UpdateLeftPreview();

            if (currentStep == 5)
                UpdateReviewPanel();
        }

        private decimal GetCompositeServicesPrice()
        {
            decimal compositePrice = 0m;

            if (AirportTransferCheckBox.IsChecked == true)
                compositePrice += 30;

            if (TravelInsuranceCheckBox.IsChecked == true)
                compositePrice += 20;

            if (TourGuideCheckBox.IsChecked == true)
                compositePrice += 40;

            if (FreeCancellationCheckBox.IsChecked == true)
                compositePrice += 25;

            return compositePrice;
        }
    }
}