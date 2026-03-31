using System.Windows;
using System.Windows.Controls;
using TravelAgency.Core.Models.TripPkg.Package;
using TravelAgency.WPF.ViewModels.Client;

namespace TravelAgency.WPF.Views
{
    public partial class ClientWindow : Window
    {
        public ClientWindow()
        {
            InitializeComponent();
            DataContext = new ClientWindowViewModel();
        }

        private void ViewDetails_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ClientWindowViewModel vm &&
                sender is Button button &&
                button.Tag is TripPackage trip)
            {
                vm.SelectedPackage = trip;
            }
        }
    }
}