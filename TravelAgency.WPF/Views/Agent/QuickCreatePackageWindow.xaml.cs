using FluentValidation;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TravelAgency.Core.Models;
using TravelAgency.Core.Models.Locations;
using TravelAgency.Core.Models.TripPkg.Package;
using TravelAgency.Core.Patterns.Facades;
using TravelAgency.Core.Patterns.Strategy;
using TravelAgency.Core.Validators;

namespace TravelAgency.WPF.Views.Agent
{
    public partial class QuickCreatePackageWindow : Window
    {
        public TripPackage? CreatedTrip { get; private set; }

        private readonly TravelPackageFacade _facade;
        private CancellationTokenSource? _destinationSearchCts;
        private CancellationTokenSource? _countrySearchCts;
        private bool _isLoaded;
        private bool _suppressDestinationSearch;
        private bool _suppressCountrySearch;
        private LocationOption? _selectedLocation;
        private CountryOption? _selectedCountry;
        private LocationOption[] _countryCityCache = Array.Empty<LocationOption>();
        private bool _destinationSearchErrorShown;
        private bool _countrySearchErrorShown;

        public QuickCreatePackageWindow(TravelPackageFacade facade)
        {
            _facade = facade ?? throw new ArgumentNullException(nameof(facade));
            InitializeComponent();
            StartDatePicker.SelectedDate = DateTime.Today.AddDays(30);
            EndDatePicker.SelectedDate = DateTime.Today.AddDays(37);

            Loaded += (_, __) =>
            {
                _isLoaded = true;
                UpdateComputedUi();
            };
        }

        private void AnyInputChanged(object sender, EventArgs e)
        {
            UpdateComputedUi();
        }

        private static string GetDestinationCityQuery(string raw)
        {
            var text = raw?.Trim() ?? string.Empty;
            if (text.Length == 0)
                return string.Empty;

            // Users sometimes paste "City, Country" into the editable ComboBox.
            var commaIdx = text.IndexOf(',', StringComparison.Ordinal);
            if (commaIdx > 0)
                return text[..commaIdx].Trim();

            return text;
        }

