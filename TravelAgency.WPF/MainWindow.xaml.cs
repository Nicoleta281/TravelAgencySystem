

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

using TravelAgency.Core.Models.TripPkg.Package;
using TravelAgency.Core.Singletons;
using TravelAgency.Core.Services;
using TravelAgency.Core.Data.Repositories;

namespace TravelAgency.WPF
{
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<TripPackage> _trips = new();
        private readonly TripCreationService _tripCreationService = new();
        private readonly ITripPackageRepository _repo = new EfTripPackageRepository();

        public MainWindow()
        {
            InitializeComponent();

            TripsList.ItemsSource = _trips;

         

            // Load din DB prin repo
            foreach (var trip in _repo.GetAll())
                _trips.Add(trip);
        }

        private void CreateTrip_Click(object sender, RoutedEventArgs e)
        {
            var tripType = (TripTypeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Budget";
            var transportType = (TransportCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Train";

            var trip = _tripCreationService.CreateTrip(
                tripType: tripType,
                transportType: transportType,
                name: $"{tripType} {transportType} Trip",
                price: tripType == "Premium" ? 2000 : 1000
            );

            _repo.Add(trip);         
            _trips.Add(trip);         
            TripsList.SelectedItem = trip;

            ResultText.Text = $"{tripType} trip created with {trip.Transport.GetType().Name} + {trip.Stay.GetType().Name}.";
        }
        private void CreateTrip_Builder_Click(object sender, RoutedEventArgs e)
        {
            var season = new TravelAgency.Core.Models.TripPkg.Package.Season
            {
                Name = "Summer",
                StartDate = new DateTime(DateTime.Now.Year, 6, 1),
                EndDate = new DateTime(DateTime.Now.Year, 8, 31)
            };

            var trip = new TravelAgency.Core.Builders.TripPackageBuilder()
                .WithName("Builder Trip")
                .WithPrice(1200)
                .WithSeason(season)
                .WithTransport(new TravelAgency.Core.Models.TripPkg.Transport.Plane())
                .WithStay(new TravelAgency.Core.Models.TripPkg.Stay.Hotel { Name = "Hotel Roma", Address = "Center" })
                .WithExtra(new TravelAgency.Core.Models.TripPkg.Services.Guide())
                .WithExtra(new TravelAgency.Core.Models.TripPkg.Services.Breakfast())
                .WithDay(new TravelAgency.Core.Models.TripPkg.Package.TripDay())
                .Build();

            // UI list
            _trips.Add(trip);
            TripsList.SelectedItem = trip;

            // DB save (Supabase Postgres)
            _repo.Add(trip);

            var cfg = AppConfig.Instance;
            decimal basePrice = (decimal)trip.Price;
            decimal finalPrice = cfg.FinalPrice(basePrice);

            ResultText.Text =
                $"Builder: {trip.Name} | Base: {basePrice} {cfg.Currency} | Final: {finalPrice:F2} {cfg.Currency}\n" +
                $"Created with {trip.Transport.GetType().Name} + {trip.Stay.GetType().Name}";
        }
        private void CloneTrip_Click(object sender, RoutedEventArgs e)
        {
            if (TripsList.SelectedItem is not TripPackage selected)
            {
                ResultText.Text = "Selectează un trip din listă ca să îl clonezi.";
                return;
            }

            var clone = selected.DeepClone();
            clone.Name = selected.Name + " (Clone)";

            _trips.Add(clone);
            TripsList.SelectedItem = clone;

            _repo.Add(clone);

            ResultText.Text = $"Prototype: cloned '{selected.Name}' -> '{clone.Name}'";
        }


    }
}
