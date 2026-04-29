using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using TravelAgency.WPF.ViewModels;

namespace TravelAgency.WPF.Views
{
    public partial class ForgotPasswordWindow : Window
    {
        public ForgotPasswordWindow()
        {
            InitializeComponent();
            DataContext = ActivatorUtilities.CreateInstance<ForgotPasswordViewModel>(App.Services, this);
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

