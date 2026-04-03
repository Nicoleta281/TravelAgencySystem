using FluentValidation;
using System.Windows;
using System.Windows.Input;
using TravelAgency.Core.Data.Repositories;
using TravelAgency.Core.Models.Users;
using TravelAgency.Core.Models.Users.Access;
using TravelAgency.Core.Services;
using TravelAgency.Core.Validators;
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
        public ICommand OpenRegisterCommand { get; }

        private string _username = "";
        private string _password = "";
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

        public string ErrorMessage
        {
            get => _errorMessage;
            set => Set(ref _errorMessage, value);
        }

        public ICommand LoginCommand { get; }

        public LoginViewModel(Window loginWindow)
        {
            _loginWindow = loginWindow;
            _userRepository = new EfUserRepository();
            _authenticationService = new AuthenticationService(new EfUserRepository());
            LoginCommand = new RelayCommand(Login);
            OpenRegisterCommand = new RelayCommand(OpenRegister);

        }

        private void Login()
        {
            ErrorMessage = "";

            var request = new LoginRequest
            {
                Username = Username?.Trim() ?? "",
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

            var user = _authenticationService.Authenticate(request.Username, request.Password);

            if (user == null)
            {
                ErrorMessage = "Invalid username or password.";
                return;
            }

            user.Login();
            _userRepository.Update(user);

            SessionManager.Instance.CurrentSession.StartSession(user);

            bool opened = OpenWorkspaceByRole(user);

            if (opened)
            {
                _loginWindow.Close();
            }
        }

        private bool OpenWorkspaceByRole(User user)
        {
            if (user is TravelAgency.Core.Models.Users.Admin)
            {
                var adminWindow = new AdminWindow();
                adminWindow.Show();
                return true;
            }

            if (user is Agent)
            {
                var agentWindow = new AgentWindow();
                agentWindow.Show();
                return true;
            }

            if (user is Client)
            {
                var clientWindow = new ClientWindow();
                clientWindow.Show();
                return true;
            }

            MessageBox.Show("Unknown user type assigned to this user.",
                            "Login",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

            return false;
        }

        private void OpenRegister()
        {
            var registerWindow = new RegisterWindow();
            registerWindow.Show();
            _loginWindow.Close();
        }
    }
}
