using System.Windows;
using TravelAgency.Core.Patterns.Adapters.SerpApi;
using TravelAgency.Core.Services;
using TravelAgency.Core.Models.Locations;
using Microsoft.Extensions.DependencyInjection;
using TravelAgency.Core.Interfaces;
using TravelAgency.WPF.ViewModels.AgentVM;
using TravelAgency.WPF.Services.Navigation;

namespace TravelAgency.WPF.Views.Agent
{
    public partial class AgentWindow : Window
    {
        private readonly INavigationService _navigation =
            App.Services.GetRequiredService<INavigationService>();

        public AgentWindow()
        {
            InitializeComponent();
            DataContext = ActivatorUtilities.CreateInstance<AgentViewModel>(App.Services);
        }

        private void CreatePackage_Click(object sender, RoutedEventArgs e)
        {
            var window = _navigation.CreateNewPackageWindow();
            var result = window.ShowDialog();

            if (result == true && DataContext is AgentViewModel vm)
            {
                vm.ReloadCommand.Execute(null);
            }
        }

        private void QuickCreatePackage_Click(object sender, RoutedEventArgs e)
        {
            var window = _navigation.CreateQuickCreatePackageWindow();
            window.Owner = this;

            var result = window.ShowDialog();

            if (result == true && DataContext is AgentViewModel vm)
            {
                var createdId = window.CreatedTrip?.Id ?? 0;

                vm.ReloadCommand.Execute(null);
                vm.SelectTripById(createdId);
            }
        }

        private void EditPackage_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not AgentViewModel vm || vm.SelectedTrip == null)
                return;

            var window = _navigation.CreateEditPackageWindow(vm.SelectedTrip);
            var result = window.ShowDialog();

            if (result == true)
            {
                vm.ReloadCommand.Execute(null);
            }
        }

        private async void TestHotelSearch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var key = Environment.GetEnvironmentVariable("SERPAPI_API_KEY")?.Trim();
                if (string.IsNullOrWhiteSpace(key))
                    key = Environment.GetEnvironmentVariable(
                        "SERPAPI_API_KEY",
                        EnvironmentVariableTarget.User)?.Trim();

                if (string.IsNullOrWhiteSpace(key))
                    key = Environment.GetEnvironmentVariable(
                        "SERPAPI_API_KEY",
                        EnvironmentVariableTarget.Machine)?.Trim();

                if (string.IsNullOrWhiteSpace(key))
                {
                    MessageBox.Show("SERPAPI_API_KEY nu exista sau este goala.");
                    return;
                }

                MessageBox.Show($"Cheia exista. Lungime: {key.Length}");

                var provider = App.Services.GetRequiredService<IHotelSearchProvider>();
                var service = new HotelSearchService(provider);

                var hotels = await service.SearchHotelsAsync(
                    "Paris",
                    new DateTime(2026, 7, 10),
                    new DateTime(2026, 7, 15),
                    2
                );

                if (hotels.Count == 0)
                {
                    MessageBox.Show("No hotels returned by API.", "Hotel Search",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var previewCount = Math.Min(10, hotels.Count);

                string FormatHotelLine(int index, HotelSearchOption h)
                {
                    var price =
                        h.PricePerNight.HasValue ? $"{h.PricePerNight.Value:F2} /night" : "N/A";
                    var classText = h.HotelClass.HasValue ? $" | Class: {h.HotelClass.Value}" : "";
                    return $"{index}. {h.Name} | {price}{classText}";
                }

                var preview = string.Join(Environment.NewLine,
                    hotels.Take(previewCount).Select((h, i) => FormatHotelLine(i + 1, h)));

                MessageBox.Show(
                    $"Found {hotels.Count} hotels. Showing top {previewCount}:{Environment.NewLine}{preview}",
                    "Hotel Search",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }
}