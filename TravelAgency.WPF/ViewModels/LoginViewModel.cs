using FluentValidation;
using System.Windows;
using System.Windows.Input;
using TravelAgency.Core.Data.Repositories;
using TravelAgency.Core.Models.Users;
using TravelAgency.Core.Models.Users.Access;
using TravelAgency.Core.Services;
using TravelAgency.Core.Validators;
using TravelAgency.WPF.Messaging;
using TravelAgency.WPF.Messaging.Messages;
using TravelAgency.WPF.Commands;
using TravelAgency.WPF.Views;
using System.Linq;

namespace TravelAgency.WPF.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly AuthenticationService _authenticationService;
        private readonly IUserRepository _userRepository;
        private readonly Window _loginWindow;
        private readonly IMediator _mediator;
        public ICommand OpenRegisterCommand { get; }
        public ICommand OpenForgotPasswordCommand { get; }

        private string _email = "";
        private string _password = "";
        private string _errorMessage = "";

        public string Email
        {
            get => _email;
            set => Set(ref _email, value);
        }

        public string Password
        {
            get => _password;
            set => Set(ref _password, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => Set(ref _errorMessage, value);
        }

        public ICommand LoginCommand { get; }

        public LoginViewModel(Window loginWindow, IMediator mediator)
        {
            _loginWindow = loginWindow;
            _mediator = mediator;
            _userRepository = new EfUserRepository();
            _authenticationService = new AuthenticationService(new EfUserRepository());
            LoginCommand = new RelayCommand(Login);
            OpenRegisterCommand = new RelayCommand(OpenRegister);
            OpenForgotPasswordCommand = new RelayCommand(OpenForgotPassword);

        }

        private void Login()
        {
            ErrorMessage = "";

            var request = new LoginRequest
            {
                Email = Email?.Trim() ?? "",
                Password = Password ?? ""
            };

            try
            {
                var validator = new LoginRequestValidator();
                validator.ValidateAndThrow(request);
            }
            catch (ValidationException ex)
            {
                ErrorMessage = ex.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid login input.";
                return;
            }

            var user = _authenticationService.Authenticate(request.Email, request.Password);

            if (user == null)
            {
                ErrorMessage = "Invalid email or password.";
                return;
            }

            user.Login();
            _userRepository.Update(user);

            SessionManager.Instance.CurrentSession.StartSession(user);

            _mediator.Publish(new UserLoggedInMessage(user, _loginWindow));
        }

        private void OpenRegister()
        {
            var registerWindow = new RegisterWindow();
            registerWindow.Show();
            _loginWindow.Close();
        }

        private void OpenForgotPassword()
        {
            var forgotWindow = new ForgotPasswordWindow
            {
                Owner = _loginWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            forgotWindow.ShowDialog();
        }
    }
}
