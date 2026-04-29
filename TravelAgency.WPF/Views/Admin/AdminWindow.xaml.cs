using System.Windows;
using Microsoft.Extensions.DependencyInjection;

using TravelAgency.WPF.ViewModels.AdminVM;

namespace TravelAgency.WPF.Views
{
    public partial class AdminWindow : Window
    {
        public AdminWindow()
        {
            InitializeComponent();
            DataContext = ActivatorUtilities.CreateInstance<AdminViewModel>(App.Services);
        }
    }
}