using FluentValidation;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TravelAgency.Core.Models;
using TravelAgency.Core.Models.Locations;
using TravelAgency.Core.Models.TripPkg.Package;
using TravelAgency.Core.Patterns.Facades;
using TravelAgency.Core.Patterns.Strategy;
using TravelAgency.Core.Validators;

namespace TravelAgency.WPF.Views
{
    public partial class QuickCreatePackageWindow : Window
    {
        private readonly TravelPackageFacade _facade = new();
        private CancellationTokenSource? _destinationSearchCts;
        private bool _isLoaded;
        private bool _suppressDestinationSearch;

        public QuickCreatePackageWindow()
        {
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

        private async void DestinationTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (_suppressDestinationSearch)
                    return;

                var query = DestinationComboBox.Text?.Trim() ?? "";

                if (query.Length < 3)
                {
                    DestinationComboBox.IsDropDownOpen = false;
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

                UpdateComputedUi();
            }
            catch (TaskCanceledException)
            {
            }
            catch
            {
                // ignore autocomplete errors (do not block quick create)
            }
        }

        private void DestinationSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DestinationComboBox.SelectedItem is LocationOption loc)
            {
                try
                {
                    _suppressDestinationSearch = true;
                    DestinationComboBox.Text = loc.City;
                    CountryTextBox.Text = loc.Country;
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

                var editor = new CreatePackageWindow(created)
                {
                    Owner = this
                };
                editor.ShowDialog();

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

        private TripRequest BuildRequest()
        {
            string destination = DestinationComboBox.Text.Trim();
            string country = CountryTextBox.Text.Trim();

            var start = StartDatePicker.SelectedDate;
            var end = EndDatePicker.SelectedDate;

            if (!start.HasValue || !end.HasValue)
                throw new InvalidOperationException("Start date and end date are required.");

            if (end.Value < start.Value)
                throw new InvalidOperationException("End date must be after start date.");

            int seats = ParseInt(SeatsTextBox.Text, "Available seats");

            string templateType = GetComboBoxText(TemplateTypeComboBox);
            string tier = GetComboBoxText(TierComboBox);
            string transport = GetComboBoxText(TransportComboBox);

            double basePrice = ParseDouble(BasePriceTextBox.Text, "Base price");
            double discount = ParseDouble(DiscountTextBox.Text, "Discount");
            double vat = ParseDouble(VatTextBox.Text, "VAT");

            string packageName = $"{templateType} - {destination}".Trim();

            return new TripRequest
            {
                PackageName = packageName,
                TripType = tier,
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
            var country = CountryTextBox.Text?.Trim() ?? "";

            CreateAndEditButton.IsEnabled =
                destination.Length >= 2 &&
                country.Length >= 2 &&
                StartDatePicker.SelectedDate.HasValue &&
                EndDatePicker.SelectedDate.HasValue;

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
            }
            catch
            {
                EstimatedPriceText.Text = "€ -";
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

