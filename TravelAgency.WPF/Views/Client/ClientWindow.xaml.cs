using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using TravelAgency.Core.Models.TripPkg.Package;
using TravelAgency.WPF.ViewModels.ClientVM;
using TravelAgency.WPF.Views.Common;

namespace TravelAgency.WPF.Views
{
    public partial class ClientWindow : Window
    {
        public ClientWindow()
        {
            InitializeComponent();
            DataContext = ActivatorUtilities.CreateInstance<ClientViewModel>(App.Services);
        }

        private void ViewDetails_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ClientViewModel vm &&
                sender is Button button &&
                button.Tag is TripPackage trip)
            {
                if (trip.AvailableSeats <= 0)
                {
                    MessageBox.Show(
                        "Nu mai sunt disponibile locuri la acest pachet.",
                        "Sold out",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                vm.SelectedPackage = trip;
            }
        }

        private void ClientThumb_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is string url && !string.IsNullOrWhiteSpace(url))
            {
                ImagePreviewWindow.ShowForUrl(this, "Image", url);
                e.Handled = true;
            }
        }

        private void ClientDetailsImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is ClientViewModel vm &&
                vm.SelectedPackage?.CoverImageUrl is string url &&
                !string.IsNullOrWhiteSpace(url))
            {
                ImagePreviewWindow.ShowForUrl(this, "Package cover", url);
                e.Handled = true;
            }
        }
    }
}