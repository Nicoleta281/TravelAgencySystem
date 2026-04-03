using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using TravelAgency.Core.Data.Repositories;
using TravelAgency.Core.Models.Users.Access;
using TravelAgency.Core.Services;
using TravelAgency.WPF.Commands;
using TravelAgency.WPF.Commands.Admin;
using TravelAgency.WPF.ViewModels.Admin;
using TravelAgency.WPF.Views;

namespace TravelAgency.WPF.ViewModels.AdminVM
{
    public class AdminViewModel : ViewModelBase
    {
        private int _totalUsersCount;
        private int _pendingBookingsCount;
        private decimal _monthlyRevenue;
        private double _cancellationRate;

        private int _vatRate;
        private int _globalDiscount;
        private string _selectedCurrency = "USD";

        private AdminUserRowViewModel? _selectedUser;
        private ModerationPackageViewModel? _selectedPackage;

        public ObservableCollection<AdminUserRowViewModel> Users { get; set; }
        public ObservableCollection<ModerationPackageViewModel> PendingPackages { get; set; }
        public ObservableCollection<string> AvailableCurrencies { get; set; }
        private readonly IUserRepository _userRepository;

        public int TotalUsersCount
        {
            get => _totalUsersCount;
            set => Set(ref _totalUsersCount, value);
        }

        public int PendingBookingsCount
        {
            get => _pendingBookingsCount;
            set => Set(ref _pendingBookingsCount, value);
        }

        public decimal MonthlyRevenue
        {
            get => _monthlyRevenue;
            set => Set(ref _monthlyRevenue, value);
        }

        public double CancellationRate
        {
            get => _cancellationRate;
            set => Set(ref _cancellationRate, value);
        }

        public int VatRate
        {
            get => _vatRate;
            set => Set(ref _vatRate, value);
        }

        public int GlobalDiscount
        {
            get => _globalDiscount;
            set => Set(ref _globalDiscount, value);
        }

        public string SelectedCurrency
        {
            get => _selectedCurrency;
            set => Set(ref _selectedCurrency, value);
        }

        public AdminUserRowViewModel? SelectedUser
        {
            get => _selectedUser;
            set
            {
                Set(ref _selectedUser, value);
                RaiseCommandStates();
            }
        }

        public ModerationPackageViewModel? SelectedPackage
        {
            get => _selectedPackage;
            set
            {
                Set(ref _selectedPackage, value);
                RaiseCommandStates();
            }
        }

        public string MonthlyRevenueText => $"{MonthlyRevenue:N0} {SelectedCurrency}";
        public string CancellationRateText => $"{CancellationRate:F1}%";

        public int ActiveUsersCount => Users.Count(x => x.Status == "Active");
        public int BlockedUsersCount => Users.Count(x => x.Status == "Blocked");

        public ICommand BlockUserCommand { get; }
        public ICommand UnblockUserCommand { get; }
        public ICommand ChangeRoleCommand { get; }
        public ICommand ApprovePackageCommand { get; }
        public ICommand RejectPackageCommand { get; }
        public ICommand SaveFinancialSettingsCommand { get; }
        public ICommand RefreshDashboardCommand { get; }
        public ICommand LogoutCommand { get; }

        public AdminViewModel()
        {
            _userRepository = new EfUserRepository();

            Users = new ObservableCollection<AdminUserRowViewModel>();
            PendingPackages = new ObservableCollection<ModerationPackageViewModel>();
            AvailableCurrencies = new ObservableCollection<string> { "USD", "EUR", "MDL" };

            BlockUserCommand = new RelayCommand(BlockSelectedUser, CanBlockSelectedUser);
            UnblockUserCommand = new RelayCommand(UnblockSelectedUser, CanUnblockSelectedUser);
            ChangeRoleCommand = new RelayCommand(ChangeRoleOfSelectedUser, CanChangeRole);
            ApprovePackageCommand = new RelayCommand(ApproveSelectedPackage, CanModeratePackage);
            RejectPackageCommand = new RelayCommand(RejectSelectedPackage, CanModeratePackage);
            SaveFinancialSettingsCommand = new RelayCommand(SaveFinancialSettings);
            RefreshDashboardCommand = new RelayCommand(LoadDataFromDatabase);
            LogoutCommand = new RelayCommand(Logout);

            LoadDataFromDatabase();
        }

        private void LoadDataFromDatabase()
        {
            Users.Clear();
            PendingPackages.Clear();

            var dbUsers = _userRepository.GetAll();

            foreach (var user in dbUsers)
            {
                Users.Add(new AdminUserRowViewModel
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = $"{user.Username}@travelagency.com",
                    Role = user.Role?.Name ?? "",
                    Status = user.IsBlocked ? "Blocked" : "Active"
                });
            }

            TotalUsersCount = Users.Count;
            PendingBookingsCount = 18;
            MonthlyRevenue = 45200;
            CancellationRate = 12.5;

            VatRate = 19;
            GlobalDiscount = 10;
            SelectedCurrency = "USD";

