using FluentValidation;
using System;
using System.IO;
using System.Net.Sockets;
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
using TravelAgency.WPF.Services.Navigation;
using System.Linq;

namespace TravelAgency.WPF.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly AuthenticationService _authenticationService;
        private readonly IUserRepository _userRepository;
        private readonly Window _loginWindow;
        private readonly IMediator _mediator;
        private readonly INavigationService _navigation;
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

        public LoginViewModel(
            Window loginWindow,
            IMediator mediator,
            IUserRepository userRepository,
            AuthenticationService authenticationService,
            INavigationService navigation)
        {
            _loginWindow = loginWindow;
            _mediator = mediator;
            _navigation = navigation ?? throw new System.ArgumentNullException(nameof(navigation));
            _userRepository = userRepository ?? throw new System.ArgumentNullException(nameof(userRepository));
            _authenticationService = authenticationService ?? throw new System.ArgumentNullException(nameof(authenticationService));
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

            try
            {
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
            catch (Exception ex) when (IsLikelyDatabaseOrNetworkFailure(ex))
            {
                ErrorMessage =
                    "Nu mă pot conecta stabil la baza de date. Verifică că PostgreSQL rulează, rețeaua/VPN-ul, " +
                    "șirul TravelAgencyDb și variabila TRAVEL_AGENCY_DB_PASSWORD, apoi încearcă din nou.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Eroare la autentificare: {ex.Message}";
            }
        }

        private static bool IsLikelyDatabaseOrNetworkFailure(Exception ex)
        {
            for (var cur = ex; cur != null; cur = cur.InnerException)
            {
                if (cur is IOException or SocketException or TimeoutException)
                    return true;

                if (string.Equals(cur.GetType().Namespace, "Npgsql", StringComparison.Ordinal))
                    return true;

                if (cur is InvalidOperationException io &&
                    io.Message.Contains("transient", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private void OpenRegister()
        {
            _navigation.ShowRegisterAndClose(_loginWindow);
        }

        private void OpenForgotPassword()
        {
            _navigation.ShowForgotPasswordDialog(_loginWindow);
        }
    }
}
