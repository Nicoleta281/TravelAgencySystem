using System.Windows;
using System.Windows.Controls;
using TravelAgency.Core.Factories;
using TravelAgency.Core.Factories;
using TravelAgency.Core.Models;
using TravelAgency.Core.Models.TripPkg;
using TravelAgency.Core.Models.TripPkg.Package;


namespace TravelAgency.WPF
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void CreateTrip_Click(object sender, RoutedEventArgs e)
        {
            var tripType = (TripTypeCombo.SelectedItem as ComboBoxItem)?.Content.ToString();
            var transportType = (TransportCombo.SelectedItem as ComboBoxItem)?.Content.ToString();

            // Abstract Factory
            ITripComponentFactory componentFactory =
                tripType == "Premium" ? new PremiumTripFactory() : new BudgetTripFactory();

            // Factory Method
            TransportFactory transportFactory = transportType switch
            {
                "Plane" => new PlaneFactory(),
                "Bus" => new BusFactory(),
                _ => new TrainFactory()
            };

            var tripPackage = new TripPackage(transportFactory);

            var assembly = new TripAssembly(
                componentFactory.CreateTransport(),
                componentFactory.CreateStay(),
                componentFactory.CreateExtraService()
            );

            ResultText.Text =
                $"{tripType} trip created with {transportType} transport.";
        }

        private void CreateBudgetTrip_Click(object sender, RoutedEventArgs e)
        {
            ITripComponentFactory factory = new BudgetTripFactory();

            TripAssembly trip = new TripAssembly(
                factory.CreateTransport(),
                factory.CreateStay(),
                factory.CreateExtraService()
            );

            ResultText.Text = "Budget Trip created: Bus + Hostel + Breakfast";
        }

        private void CreatePremiumTrip_Click(object sender, RoutedEventArgs e)
        {
            ITripComponentFactory factory = new PremiumTripFactory();

            TripAssembly trip = new TripAssembly(
                factory.CreateTransport(),
                factory.CreateStay(),
                factory.CreateExtraService()
            );

            ResultText.Text = "Premium Trip created: Plane + Hotel + Guide";
        }

    }
}
