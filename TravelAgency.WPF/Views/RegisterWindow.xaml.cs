using System.Windows;
using TravelAgency.WPF.ViewModels;

namespace TravelAgency.WPF.Views
{
    public partial class RegisterWindow : Window
    {
        public RegisterWindow()
        {
            InitializeComponent();
            DataContext = new RegisterViewModel(this);
        }

        private void PasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is RegisterViewModel vm)
            {
                vm.Password = PasswordInput.Password;
            }
        }

        private void ConfirmPasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is RegisterViewModel vm)
            {
                vm.ConfirmPassword = ConfirmPasswordInput.Password;
            }
        }
    }
}