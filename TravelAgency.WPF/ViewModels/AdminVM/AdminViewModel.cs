using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using TravelAgency.Core.Data;
using TravelAgency.Core.Data.Mappers;
using TravelAgency.Core.Data.Repositories;
using TravelAgency.Core.Models.Analytics;
using TravelAgency.Core.Models.Booking;
using TravelAgency.Core.Models.Users.Access;
using TravelAgency.Core.Patterns.ChainOfResponsibility;
using TravelAgency.Core.Patterns.Memento;
using TravelAgency.Core.Services;
using TravelAgency.WPF.Commands;
using TravelAgency.WPF.Commands.Admin;
using TravelAgency.WPF.ViewModels.Admin;
using TravelAgency.WPF.Messaging.Messages;
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
        private string _userFilterText = "";
        private readonly ICollectionView _usersView;

        public ObservableCollection<AdminUserRowViewModel> Users { get; set; }
        public ObservableCollection<ModerationPackageViewModel> PendingPackages { get; set; }
        public ObservableCollection<string> AvailableCurrencies { get; set; }
        private readonly IUserRepository _userRepository;
        private readonly IAdminAnalyticsSnapshotRepository _snapshotRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly BookingService _bookingService;
        private readonly IBookingApprovalHandler _bookingApprovalChain;

        private Booking? _selectedBooking;
        private string _bookingFilterText = "";
        private string _selectedBookingStatusFilter = "All";
        private readonly ICollectionView _bookingsView;

        private AdminAnalyticsState _liveAnalytics = new();
        public AdminAnalyticsState LiveAnalytics
        {
            get => _liveAnalytics;
            set => Set(ref _liveAnalytics, value);
        }

        public ObservableCollection<AdminAnalyticsMemento> AnalyticsSnapshots { get; set; } = new();

        private AdminAnalyticsMemento? _selectedSnapshot;
        public bool HasSelectedSnapshot => SelectedSnapshot != null;
        public AdminAnalyticsMemento? SelectedSnapshot
        {
            get => _selectedSnapshot;
            set
            {
                Set(ref _selectedSnapshot, value);

                OnPropertyChanged(nameof(HasSelectedSnapshot));
                RaiseAnalyticsComparisonProperties();

                if (RestoreAnalyticsSnapshotCommand is RelayCommand cmd)
                    cmd.RaiseCanExecuteChanged();
            }
        }
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

        public string UserFilterText
        {
            get => _userFilterText;
            set
            {
                if (!Set(ref _userFilterText, value))
                    return;

                _usersView.Refresh();
                if (SelectedUser != null
                    && !_usersView.Cast<AdminUserRowViewModel>().Contains(SelectedUser))
                {
                    SelectedUser = null;
                }
            }
        }

        public ICollectionView UsersView => _usersView;

        public ICollectionView BookingsView => _bookingsView;

        public Booking? SelectedBooking
        {
            get => _selectedBooking;
            set
            {
                if (Set(ref _selectedBooking, value))
                    RaiseBookingCommandStates();
            }
        }

        public string BookingFilterText
        {
            get => _bookingFilterText;
            set
            {
                if (!Set(ref _bookingFilterText, value))
                    return;

                _bookingsView.Refresh();
                ClearSelectedBookingIfHidden();
            }
        }

        public string SelectedBookingStatusFilter
        {
            get => _selectedBookingStatusFilter;
            set
            {
                if (!Set(ref _selectedBookingStatusFilter, value))
                    return;

                _bookingsView.Refresh();
                ClearSelectedBookingIfHidden();
            }
        }

        public int AdminBookingsTotalCount => AdminBookings.Count;

        public int AdminBookingsPendingCount =>
            AdminBookings.Count(b => string.Equals(b.StatusName, "Pending", StringComparison.OrdinalIgnoreCase));

        public int AdminBookingsConfirmedCount =>
            AdminBookings.Count(b => string.Equals(b.StatusName, "Confirmed", StringComparison.OrdinalIgnoreCase));

        public int AdminBookingsRejectedCount =>
            AdminBookings.Count(b => string.Equals(b.StatusName, "Rejected", StringComparison.OrdinalIgnoreCase));

        public int AdminBookingsCancelledCount =>
            AdminBookings.Count(b => string.Equals(b.StatusName, "Cancelled", StringComparison.OrdinalIgnoreCase));

        public ICommand BlockUserCommand { get; }
        public ICommand UnblockUserCommand { get; }
        public ICommand ChangeRoleCommand { get; }
        public ICommand ApprovePackageCommand { get; }
        public ICommand RejectPackageCommand { get; }
        public ICommand SaveFinancialSettingsCommand { get; }
        public ICommand RefreshDashboardCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand SaveAnalyticsSnapshotCommand { get; }
        public ICommand RestoreAnalyticsSnapshotCommand { get; }
        public ICommand ShowDashboardCommand { get; }
        public ICommand ShowAnalyticsCommand { get; }
        public ICommand ShowUserManagementCommand { get; }
        public ICommand ShowBookingsCommand { get; }
        public ICommand ShowFinancialSettingsCommand { get; }
        public ICommand ShowPackageModerationCommand { get; }
        public ICommand RefreshBookingsCommand { get; }
        public ICommand ConfirmBookingCommand { get; }
        public ICommand RejectBookingCommand { get; }
        public ICommand CancelBookingCommand { get; }

        public ObservableCollection<Booking> AdminBookings { get; } = new();
        public ObservableCollection<string> BookingStatusFilters { get; } = new()
        {
            "All", "Pending", "Confirmed", "Rejected", "Cancelled"
        };

        public AdminViewModel(
            IUserRepository userRepository,
            IAdminAnalyticsSnapshotRepository snapshotRepository,
            IBookingRepository bookingRepository,
            BookingService bookingService,
            BookingApprovalChainFactory bookingApprovalChainFactory)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _snapshotRepository = snapshotRepository ?? throw new ArgumentNullException(nameof(snapshotRepository));
            _bookingRepository = bookingRepository ?? throw new ArgumentNullException(nameof(bookingRepository));
            _bookingService = bookingService ?? throw new ArgumentNullException(nameof(bookingService));
            _bookingApprovalChain = (bookingApprovalChainFactory ?? throw new ArgumentNullException(nameof(bookingApprovalChainFactory)))
                .Create();
            Users = new ObservableCollection<AdminUserRowViewModel>();
            PendingPackages = new ObservableCollection<ModerationPackageViewModel>();
            AvailableCurrencies = new ObservableCollection<string> { "USD", "EUR", "MDL" };

            _usersView = CollectionViewSource.GetDefaultView(Users);
            _usersView.Filter = UserMatchesFilter;

            _bookingsView = CollectionViewSource.GetDefaultView(AdminBookings);
            _bookingsView.Filter = AdminBookingMatchesFilter;
            AdminBookings.CollectionChanged += (_, __) => RefreshAdminBookingKpiProperties();

            BlockUserCommand = new RelayCommand(BlockSelectedUser, CanBlockSelectedUser);
            UnblockUserCommand = new RelayCommand(UnblockSelectedUser, CanUnblockSelectedUser);
            ChangeRoleCommand = new RelayCommand(ChangeRoleOfSelectedUser, CanChangeRole);
            ApprovePackageCommand = new RelayCommand(ApproveSelectedPackage, CanModeratePackage);
            RejectPackageCommand = new RelayCommand(RejectSelectedPackage, CanModeratePackage);
            SaveFinancialSettingsCommand = new RelayCommand(SaveFinancialSettings);
            RefreshDashboardCommand = new RelayCommand(LoadDataFromDatabase);
            LogoutCommand = new RelayCommand(Logout);
            SaveAnalyticsSnapshotCommand = new RelayCommand(() => SaveAnalyticsSnapshot());
           
            RestoreAnalyticsSnapshotCommand = new RelayCommand(
                () => RestoreAnalyticsSnapshot(),
                () => SelectedSnapshot != null
            );

            ShowDashboardCommand = new RelayCommand(() => ShowDashboard());
            ShowAnalyticsCommand = new RelayCommand(() => ShowAnalytics());
            ShowUserManagementCommand = new RelayCommand(() => ShowUserManagement());
            ShowBookingsCommand = new RelayCommand(ShowBookingsSection);
            ShowFinancialSettingsCommand = new RelayCommand(ShowFinancialSettingsSection);
            ShowPackageModerationCommand = new RelayCommand(ShowPackageModerationSection);
            RefreshBookingsCommand = new RelayCommand(LoadAdminBookings);
            ConfirmBookingCommand = new RelayCommand(ConfirmAdminBooking, CanConfirmAdminBooking);
            RejectBookingCommand = new RelayCommand(RejectAdminBooking, CanRejectAdminBooking);
            CancelBookingCommand = new RelayCommand(CancelAdminBooking, CanCancelAdminBooking);
            LoadDataFromDatabase();
            LoadLiveAnalytics();
            LoadAnalyticsSnapshotsFromDatabase();
        }
        private void RestoreAnalyticsSnapshot()
        {
            if (SelectedSnapshot == null)
                return;

            LiveAnalytics.Restore(SelectedSnapshot);

            OnPropertyChanged(nameof(LiveAnalytics));
            OnPropertyChanged(nameof(TotalBookingsText));
            OnPropertyChanged(nameof(ConfirmedBookingsText));
            OnPropertyChanged(nameof(RejectedBookingsText));
            OnPropertyChanged(nameof(TotalRevenueText));
            OnPropertyChanged(nameof(TotalUsersText));
            OnPropertyChanged(nameof(TopDestinationText));
            RaiseAnalyticsComparisonProperties();
        }

        private string _currentSection = "Dashboard";
        public string CurrentSection
        {
            get => _currentSection;
            set
            {
                Set(ref _currentSection, value);

                OnPropertyChanged(nameof(IsDashboardSection));
                OnPropertyChanged(nameof(IsAnalyticsSection));
                OnPropertyChanged(nameof(IsUserManagementSection));
                OnPropertyChanged(nameof(IsBookingsSection));
                OnPropertyChanged(nameof(IsFinancialSection));
                OnPropertyChanged(nameof(IsPackageModerationSection));
            }
        }

        public bool IsDashboardSection => CurrentSection == "Dashboard";
        public bool IsAnalyticsSection => CurrentSection == "Analytics";
        public bool IsUserManagementSection => CurrentSection == "Users";
        public bool IsBookingsSection => CurrentSection == "Bookings";
        public bool IsFinancialSection => CurrentSection == "Financial";
        public bool IsPackageModerationSection => CurrentSection == "Packages";
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
                    Email = user.Email ?? "",
                    Role = user.Role?.Name ?? "",
                    Status = user.IsBlocked ? "Blocked" : "Active"
                });
            }

            TotalUsersCount = Users.Count;
            PendingBookingsCount = _bookingRepository.GetPending().Count;
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
            _usersView.Refresh();
        }

        private bool UserMatchesFilter(object obj)
        {
            if (obj is not AdminUserRowViewModel u)
                return false;

            if (string.IsNullOrWhiteSpace(UserFilterText))
                return true;

            var q = UserFilterText.Trim();
            return u.Username.Contains(q, StringComparison.OrdinalIgnoreCase)
                   || u.Email.Contains(q, StringComparison.OrdinalIgnoreCase)
                   || u.Role.Contains(q, StringComparison.OrdinalIgnoreCase)
                   || u.Status.Contains(q, StringComparison.OrdinalIgnoreCase);
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

            RaiseBookingCommandStates();
        }

        private void RaiseBookingCommandStates()
        {
            if (ConfirmBookingCommand is RelayCommand c1)
                c1.RaiseCanExecuteChanged();

            if (RejectBookingCommand is RelayCommand c2)
                c2.RaiseCanExecuteChanged();

            if (CancelBookingCommand is RelayCommand c3)
                c3.RaiseCanExecuteChanged();
        }

        private void RefreshAdminBookingKpiProperties()
        {
            OnPropertyChanged(nameof(AdminBookingsTotalCount));
            OnPropertyChanged(nameof(AdminBookingsPendingCount));
            OnPropertyChanged(nameof(AdminBookingsConfirmedCount));
            OnPropertyChanged(nameof(AdminBookingsRejectedCount));
            OnPropertyChanged(nameof(AdminBookingsCancelledCount));
        }

        private void ClearSelectedBookingIfHidden()
        {
            if (SelectedBooking == null)
                return;

            foreach (var o in _bookingsView)
            {
                if (ReferenceEquals(o, SelectedBooking))
                    return;
            }

            SelectedBooking = null;
        }

        private bool AdminBookingMatchesFilter(object obj)
        {
            if (obj is not Booking b)
                return false;

            if (!string.Equals(SelectedBookingStatusFilter, "All", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(b.StatusName, SelectedBookingStatusFilter, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(BookingFilterText))
                return true;

            var q = BookingFilterText.Trim();
            var client = b.Client?.Username ?? "";
            var pkg = b.TripPackage?.Name ?? "";

            return client.Contains(q, StringComparison.OrdinalIgnoreCase)
                   || pkg.Contains(q, StringComparison.OrdinalIgnoreCase)
                   || b.StatusName.Contains(q, StringComparison.OrdinalIgnoreCase)
                   || b.Id.ToString().Contains(q, StringComparison.OrdinalIgnoreCase);
        }

        private void ShowBookingsSection()
        {
            CurrentSection = "Bookings";
            LoadAdminBookings();
        }

        private void ShowFinancialSettingsSection()
        {
            CurrentSection = "Financial";
        }

        private void ShowPackageModerationSection()
        {
            CurrentSection = "Packages";
        }

        private void LoadAdminBookings()
        {
            AdminBookings.Clear();

            foreach (var b in _bookingRepository.GetAll())
                AdminBookings.Add(b);

            PendingBookingsCount = _bookingRepository.GetPending().Count;
            _bookingsView.Refresh();
            RefreshAdminBookingKpiProperties();
            SelectedBooking = null;
            RaiseBookingCommandStates();
        }

        private bool CanConfirmAdminBooking()
        {
            return SelectedBooking != null
                   && string.Equals(SelectedBooking.StatusName, "Pending", StringComparison.OrdinalIgnoreCase);
        }

        private bool CanRejectAdminBooking()
        {
            return SelectedBooking != null
                   && string.Equals(SelectedBooking.StatusName, "Pending", StringComparison.OrdinalIgnoreCase);
        }

        private bool CanCancelAdminBooking()
        {
            if (SelectedBooking == null)
                return false;

            return string.Equals(SelectedBooking.StatusName, "Pending", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(SelectedBooking.StatusName, "Confirmed", StringComparison.OrdinalIgnoreCase);
        }

        private async void ConfirmAdminBooking()
        {
            if (SelectedBooking == null)
            {
                MessageBox.Show("Select a booking first.", "Confirm", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var context = new BookingApprovalContext(SelectedBooking);
            var approvalResult = _bookingApprovalChain.Handle(context);

            if (!approvalResult.IsApproved)
            {
                MessageBox.Show(approvalResult.Message, "Approval blocked", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var bookingToApprove = SelectedBooking;
                bookingToApprove.IsBeingRemoved = true;

                await Task.Delay(350).ConfigureAwait(true);

                _bookingService.ConfirmBooking(bookingToApprove);
                SelectedBooking = null;

                MessageBox.Show("Booking confirmed.", "Bookings", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Bookings", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                LoadAdminBookings();
                LoadLiveAnalytics();
            }
        }

        private async void RejectAdminBooking()
        {
            if (SelectedBooking == null)
            {
                MessageBox.Show("Select a booking first.", "Reject", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var bookingToReject = SelectedBooking;
                bookingToReject.IsBeingRemoved = true;

                await Task.Delay(350).ConfigureAwait(true);

                _bookingService.RejectBooking(bookingToReject);
                SelectedBooking = null;

                MessageBox.Show("Booking rejected.", "Bookings", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Bookings", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                LoadAdminBookings();
                LoadLiveAnalytics();
            }
        }

        private void CancelAdminBooking()
        {
            if (SelectedBooking == null)
            {
                MessageBox.Show("Select a booking first.", "Cancel", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _bookingService.CancelBooking(SelectedBooking);
                SelectedBooking = null;
                MessageBox.Show("Booking cancelled.", "Bookings", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Bookings", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                LoadAdminBookings();
                LoadLiveAnalytics();
            }
        }

        public string TotalBookingsText => LiveAnalytics.TotalBookings.ToString();
        public string ConfirmedBookingsText => LiveAnalytics.ConfirmedBookings.ToString();
        public string RejectedBookingsText => LiveAnalytics.RejectedBookings.ToString();
        public string TotalRevenueText => $"{LiveAnalytics.TotalRevenue:F2} €";
        public string TotalUsersText => LiveAnalytics.TotalUsers.ToString();
        public string TopDestinationText => LiveAnalytics.TopDestination;

        private const double AnalyticsBarMaxHeight = 110;

        public double AnalyticsBarBookingsSnapshot =>
            SelectedSnapshot == null
                ? 0
                : AnalyticsBarMaxHeight * SelectedSnapshot.TotalBookings
                  / Math.Max(Math.Max(SelectedSnapshot.TotalBookings, LiveAnalytics.TotalBookings), 1);

        public double AnalyticsBarBookingsLive =>
            SelectedSnapshot == null
                ? 0
                : AnalyticsBarMaxHeight * LiveAnalytics.TotalBookings
                  / Math.Max(Math.Max(SelectedSnapshot.TotalBookings, LiveAnalytics.TotalBookings), 1);

        public double AnalyticsBarConfirmedSnapshot =>
            SelectedSnapshot == null
                ? 0
                : AnalyticsBarMaxHeight * SelectedSnapshot.ConfirmedBookings
                  / Math.Max(Math.Max(SelectedSnapshot.ConfirmedBookings, LiveAnalytics.ConfirmedBookings), 1);

        public double AnalyticsBarConfirmedLive =>
            SelectedSnapshot == null
                ? 0
                : AnalyticsBarMaxHeight * LiveAnalytics.ConfirmedBookings
                  / Math.Max(Math.Max(SelectedSnapshot.ConfirmedBookings, LiveAnalytics.ConfirmedBookings), 1);

        public double AnalyticsBarRejectedSnapshot =>
            SelectedSnapshot == null
                ? 0
                : AnalyticsBarMaxHeight * SelectedSnapshot.RejectedBookings
                  / Math.Max(Math.Max(SelectedSnapshot.RejectedBookings, LiveAnalytics.RejectedBookings), 1);

        public double AnalyticsBarRejectedLive =>
            SelectedSnapshot == null
                ? 0
                : AnalyticsBarMaxHeight * LiveAnalytics.RejectedBookings
                  / Math.Max(Math.Max(SelectedSnapshot.RejectedBookings, LiveAnalytics.RejectedBookings), 1);

        public double AnalyticsBarRevenueSnapshot =>
            SelectedSnapshot == null
                ? 0
                : AnalyticsBarMaxHeight * SelectedSnapshot.TotalRevenue
                  / Math.Max(Math.Max(SelectedSnapshot.TotalRevenue, LiveAnalytics.TotalRevenue), 1.0);

        public double AnalyticsBarRevenueLive =>
            SelectedSnapshot == null
                ? 0
                : AnalyticsBarMaxHeight * LiveAnalytics.TotalRevenue
                  / Math.Max(Math.Max(SelectedSnapshot.TotalRevenue, LiveAnalytics.TotalRevenue), 1.0);

        public double AnalyticsBarUsersSnapshot =>
            SelectedSnapshot == null
                ? 0
                : AnalyticsBarMaxHeight * SelectedSnapshot.TotalUsers
                  / Math.Max(Math.Max(SelectedSnapshot.TotalUsers, LiveAnalytics.TotalUsers), 1);

        public double AnalyticsBarUsersLive =>
            SelectedSnapshot == null
                ? 0
                : AnalyticsBarMaxHeight * LiveAnalytics.TotalUsers
                  / Math.Max(Math.Max(SelectedSnapshot.TotalUsers, LiveAnalytics.TotalUsers), 1);

        private void RaiseAnalyticsComparisonProperties()
        {
            OnPropertyChanged(nameof(AnalyticsBarBookingsSnapshot));
            OnPropertyChanged(nameof(AnalyticsBarBookingsLive));
            OnPropertyChanged(nameof(AnalyticsBarConfirmedSnapshot));
            OnPropertyChanged(nameof(AnalyticsBarConfirmedLive));
            OnPropertyChanged(nameof(AnalyticsBarRejectedSnapshot));
            OnPropertyChanged(nameof(AnalyticsBarRejectedLive));
            OnPropertyChanged(nameof(AnalyticsBarRevenueSnapshot));
            OnPropertyChanged(nameof(AnalyticsBarRevenueLive));
            OnPropertyChanged(nameof(AnalyticsBarUsersSnapshot));
            OnPropertyChanged(nameof(AnalyticsBarUsersLive));
        }

        private void LoadLiveAnalytics()
        {
            var bookings = _bookingRepository.GetAll();

            LiveAnalytics.TotalBookings = bookings.Count;
            LiveAnalytics.ConfirmedBookings = bookings.Count(b => b.StatusName == "Confirmed");
            LiveAnalytics.RejectedBookings = bookings.Count(b => b.StatusName == "Rejected");

            LiveAnalytics.TotalRevenue = bookings
                .Where(b => b.StatusName == "Confirmed")
                .Sum(b => b.TotalPrice);

            LiveAnalytics.TopDestination = bookings
                .Where(b => b.TripPackage != null)
                .GroupBy(b => b.TripPackage!.Name)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault() ?? "N/A";

            var users = _userRepository.GetAll();

            LiveAnalytics.TotalUsers = users.Count;
         
            OnPropertyChanged(nameof(LiveAnalytics));
            OnPropertyChanged(nameof(TotalBookingsText));
            OnPropertyChanged(nameof(ConfirmedBookingsText));
            OnPropertyChanged(nameof(RejectedBookingsText));
            OnPropertyChanged(nameof(TotalRevenueText));
            OnPropertyChanged(nameof(TotalUsersText));
            OnPropertyChanged(nameof(TopDestinationText));
            RaiseAnalyticsComparisonProperties();
        }
        private void SaveAnalyticsSnapshot()
        {
            LoadDataFromDatabase();
            LoadLiveAnalytics();

            var snapshot = LiveAnalytics.Save();

            var entity = AdminAnalyticsSnapshotMapper.ToEntity(
                snapshot,
                ActiveUsersCount,
                BlockedUsersCount);

            _snapshotRepository.Add(entity);

            LoadAnalyticsSnapshotsFromDatabase();
        }

        private void LoadAnalyticsSnapshotsFromDatabase()
        {
            AnalyticsSnapshots.Clear();

            var entities = _snapshotRepository.GetAll();

            foreach (var entity in entities)
            {
                var memento = AdminAnalyticsSnapshotMapper.ToMemento(entity);
                AnalyticsSnapshots.Add(memento);
            }
        }

        private void ShowDashboard()
        {
            CurrentSection = "Dashboard";
            PendingBookingsCount = _bookingRepository.GetPending().Count;
        }

        private void ShowAnalytics()
        {
            CurrentSection = "Analytics";
            LoadLiveAnalytics();
        }

        private void ShowUserManagement()
        {
            CurrentSection = "Users";
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

            var window = Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w is Views.AdminWindow);

            if (window != null)
                App.Mediator.Publish(new LogoutRequestedMessage(window));
        }
    }
}