            PendingPackages.Add(new ModerationPackageViewModel
            {
                Id = 1,
                Name = "Tropical Paradise",
                Destination = "Bali",
                PeriodText = "05 Jul - 12 Jul",
                CreatedByAgent = "agent1",
                Status = "Pending"
            });

            PendingPackages.Add(new ModerationPackageViewModel
            {
                Id = 2,
                Name = "Maldives Escape",
                Destination = "Maldives",
                PeriodText = "20 Jul - 27 Jul",
                CreatedByAgent = "agent2",
                Status = "Pending"
            });

            PendingPackages.Add(new ModerationPackageViewModel
            {
                Id = 3,
                Name = "Rome City Break",
                Destination = "Rome",
                PeriodText = "01 Aug - 06 Aug",
                CreatedByAgent = "agent1",
                Status = "Pending"
            });

            OnPropertyChanged(nameof(MonthlyRevenueText));
            OnPropertyChanged(nameof(CancellationRateText));
            OnPropertyChanged(nameof(ActiveUsersCount));
            OnPropertyChanged(nameof(BlockedUsersCount));

            RaiseCommandStates();
        }

        private void BlockSelectedUser()
        {
            if (SelectedUser == null)
                return;

            var command = new BlockSelectedUserAdminCommand(SelectedUser);

            if (!command.CanExecute())
                return;

            command.Execute();

            var user = _userRepository.GetById(SelectedUser.Id);
            if (user != null)
            {
                user.IsBlocked = true;
                _userRepository.Update(user);
            }

            LoadDataFromDatabase();
        }

        private bool CanBlockSelectedUser()
        {
            return SelectedUser != null && SelectedUser.Status != "Blocked";
        }

        private void UnblockSelectedUser()
        {
            if (SelectedUser == null)
                return;

            var command = new UnblockSelectedUserAdminCommand(SelectedUser);

            if (!command.CanExecute())
                return;

            command.Execute();

            var user = _userRepository.GetById(SelectedUser.Id);
            if (user != null)
            {
                user.IsBlocked = false;
                _userRepository.Update(user);
            }

            LoadDataFromDatabase();
        }

        private bool CanUnblockSelectedUser()
        {
            return SelectedUser != null && SelectedUser.Status == "Blocked";
        }

        private void ChangeRoleOfSelectedUser()
        {
            if (SelectedUser == null)
                return;

            var command = new ChangeUserRoleAdminCommand(SelectedUser);

            if (!command.CanExecute())
                return;

            command.Execute();

            var user = _userRepository.GetById(SelectedUser.Id);
            if (user != null)
            {
                user.Role = new Role { Name = SelectedUser.Role };
                _userRepository.Update(user);
                LoadDataFromDatabase();
            }

            RaiseCommandStates();
        }

        private bool CanChangeRole()
        {
            return SelectedUser != null;
        }

        private void ApproveSelectedPackage()
        {
            if (SelectedPackage == null)
                return;

            var command = new ApprovePackageAdminCommand(SelectedPackage, PendingPackages);

            if (!command.CanExecute())
                return;

            command.Execute();
            SelectedPackage = null;
            MessageBox.Show("Package approved successfully.", "Moderation", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RejectSelectedPackage()
        {
            if (SelectedPackage == null)
                return;

            var command = new RejectPackageAdminCommand(SelectedPackage, PendingPackages);

            if (!command.CanExecute())
                return;

            command.Execute();
            SelectedPackage = null;
            MessageBox.Show("Package rejected successfully.", "Moderation", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private bool CanModeratePackage()
        {
            return SelectedPackage != null;
        }

        private void SaveFinancialSettings()
        {
            if (VatRate < 0 || VatRate > 100)
            {
                MessageBox.Show("VAT rate must be between 0 and 100.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (GlobalDiscount < 0 || GlobalDiscount > 100)
            {
                MessageBox.Show("Global discount must be between 0 and 100.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            OnPropertyChanged(nameof(MonthlyRevenueText));
            MessageBox.Show("Financial settings saved successfully.", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RaiseCommandStates()
        {
            if (BlockUserCommand is RelayCommand blockCmd)
                blockCmd.RaiseCanExecuteChanged();

            if (UnblockUserCommand is RelayCommand unblockCmd)
                unblockCmd.RaiseCanExecuteChanged();

            if (ChangeRoleCommand is RelayCommand roleCmd)
                roleCmd.RaiseCanExecuteChanged();

            if (ApprovePackageCommand is RelayCommand approveCmd)
                approveCmd.RaiseCanExecuteChanged();

            if (RejectPackageCommand is RelayCommand rejectCmd)
                rejectCmd.RaiseCanExecuteChanged();
        }

        private void Logout()
        {
            var currentUser = SessionManager.Instance.CurrentSession.CurrentUser;

            if (currentUser != null)
            {
                currentUser.Logout();
                _userRepository.Update(currentUser);
            }

            SessionManager.Instance.CurrentSession.EndSession();

            var loginWindow = new LoginWindow();
            loginWindow.Show();

            Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w is Views.AdminWindow)
                ?.Close();
        }
    }
}