        private void TrySyncSelectedLocationFromText(string rawDestinationText, IEnumerable<LocationOption>? preferredCandidates = null)
        {
            var cityQuery = GetDestinationCityQuery(rawDestinationText);
            if (cityQuery.Length < 2)
            {
                _selectedLocation = null;
                return;
            }

            IEnumerable<LocationOption> candidates =
                preferredCandidates
                ?? (DestinationComboBox.ItemsSource as IEnumerable<LocationOption>)
                ?? _countryCityCache;

            var matches = candidates
                .Where(x => string.Equals(x.City, cityQuery, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (_selectedCountry != null)
            {
                matches = matches
                    .Where(x => string.Equals(x.CountryCode, _selectedCountry.Code, StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(x.Country, _selectedCountry.Name, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (matches.Count == 1)
            {
                _selectedLocation = matches[0];
                return;
            }

            // If we can't uniquely resolve, keep prior selection only if it still matches the text.
            if (_selectedLocation != null &&
                string.Equals(_selectedLocation.City, cityQuery, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _selectedLocation = null;
        }

        private async void DestinationTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (_suppressDestinationSearch)
                    return;

                var raw = DestinationComboBox.Text?.Trim() ?? "";
                var query = GetDestinationCityQuery(raw);

                // If user edits the destination manually, the selected location is no longer reliable.
                if (_selectedLocation != null &&
                    !string.Equals(query, _selectedLocation.City, StringComparison.OrdinalIgnoreCase))
                {
                    _selectedLocation = null;
                    DestinationComboBox.SelectedItem = null;
                    if (_selectedCountry == null)
                    {
                        _suppressCountrySearch = true;
                        CountryComboBox.SelectedItem = null;
                        CountryComboBox.Text = "";
                        _suppressCountrySearch = false;
                    }
                }

                // If a country is selected, we filter within that country's cities first.
                if (_selectedCountry != null && _countryCityCache.Length > 0)
                {
                    if (query.Length < 1)
                    {
                        DestinationComboBox.ItemsSource = _countryCityCache;
                        DestinationComboBox.IsDropDownOpen = _countryCityCache.Length > 0;
                        TrySyncSelectedLocationFromText(raw, _countryCityCache);
                        UpdateComputedUi();
                        return;
                    }

                    var filtered = _countryCityCache
                        .Where(x => (x.City ?? string.Empty).StartsWith(query, StringComparison.OrdinalIgnoreCase))
                        .Take(20)
                        .ToList();

                    DestinationComboBox.ItemsSource = filtered;
                    DestinationComboBox.IsDropDownOpen = filtered.Count > 0;
                    TrySyncSelectedLocationFromText(raw, filtered);
                    UpdateComputedUi();
                    return;
                }

                if (query.Length < 3)
                {
                    DestinationComboBox.IsDropDownOpen = false;
                    TrySyncSelectedLocationFromText(raw);
                    UpdateComputedUi();
                    return;
                }

                _destinationSearchCts?.Cancel();
                _destinationSearchCts = new CancellationTokenSource();
                var token = _destinationSearchCts.Token;

                await Task.Delay(450, token);
                if (token.IsCancellationRequested)
                    return;

                var results = await _facade.SearchLocationsAsync(query, 10);
                if (token.IsCancellationRequested)
                    return;

                DestinationComboBox.ItemsSource = results;
                DestinationComboBox.IsDropDownOpen = results.Count > 0;

                TrySyncSelectedLocationFromText(raw, results);
                UpdateComputedUi();
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
                DestinationComboBox.IsDropDownOpen = false;

                if (!_destinationSearchErrorShown)
                {
                    _destinationSearchErrorShown = true;
                    MessageBox.Show(
                        ex.Message,
                        "Location Search Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
        }

        private void DestinationSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DestinationComboBox.SelectedItem is LocationOption loc)
            {
                try
                {
                    _suppressDestinationSearch = true;
                    _selectedLocation = loc;
                    DestinationComboBox.Text = loc.City;

                    // If user picked a city, ensure country is in sync.
                    if (_selectedCountry == null ||
                        !string.Equals(_selectedCountry.Code, loc.CountryCode, StringComparison.OrdinalIgnoreCase))
                    {
                        _suppressCountrySearch = true;
                        CountryComboBox.SelectedItem = null;
                        CountryComboBox.Text = loc.Country;
                        _suppressCountrySearch = false;
                        _selectedCountry = null;
                        _countryCityCache = Array.Empty<LocationOption>();
                    }

                    DestinationComboBox.IsDropDownOpen = false;
                }
                finally
                {
                    _suppressDestinationSearch = false;
                }

                UpdateComputedUi();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void CreateAndEdit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var request = BuildRequest();

                var validator = new TripRequestValidator();
                validator.ValidateAndThrow(request);

                var created = _facade.CreateAndSavePackage(request);
                CreatedTrip = created;

                DialogResult = true;
                Close();
            }
            catch (ValidationException ex)
            {
                MessageBox.Show(
                    ex.Errors.FirstOrDefault()?.ErrorMessage ?? ex.Message,
                    "Validation error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Quick create failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private async void CountryTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (_suppressCountrySearch)
                    return;

                var query = CountryComboBox.Text?.Trim() ?? "";

                if (_selectedCountry != null &&
                    !string.Equals(query, _selectedCountry.Name, StringComparison.OrdinalIgnoreCase))
                {
                    _selectedCountry = null;
                    _countryCityCache = Array.Empty<LocationOption>();
                    DestinationComboBox.ItemsSource = null;
                    DestinationComboBox.SelectedItem = null;
                    _selectedLocation = null;
                }

                if (query.Length < 2)
                {
                    CountryComboBox.IsDropDownOpen = false;
                    UpdateComputedUi();
                    return;
                }

                _countrySearchCts?.Cancel();
                _countrySearchCts = new CancellationTokenSource();
                var token = _countrySearchCts.Token;

                await Task.Delay(600, token);
                if (token.IsCancellationRequested)
                    return;

                var results = await _facade.SearchCountriesAsync(query, 10);
                if (token.IsCancellationRequested)
                    return;

                CountryComboBox.ItemsSource = results;
                CountryComboBox.DisplayMemberPath = nameof(CountryOption.Name);
                CountryComboBox.IsDropDownOpen = results.Count > 0;

                // If the user types the full country name (or there's only one match), don't require a mouse click.
                await TryAutoCommitCountryAsync(results, query, token, allowShortAutoPick: false);

                UpdateComputedUi();
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
                CountryComboBox.IsDropDownOpen = false;

                if (!_countrySearchErrorShown)
                {
                    _countrySearchErrorShown = true;
                    MessageBox.Show(
                        ex.Message,
                        "Country Search Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
        }

        private async void CountryComboBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            e.Handled = true;

            try
            {
                var query = CountryComboBox.Text?.Trim() ?? "";
                if (query.Length < 2)
                    return;

                var list = (CountryComboBox.ItemsSource as IEnumerable<CountryOption>)?.ToList() ?? new List<CountryOption>();

                if (list.Count == 0)
                    list = await _facade.SearchCountriesAsync(query, 10);

                await TryAutoCommitCountryAsync(list, query, CancellationToken.None, allowShortAutoPick: true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Country Search Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private async void CountrySelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CountryComboBox.SelectedItem is not CountryOption country)
                return;

            await ApplySelectedCountryAsync(country);
        }

        private async Task TryAutoCommitCountryAsync(
            List<CountryOption> results,
            string query,
            CancellationToken token,
            bool allowShortAutoPick)
        {
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

            await ApplySelectedCountryAsync(pick);
        }

        private async Task ApplySelectedCountryAsync(CountryOption country)
        {
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
                var savedDestination = (DestinationComboBox.Text ?? string.Empty).Trim();

                var cities = await _facade.GetCitiesByCountryCodeAsync(country.Code, 10);
                _countryCityCache = cities.ToArray();

                DestinationComboBox.ItemsSource = _countryCityCache;
                DestinationComboBox.IsDropDownOpen = _countryCityCache.Length > 0;

                _selectedLocation = null;
                DestinationComboBox.SelectedItem = null;

                if (string.IsNullOrWhiteSpace(savedDestination))
                {
                    DestinationComboBox.Text = string.Empty;
                }
                else
                {
                    var match = _countryCityCache.FirstOrDefault(c =>
                        string.Equals(c.City, savedDestination, StringComparison.OrdinalIgnoreCase));

                    if (match != null)
                    {
                        DestinationComboBox.SelectedItem = match;
                        DestinationComboBox.Text = match.City;
                        _selectedLocation = match;
                    }
                    else
                    {
                        // city text may not be in the first N API results – keep the free text instead of clearing it
                        DestinationComboBox.Text = savedDestination;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Destination Load Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            finally
            {
                UpdateComputedUi();
            }
        }

        private TripRequest BuildRequest()
        {
            string destination = GetDestinationCityQuery(DestinationComboBox.Text);
            string country = CountryComboBox.Text.Trim();

            var start = StartDatePicker.SelectedDate;
            var end = EndDatePicker.SelectedDate;

            if (!start.HasValue || !end.HasValue)
                throw new InvalidOperationException("Start date and end date are required.");

            if (end.Value < start.Value)
                throw new InvalidOperationException("End date must be after start date.");

            int seats = ParseInt(SeatsTextBox.Text, "Available seats");

            string templateType = GetComboBoxText(TemplateTypeComboBox); // e.g. "City Break"
            string tier = GetComboBoxText(TierComboBox);                 // e.g. "Premium"
            string transport = GetComboBoxText(TransportComboBox);

            double basePrice = ParseDouble(BasePriceTextBox.Text, "Base price");
            double discount = ParseDouble(DiscountTextBox.Text, "Discount");
            double vat = ParseDouble(VatTextBox.Text, "VAT");

            string packageName = $"{templateType} - {destination}".Trim();

            return new TripRequest
            {
                PackageName = packageName,
                // Align with full editor: TripType = template (City Break / Beach Holiday),
                // Category = Tier (Budget / Premium).
                TripType = templateType,
                Category = tier,
                ShortDescription = $"Draft created from template: {templateType}",
                Destination = destination,
                Country = country,
                StartDate = start,
                EndDate = end,
                NumberOfDays = (end.Value.Date - start.Value.Date).Days + 1,
                TransportType = transport,
                DepartureCity = "N/A",
                AccommodationType = "Hotel",
                AccommodationName = AccommodationNameTextBox.Text.Trim(),
                MealPlan = "Breakfast",
                AvailableSeats = seats,
                BasePrice = basePrice,
                DiscountPercent = discount,
                VatPercent = vat,
                ExtraCharges = 0,
                FinalPrice = basePrice
            };
        }

        private void UpdateComputedUi()
        {
            if (!_isLoaded)
                return;

            if (CreateAndEditButton == null || EstimatedPriceText == null)
                return;

            // Enable button only when essentials exist.
            var destination = DestinationComboBox.Text?.Trim() ?? "";
            var country = CountryComboBox.Text?.Trim() ?? "";

            TrySyncSelectedLocationFromText(destination);

            var hasDates =
                StartDatePicker.SelectedDate.HasValue &&
                EndDatePicker.SelectedDate.HasValue;

            CreateAndEditButton.IsEnabled =
                _selectedLocation != null &&
                destination.Length >= 2 &&
                country.Length >= 2 &&
                hasDates;

            if (DestinationHintText != null)
                DestinationHintText.Visibility = _selectedLocation == null ? Visibility.Visible : Visibility.Collapsed;

            // Estimate price using same strategy as main wizard.
            try
            {
                var basePrice = (decimal)ParseDouble(BasePriceTextBox.Text, "Base price");
                var discount = (decimal)ParseDouble(DiscountTextBox.Text, "Discount");
                var vat = (decimal)ParseDouble(VatTextBox.Text, "VAT");

                IPricingStrategy strategy =
                    (discount > 0 || vat > 0)
                        ? new FullPricingStrategy(discount, vat)
                        : new StandardPricingStrategy();

                var context = new PricingContext(strategy);
                var final = context.CalculateFinalPrice(basePrice, 0);
                EstimatedPriceText.Text = $"€ {final:0.00}";

                var template = GetComboBoxText(TemplateTypeComboBox);
                var transport = GetComboBoxText(TransportComboBox);
                var start = StartDatePicker.SelectedDate;
                var end = EndDatePicker.SelectedDate;

                if (CreateAndEditButton.IsEnabled && start.HasValue && end.HasValue)
                {
                    SummaryText.Text =
                        $"{template} - {destination} • " +
                        $"{start.Value:dd.MM.yyyy} – {end.Value:dd.MM.yyyy} • " +
                        transport;
                }
                else
                {
                    SummaryText.Text = string.Empty;
                }
            }
            catch
            {
                EstimatedPriceText.Text = "€ -";
                SummaryText.Text = string.Empty;
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
    }
}

