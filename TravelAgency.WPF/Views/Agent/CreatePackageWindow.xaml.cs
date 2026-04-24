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

namespace TravelAgency.WPF.Views
{
    public partial class CreatePackageWindow : Window
    {
        /// <summary>Package stay category when the wizard no longer asks for hotel vs apartment etc.</summary>
        private const string DefaultStayCategory = "Lodging";

        private readonly TripPackage? _editingTrip;

        private List<HotelSearchOption> _hotelResults = new();
        private string? _selectedHotelThumbnailUrl;
        private readonly TravelPackageFacade _facade = new TravelPackageFacade();
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

        public CreatePackageWindow(TripPackage? tripToEdit = null, int initialStep = 1)
        {
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
                    var request = BuildTripRequestFromForm();

                    var validator = new TripRequestValidator();
                    validator.ValidateAndThrow(request);

                    TripPackage trip;

                    if (_editingTrip != null)
                    {
                        trip = _facade.CreateAndUpdatePackage(request, _editingTrip.Id);

                        MessageBox.Show(
                            $"Package updated successfully!\n\nName: {trip.Name}\nPrice: {trip.Price:F2}",
                            "Success",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else
                    {
                        trip = _facade.CreateAndSavePackage(request);

                        MessageBox.Show(
                            $"Package created successfully!\n\nName: {trip.Name}\nPrice: {trip.Price:F2}",
                            "Success",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
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

        private void DatesChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateNumberOfDays();
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

        private void DestinationComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DestinationComboBox.SelectedItem is LocationOption selectedLocation)
            {
                DestinationComboBox.Text = selectedLocation.City;
                if (_selectedCountry == null)
                    CountryComboBox.Text = selectedLocation.Country;

                DestinationComboBox.IsDropDownOpen = false;

                SyncHotelSearchLocationFromDestination(resetDirty: true);
                UpdateHotelSearchUiState();

                UpdateLeftPreview();
                if (currentStep == 5)
                    UpdateReviewPanel();
            }
        }

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