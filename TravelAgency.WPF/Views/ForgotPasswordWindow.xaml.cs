using System.Windows;
using TravelAgency.WPF.ViewModels;

namespace TravelAgency.WPF.Views
{
    public partial class ForgotPasswordWindow : Window
    {
        public ForgotPasswordWindow()
        {
            InitializeComponent();
            DataContext = new ForgotPasswordViewModel(this);
            Closed += (_, _) =>
            {
                if (DataContext is ForgotPasswordViewModel vm)
                    vm.Dispose();
            };
        }

        private void NewPasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is ForgotPasswordViewModel vm)
            {
                vm.NewPassword = NewPasswordInput.Password;
            }
        }

        private void ConfirmPasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is ForgotPasswordViewModel vm)
            {
                vm.ConfirmPassword = ConfirmPasswordInput.Password;
            }
        }
    }
}

