using System.Windows;
using TravelAgency.WPF.Messaging;
using TravelAgency.WPF.ViewModels;

namespace TravelAgency.WPF.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow() : this(App.Mediator)
        {
        }

        public LoginWindow(IMediator mediator)
        {
            InitializeComponent();
            DataContext = new LoginViewModel(this, mediator);
        }

        private void PasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm)
            {
                vm.Password = PasswordInput.Password;
            }
        }
    }
}