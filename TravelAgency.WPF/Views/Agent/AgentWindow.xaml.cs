using System.Windows;
using TravelAgency.Core.Patterns.Adapters.SerpApi;
using TravelAgency.Core.Services;
using TravelAgency.Core.Models.Locations;
using TravelAgency.WPF.ViewModels.AgentVM;

namespace TravelAgency.WPF.Views
{
    public partial class AgentWindow : Window
    {
        public AgentWindow()
        {
            InitializeComponent();
            DataContext = new TravelAgency.WPF.ViewModels.AgentVM.AgentViewModel();
        }

        private void CreatePackage_Click(object sender, RoutedEventArgs e)
        {
            var window = new CreatePackageWindow();
            var result = window.ShowDialog();

            if (result == true && DataContext is AgentViewModel vm)
            {
                vm.ReloadCommand.Execute(null);
            }
        }

        private void EditPackage_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not AgentViewModel vm || vm.SelectedTrip == null)
                return;

            var window = new CreatePackageWindow(vm.SelectedTrip);
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

                var adapter = new SerpApiHotelAdapter();
                var service = new HotelSearchService(adapter);

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