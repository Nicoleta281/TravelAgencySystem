using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using FluentValidation;
using TravelAgency.Core.Data.Repositories;
using TravelAgency.Core.Models.Users.Access;
using TravelAgency.Core.Services;
using TravelAgency.Core.Validators;
using TravelAgency.WPF.Commands;
using TravelAgency.WPF.Views;

namespace TravelAgency.WPF.ViewModels
{
    public class RegisterViewModel : ViewModelBase
    {
        private readonly Window _registerWindow;
        private readonly RegistrationService _registrationService;

        private string _username = "";
        private string _password = "";
        private string _confirmPassword = "";
        private string _errorMessage = "";

        public string Username
        {
            get => _username;
            set => Set(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => Set(ref _password, value);
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => Set(ref _confirmPassword, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => Set(ref _errorMessage, value);
        }

        public ICommand RegisterCommand { get; }
        public ICommand BackToLoginCommand { get; }

        public RegisterViewModel(Window registerWindow)
        {
            _registerWindow = registerWindow;
            _registrationService = new RegistrationService(new EfUserRepository());

            RegisterCommand = new RelayCommand(Register);
            BackToLoginCommand = new RelayCommand(BackToLogin);
        }

        private void Register()
        {
            ErrorMessage = "";

            var request = new RegisterRequest
            {
                Username = Username?.Trim() ?? "",
                Password = Password ?? "",
                ConfirmPassword = ConfirmPassword ?? ""
            };

            try
            {
                var validator = new RegisterRequestValidator();
                validator.ValidateAndThrow(request);

                _registrationService.RegisterClient(request);

                MessageBox.Show("Account created successfully. You can now log in.",
                                "Register",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);

                var loginWindow = new LoginWindow();
                loginWindow.Show();
                _registerWindow.Close();
            }
            catch (ValidationException ex)
            {
                ErrorMessage = ex.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid registration input.";
            }
            catch (InvalidOperationException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (Exception)
            {
                ErrorMessage = "Registration failed due to an unexpected error.";
            }
        }

        private void BackToLogin()
        {
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            _registerWindow.Close();
        }
    }
}