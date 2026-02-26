using System.Windows;
using TravelAgency.WPF.ViewModels;

namespace TravelAgency.WPF.Views
{
    public partial class AgentWindow : Window
    {
        public AgentWindow()
        {
            InitializeComponent();
            DataContext = new AgentViewModel();
        }
    }
}