using System.Windows;

using TravelAgency.WPF.ViewModels.AdminVM;

namespace TravelAgency.WPF.Views
{
    public partial class AdminWindow : Window
    {
        public AdminWindow()
        {
            InitializeComponent();
            DataContext = new AdminViewModel();
        }
    }
}