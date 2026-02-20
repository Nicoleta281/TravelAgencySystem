using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using TravelAgency.Core.Factories;
using TravelAgency.Core.Interfaces;
using TravelAgency.Core.Models;
using TravelAgency.Core.Models.TripPkg.Package;

namespace TravelAgency.WPF
{
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<TripPackage> _trips = new();

        public MainWindow()
        {
            InitializeComponent();
            TripsList.ItemsSource = _trips;
        }


        private void CreateTrip_Click(object sender, RoutedEventArgs e)
        {
            var tripType = (TripTypeCombo.SelectedItem as ComboBoxItem)?.Content.ToString();
            var transportType = (TransportCombo.SelectedItem as ComboBoxItem)?.Content.ToString();

            // ===== Abstract Factory =====
            ITripComponentFactory componentFactory =
                tripType == "Premium" ? new PremiumTripFactory() : new BudgetTripFactory();

            // ===== Factory Method =====
            TransportFactory transportFactory = transportType switch
            {
                "Plane" => new PlaneFactory(),
                "Bus" => new BusFactory(),
                _ => new TrainFactory()
            };

            // Factory Method: creezi transportul
            ITransport transport = transportFactory.CreateTransport();

            // Abstract Factory: restul componentelor
            var assembly = new TripAssembly(
                transport,
                componentFactory.CreateStay(),
                componentFactory.CreateExtraService()
            );

            ResultText.Text = $"{tripType} trip created with {transport.GetType().Name}.";
        }

        private void CreateBudgetTrip_Click(object sender, RoutedEventArgs e)
        {
            ITripComponentFactory factory = new BudgetTripFactory();

            var trip = new TripAssembly(
                factory.CreateTransport(),
                factory.CreateStay(),
                factory.CreateExtraService()
            );

            ResultText.Text = "Budget Trip created: Bus + Hostel + Breakfast";
        }

        private void CreatePremiumTrip_Click(object sender, RoutedEventArgs e)
        {
            ITripComponentFactory factory = new PremiumTripFactory();

            var trip = new TripAssembly(
                factory.CreateTransport(),
                factory.CreateStay(),
                factory.CreateExtraService()
            );

            ResultText.Text = "Premium Trip created: Plane + Hotel + Guide";
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
                .WithStay(new TravelAgency.Core.Models.TripPkg.Stay.Hotel
                {
                    Name = "Hotel Roma",
                    Address = "Center"
                })
                .WithExtra(new TravelAgency.Core.Models.TripPkg.Services.Guide())
                .WithExtra(new TravelAgency.Core.Models.TripPkg.Services.Breakfast())
                .WithDay(new TravelAgency.Core.Models.TripPkg.Package.TripDay())
                .Build();
            _trips.Add(trip);
            TripsList.SelectedItem = trip;


            ResultText.Text =
                $"Builder: {trip.Name} created with {trip.Transport.GetType().Name} + {trip.Stay.GetType().Name}";

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

            ResultText.Text = $"Prototype: cloned '{selected.Name}' -> '{clone.Name}'";
        }

    }
}
