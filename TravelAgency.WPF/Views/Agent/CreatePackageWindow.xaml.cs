using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TravelAgency.Core.Models;
using TravelAgency.Core.Models.Locations;
using TravelAgency.Core.Models.TripPkg.Package;
using TravelAgency.Core.Validators;
using FluentValidation;
using System.Windows.Input;
using TravelAgency.Core.Patterns.Facades;
namespace TravelAgency.WPF.Views
{
    public partial class CreatePackageWindow : Window
    {





        
       
        private readonly TripPackage? _editingTrip;
       
        private List<HotelSearchOption> _hotelResults = new();
        private readonly TravelPackageFacade _facade = new TravelPackageFacade();
        private List<LocationOption> _locationResults = new();
        private bool _isUpdatingDestinationSuggestions;
        private CancellationTokenSource? _locationSearchCts;
        private int currentStep = 1;

        public CreatePackageWindow(TripPackage? tripToEdit = null)
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
                Title = "Edit Package";
                LoadTripIntoForm();
            }

            UpdateWizardUI();
            UpdateLeftPreview();
        }
        private TripRequest BuildTripRequestFromForm()
        {
            string packageName = PackageNameTextBox.Text.Trim();
            string tripType = GetComboBoxText(TripTypeComboBox);
            string category = GetComboBoxText(CategoryComboBox);
            string shortDescription = ShortDescriptionTextBox.Text.Trim();
            string destination = DestinationComboBox.Text.Trim(); 
            
            string country = CountryTextBox.Text.Trim();
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
            string accommodationType = GetComboBoxText(AccommodationTypeComboBox);
            string accommodationName = AccommodationNameTextBox.Text.Trim();
            string mealPlan = GetComboBoxText(MealPlanComboBox);
            int availableSeats = ParseInt(AvailableSeatsTextBox.Text, "Available Seats");

            double basePrice = ParseDouble(BasePriceTextBox.Text, "Base Price");
            double discount = ParseDouble(DiscountTextBox.Text, "Discount");
            double vat = ParseDouble(VatTextBox.Text, "VAT");
            double extraCharges = ParseDouble(ExtraChargesTextBox.Text, "Extra Charges");

            decimal compositePrice = GetCompositeServicesPrice();

            double finalPrice = (double)_facade.CalculateFinalPrice(
                (decimal)basePrice,
                (decimal)discount,
                (decimal)vat,
                (decimal)extraCharges,
                compositePrice);

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

                    // FluentValidation pe TripRequest (input din wizard)
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

        private void UpdateWizardUI()
        {
            Step1Panel.Visibility = currentStep == 1 ? Visibility.Visible : Visibility.Collapsed;
            Step2Panel.Visibility = currentStep == 2 ? Visibility.Visible : Visibility.Collapsed;
            Step3Panel.Visibility = currentStep == 3 ? Visibility.Visible : Visibility.Collapsed;
            Step4Panel.Visibility = currentStep == 4 ? Visibility.Visible : Visibility.Collapsed;
            Step5Panel.Visibility = currentStep == 5 ? Visibility.Visible : Visibility.Collapsed;

            BackButton.IsEnabled = currentStep > 1;

            if (currentStep == 5)
                NextButton.Content = "Finish";
            else
                NextButton.Content = "Next";

            // Step 1 style
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

            // Step 2 style
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
            // Step 3 style
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
            // Step 4 style
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
            // Step 5 style
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
        }

        private void UpdateReviewPanel()
        {
            ReviewPackageNameText.Text = GetSafeText(PackageNameTextBox.Text);
            ReviewTripTypeText.Text = GetSafeText(GetComboBoxText(TripTypeComboBox));
            ReviewCategoryText.Text = GetSafeText(GetComboBoxText(CategoryComboBox));
            ReviewDescriptionText.Text = GetSafeText(ShortDescriptionTextBox.Text);

            ReviewDestinationText.Text = GetSafeText(DestinationComboBox.Text);
            ReviewCountryText.Text = GetSafeText(CountryTextBox.Text);
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
            ReviewAccommodationTypeText.Text = GetSafeText(GetComboBoxText(AccommodationTypeComboBox));
            ReviewAccommodationNameText.Text = GetSafeText(AccommodationNameTextBox.Text);
            ReviewMealPlanText.Text = GetSafeText(GetComboBoxText(MealPlanComboBox));
            ReviewAvailableSeatsText.Text = GetSafeText(AvailableSeatsTextBox.Text);

            ReviewServicesText.Text = BuildIncludedServicesText();

            double basePrice = ParseDoubleOrZero(BasePriceTextBox.Text);
            double discount = ParseDoubleOrZero(DiscountTextBox.Text);
            double vat = ParseDoubleOrZero(VatTextBox.Text);
            double extraCharges = ParseDoubleOrZero(ExtraChargesTextBox.Text);

            decimal compositePrice = GetCompositeServicesPrice();

            double finalPrice = (double)_facade.CalculateFinalPrice(
                (decimal)basePrice,
                (decimal)discount,
                (decimal)vat,
                (decimal)extraCharges,
                compositePrice);

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

        private static double ParseDoubleOrZero(string text)
        {
            if (double.TryParse(text, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double value))
                return value;

            if (double.TryParse(text, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.CurrentCulture, out value))
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
        private void UpdateLeftPreview()
        {
            string destination = GetSafeText(DestinationComboBox.Text);
            string packageName = GetSafeText(PackageNameTextBox.Text);
            string description = GetSafeText(ShortDescriptionTextBox.Text);

            string transport = GetSafeText(GetComboBoxText(TransportTypeComboBox));
            string accommodation = GetSafeText(GetComboBoxText(AccommodationTypeComboBox));

            decimal basePrice = ParseDecimal(BasePriceTextBox.Text);
            decimal discount = ParseDecimal(DiscountTextBox.Text);
            decimal vat = ParseDecimal(VatTextBox.Text);
            decimal extraCharges = ParseDecimal(ExtraChargesTextBox.Text);

            decimal compositePrice = GetCompositeServicesPrice();

            decimal finalPrice = _facade.CalculateFinalPrice(
                basePrice,
                discount,
                vat,
                extraCharges,
                compositePrice);

            PreviewDestinationCodeText.Text = destination == "-" ? "TRIP" : destination.ToUpperInvariant();
            PreviewPackageNameText.Text = packageName;
            PreviewDescriptionText.Text = description == "-" ? "Package preview" : description;

            PreviewTransportStayText.Text =
                $"{transport} + {accommodation} ({AccommodationNameTextBox.Text})";

            PreviewPriceText.Text = $"{finalPrice:F2}";
        }

        private bool _isLoading;

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
            CountryTextBox.Text = _editingTrip.Country;

            // Fallback for older/quick-created packages where destination/country weren't persisted,
            // but SeasonName was generated like "Paris, France trip".
            if (string.IsNullOrWhiteSpace(DestinationComboBox.Text) || string.IsNullOrWhiteSpace(CountryTextBox.Text))
            {
                var (dest, country) = InferDestinationCountryFromSeason(_editingTrip.Season?.Name);
                if (string.IsNullOrWhiteSpace(DestinationComboBox.Text))
                    DestinationComboBox.Text = dest;
                if (string.IsNullOrWhiteSpace(CountryTextBox.Text))
                    CountryTextBox.Text = country;
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

            SetComboBoxByText(AccommodationTypeComboBox, NormalizeStayName(_editingTrip.StayName));
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

        private static string NormalizeStayName(string stayName)
        {
            if (stayName.Contains("Hotel", StringComparison.OrdinalIgnoreCase))
                return "Hotel";

            if (stayName.Contains("Hostel", StringComparison.OrdinalIgnoreCase))
                return "Hostel";

            if (stayName.Contains("Resort", StringComparison.OrdinalIgnoreCase))
                return "Resort";

            if (stayName.Contains("Apartment", StringComparison.OrdinalIgnoreCase))
                return "Apartment";

            if (stayName.Contains("Villa", StringComparison.OrdinalIgnoreCase))
                return "Villa";

            return "Hotel";
        }

        private static (string destination, string country) InferDestinationCountryFromSeason(string? seasonName)
        {
            if (string.IsNullOrWhiteSpace(seasonName))
                return ("", "");

            // Example: "Paris, France trip"
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
                string destination = DestinationComboBox.Text?.Trim() ?? string.Empty;
                DateTime? checkIn = StartDatePicker.SelectedDate;
                DateTime? checkOut = EndDatePicker.SelectedDate;

                if (string.IsNullOrWhiteSpace(destination))
                {
                    MessageBox.Show("Select a destination first.", "Hotel Search", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!checkIn.HasValue || !checkOut.HasValue)
                {
                    MessageBox.Show("Select start and end dates first.", "Hotel Search", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (checkOut.Value <= checkIn.Value)
                {
                    MessageBox.Show("End Date must be after Start Date.", "Hotel Search", MessageBoxButton.OK, MessageBoxImage.Warning);
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

                HotelsListBox.ItemsSource = _hotelResults.Take(10).ToList();

                if (_hotelResults.Count == 0)
                {
                    MessageBox.Show("No hotels found for the selected destination and dates.",
                        "Hotel Search",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Hotel Search Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                SearchHotelsButton.IsEnabled = true;
                SearchHotelsButton.Content = "Search Hotels";
            }
        }
        private void HotelsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HotelsListBox.SelectedItem is not HotelSearchOption selectedHotel)
                return;

            AccommodationNameTextBox.Text = selectedHotel.Name ?? "";
            SetComboBoxByText(AccommodationTypeComboBox, "Hotel");

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
        private async void DestinationComboBox_KeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                string query = DestinationComboBox.Text?.Trim() ?? string.Empty;

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

                _locationResults = await _facade.SearchLocationsAsync(query, 10);

                if (token.IsCancellationRequested)
                    return;

                DestinationComboBox.ItemsSource = _locationResults;
                DestinationComboBox.IsDropDownOpen = _locationResults.Count > 0;
            }
            catch (TaskCanceledException)
            {
                // normal, user continues typing
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
                CountryTextBox.Text = selectedLocation.Country;

                DestinationComboBox.IsDropDownOpen = false;

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
            decimal vat = ParseDecimal(VatTextBox.Text);
            decimal extra = ParseDecimal(ExtraChargesTextBox.Text);

            // ===== Composite =====
            decimal compositePrice = 0m;

            if (AirportTransferCheckBox.IsChecked == true)
                compositePrice += 30;

            if (TravelInsuranceCheckBox.IsChecked == true)
                compositePrice += 20;

            if (TourGuideCheckBox.IsChecked == true)
                compositePrice += 40;

            if (FreeCancellationCheckBox.IsChecked == true)
                compositePrice += 25;

            // ===== calcul final =====
            decimal finalPrice = _facade.CalculateFinalPrice(
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