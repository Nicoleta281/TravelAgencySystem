using System.Windows;

namespace TravelAgency.WPF.Views
{
    public partial class AgentWindow : Window
    {
        public AgentWindow()
        {
            InitializeComponent();
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
    }
}