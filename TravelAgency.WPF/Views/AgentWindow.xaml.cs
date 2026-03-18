using System.Windows;
using TravelAgency.Core.Adapters.SerpApi;
using TravelAgency.Core.Services;

namespace TravelAgency.WPF.Views
{
    public partial class AgentWindow : Window
    {
        public AgentWindow()
        {
            InitializeComponent();
            DataContext = new TravelAgency.WPF.ViewModels.AgentViewModel();
        }

        private void CreatePackage_Click(object sender, RoutedEventArgs e)
        {
            var window = new CreatePackageWindow();
            var result = window.ShowDialog();

            if (result == true && DataContext is TravelAgency.WPF.ViewModels.AgentViewModel vm)
            {
                vm.ReloadCommand.Execute(null);
            }
        }

        private void EditPackage_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not TravelAgency.WPF.ViewModels.AgentViewModel vm || vm.SelectedTrip == null)
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

                MessageBox.Show($"Found {hotels.Count} hotels.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }
}