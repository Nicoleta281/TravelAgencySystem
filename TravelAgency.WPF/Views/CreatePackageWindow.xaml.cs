using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using TravelAgency.Core.Models;
using TravelAgency.Core.Services;
using TravelAgency.Core.Data.Repositories;

namespace TravelAgency.WPF.Views
{
    public partial class CreatePackageWindow : Window
    {
        private readonly TripCreationService _tripCreationService = new();
        private readonly ITripPackageRepository _repo = new EfTripPackageRepository();

        private int currentStep = 1;

        public CreatePackageWindow()
        {
            InitializeComponent();
            UpdateWizardUI();
            UpdateLeftPreview();
        }
        private TripRequest BuildTripRequestFromForm()
        {
            string packageName = PackageNameTextBox.Text.Trim();
            string tripType = GetComboBoxText(TripTypeComboBox);
            string category = GetComboBoxText(CategoryComboBox);
            string shortDescription = ShortDescriptionTextBox.Text.Trim();

            string destination = DestinationTextBox.Text.Trim();
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

            int numberOfDays;
            if (string.IsNullOrWhiteSpace(NumberOfDaysTextBox.Text))
                numberOfDays = (endDate.Value - startDate.Value).Days;
            else
                numberOfDays = ParseInt(NumberOfDaysTextBox.Text, "Number of Days");

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

            double afterDiscount = basePrice - (basePrice * discount / 100.0);
            double afterVat = afterDiscount + (afterDiscount * vat / 100.0);
            double finalPrice = afterVat + extraCharges;

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
                    var trip = _tripCreationService.CreateTrip(request);

                    _repo.Add(trip);

                    MessageBox.Show(
                        $"Package created successfully!\n\nName: {trip.Name}\nPrice: {trip.Price:F2}",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

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

            ReviewDestinationText.Text = GetSafeText(DestinationTextBox.Text);
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
                    (EndDatePicker.SelectedDate.Value - StartDatePicker.SelectedDate.Value).Days.ToString();
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

            double afterDiscount = basePrice - (basePrice * discount / 100.0);
            double afterVat = afterDiscount + (afterDiscount * vat / 100.0);
            double finalPrice = afterVat + extraCharges;

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

        private void UpdateLeftPreview()
        {
            string destination = GetSafeText(DestinationTextBox.Text);
            string packageName = GetSafeText(PackageNameTextBox.Text);
            string description = GetSafeText(ShortDescriptionTextBox.Text);

            string transport = GetSafeText(GetComboBoxText(TransportTypeComboBox));
            string accommodation = GetSafeText(GetComboBoxText(AccommodationTypeComboBox));

            double basePrice = ParseDoubleOrZero(BasePriceTextBox.Text);
            double discount = ParseDoubleOrZero(DiscountTextBox.Text);
            double vat = ParseDoubleOrZero(VatTextBox.Text);
            double extraCharges = ParseDoubleOrZero(ExtraChargesTextBox.Text);

            double afterDiscount = basePrice - (basePrice * discount / 100.0);
            double afterVat = afterDiscount + (afterDiscount * vat / 100.0);
            double finalPrice = afterVat + extraCharges;

            PreviewDestinationCodeText.Text = destination == "-" ? "TRIP" : destination.ToUpperInvariant();
            PreviewPackageNameText.Text = packageName;
            PreviewDescriptionText.Text = description == "-" ? "Package preview" : description;
            PreviewTransportStayText.Text = $"{transport} + {accommodation}";
            PreviewPriceText.Text = $"{finalPrice:F2}";
        }
    }
}