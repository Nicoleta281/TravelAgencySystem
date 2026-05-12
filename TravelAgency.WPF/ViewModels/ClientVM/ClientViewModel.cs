using FluentValidation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using TravelAgency.Core.Data.Repositories;
using TravelAgency.Core.Models;
using TravelAgency.Core.Models.Booking;
using TravelAgency.Core.Models.Notifications;
using TravelAgency.Core.Models.Locations;
using TravelAgency.Core.Models.TripPkg.Package;
using TravelAgency.Core.Models.Users;
using TravelAgency.Core.Patterns.Decorator;
using TravelAgency.Core.Patterns.Flyweight;
using TravelAgency.Core.Patterns.Observer;
using TravelAgency.Core.Services;
using TravelAgency.Core.Validators;
using TravelAgency.WPF.Messaging.Messages;
using TravelAgency.WPF.Commands;
using TravelAgency.WPF.Services;
using TravelAgency.WPF.Views;
using TravelAgency.WPF.Views.Common;

namespace TravelAgency.WPF.ViewModels.ClientVM
{
    public class ClientViewModel : INotifyPropertyChanged, IBookingObserver
    {
        private TripPackage? _selectedPackage;
        private Booking? _selectedBooking;
        private bool _syncingBookingSelection;
        private double _basePrice;
        private double _totalPrice;
        private string _searchText = "";
        private Visibility _packagesVisibility = Visibility.Visible;
        private Visibility _bookingsVisibility = Visibility.Collapsed;
        private Visibility _favoritesVisibility = Visibility.Collapsed;
        private Visibility _profileVisibility = Visibility.Collapsed;
        private Visibility _notificationsVisibility = Visibility.Collapsed;
        private int _profileUserId;
        private string _profileUsername = "";
        private string _profileRoleDisplay = "";
        private string _profileEmail = "";
        private string _profilePhone = "";
        private string _profileStatusHint = "";
        private string _profileAccountSummaryLine = "";
        private int _profileBookingsCount;
        private int _profileFavoritesCount;
        private bool _profileIsBlocked;
        private readonly IBookingAccessService _bookingService;
        private readonly string _currentClientUsername;
        private readonly IUserRepository _userRepository;
        private readonly IUserMessageRepository _userMessages;
        private readonly BookingNotificationService _notificationService;
        private readonly IBookingRepository _bookingRepository;
        private readonly ITripPackageRepository _tripPackageRepository;
        private readonly IClientPackageFavoriteRepository _favoritesRepository;
        private readonly HashSet<int> _favoritePackageIds = new();

        /// <summary>All packages from DB; <see cref="Packages"/> is the filtered view.</summary>
        private readonly List<TripPackage> _allPackages = new();

        private string _filterDestination = "";
        private string? _selectedPriceRange = "Any";
        private string? _selectedTripTypeFilter = "Any";

        private string _appliedDestination = "";
        private string _appliedPriceRange = "Any";
        private string _appliedTripType = "Any";
        public Visibility PackagesVisibility
        {
            get => _packagesVisibility;
            set
            {
                if (_packagesVisibility != value)
                {
                    _packagesVisibility = value;
                    OnPropertyChanged(nameof(PackagesVisibility));
                    NotifyPackageBrowsePanelState();
                    NotifyClientMainChrome();
                }
            }
        }

        public Visibility BookingsVisibility
        {
            get => _bookingsVisibility;
            set
            {
                if (_bookingsVisibility != value)
                {
                    _bookingsVisibility = value;
                    OnPropertyChanged(nameof(BookingsVisibility));
                    NotifyClientMainChrome();
                }
            }
        }

        public Visibility FavoritesVisibility
        {
            get => _favoritesVisibility;
            set
            {
                if (_favoritesVisibility != value)
                {
                    _favoritesVisibility = value;
                    OnPropertyChanged(nameof(FavoritesVisibility));
                    NotifyClientMainChrome();
                }
            }
        }

        public Visibility ProfileVisibility
        {
            get => _profileVisibility;
            set
            {
                if (_profileVisibility != value)
                {
                    _profileVisibility = value;
                    OnPropertyChanged(nameof(ProfileVisibility));
                    NotifyClientMainChrome();
                    OnPropertyChanged(nameof(ShowProfileChrome));
                    OnPropertyChanged(nameof(ShowClientRightPanel));
                }
            }
        }

        public Visibility NotificationsVisibility
        {
            get => _notificationsVisibility;
            set
            {
                if (_notificationsVisibility != value)
                {
                    _notificationsVisibility = value;
                    OnPropertyChanged(nameof(NotificationsVisibility));
                    NotifyClientMainChrome();
                    OnPropertyChanged(nameof(ShowNotificationsChrome));
                    OnPropertyChanged(nameof(ShowClientRightPanel));
                }
            }
        }

        public string ProfileUsername
        {
            get => _profileUsername;
            private set
            {
                if (_profileUsername != value)
                {
                    _profileUsername = value ?? "";
                    OnPropertyChanged(nameof(ProfileUsername));
                }
            }
        }

        public string ProfileRoleDisplay
        {
            get => _profileRoleDisplay;
            private set
            {
                if (_profileRoleDisplay != value)
                {
                    _profileRoleDisplay = value ?? "";
                    OnPropertyChanged(nameof(ProfileRoleDisplay));
                }
            }
        }

        public string ProfileEmail
        {
            get => _profileEmail;
            set
            {
                if (_profileEmail != value)
                {
                    _profileEmail = value ?? "";
                    OnPropertyChanged(nameof(ProfileEmail));
                }
            }
        }

        public string ProfilePhone
        {
            get => _profilePhone;
            set
            {
                if (_profilePhone != value)
                {
                    _profilePhone = value ?? "";
                    OnPropertyChanged(nameof(ProfilePhone));
                }
            }
        }

        public string ProfileStatusHint
        {
            get => _profileStatusHint;
            private set
            {
                if (_profileStatusHint != value)
                {
                    _profileStatusHint = value ?? "";
                    OnPropertyChanged(nameof(ProfileStatusHint));
                }
            }
        }

        public bool ProfileIsBlocked
        {
            get => _profileIsBlocked;
            private set
            {
                if (_profileIsBlocked != value)
                {
                    _profileIsBlocked = value;
                    OnPropertyChanged(nameof(ProfileIsBlocked));
                }
            }
        }

        /// <summary>Text scurt sub badge-ul de stare (activ / blocat).</summary>
        public string ProfileAccountSummaryLine
        {
            get => _profileAccountSummaryLine;
            private set
            {
                if (_profileAccountSummaryLine != value)
                {
                    _profileAccountSummaryLine = value ?? "";
                    OnPropertyChanged(nameof(ProfileAccountSummaryLine));
                }
            }
        }

        public int ProfileBookingsCount
        {
            get => _profileBookingsCount;
            private set
            {
                if (_profileBookingsCount != value)
                {
                    _profileBookingsCount = value;
                    OnPropertyChanged(nameof(ProfileBookingsCount));
                }
            }
        }

        public int ProfileFavoritesCount
        {
            get => _profileFavoritesCount;
            private set
            {
                if (_profileFavoritesCount != value)
                {
                    _profileFavoritesCount = value;
                    OnPropertyChanged(nameof(ProfileFavoritesCount));
                }
            }
        }

        /// <summary>Antet + filtre pentru modul profil.</summary>
        public bool ShowProfileChrome => _profileVisibility == Visibility.Visible;

        public bool ShowNotificationsChrome => _notificationsVisibility == Visibility.Visible;

        /// <summary>Panoul din dreapta (detalii pachet / rezervare).</summary>
        public bool ShowClientRightPanel =>
            _profileVisibility != Visibility.Visible &&
            _notificationsVisibility != Visibility.Visible;

        public ICommand ShowBookingsCommand { get; set; }
        public ICommand ShowPackagesCommand { get; set; }
        public ICommand ShowFavoritesCommand { get; set; }
        public ICommand ShowProfileCommand { get; set; }
        public ICommand ShowNotificationsCommand { get; set; }
        public ICommand ClearBookingNotificationsCommand { get; set; }
        public ICommand OpenBookingFromNotificationCommand { get; set; }
        public ICommand SaveProfileCommand { get; set; }
        public ICommand TogglePackageFavoriteCommand { get; set; }
        public ObservableCollection<TripPackage> Packages { get; set; }
        public ObservableCollection<TripPackage> FavoritePackages { get; set; }
        public ObservableCollection<OptionalExtra> AvailableExtras { get; set; }

        public ObservableCollection<Booking> MyBookings { get; set; }

        public ObservableCollection<BookingUpdateNotification> BookingNotifications { get; } = new();

        public bool HasBookingNotifications => BookingNotifications.Count > 0;

        public bool HasNoBookingNotifications => BookingNotifications.Count == 0;

        public int UnreadBookingNotificationCount => BookingNotifications.Count(n => !n.IsRead);

        public bool HasBookings => MyBookings.Count > 0;

        public bool HasNoBookings => MyBookings.Count == 0;

        /// <summary>Rezervarea curentă în modul „Rezervările mele”; null în modul pachete sau fără selecție.</summary>
        public Booking? SelectedBooking
        {
            get => _selectedBooking;
            set
            {
                if (ReferenceEquals(_selectedBooking, value))
                    return;

                _selectedBooking = value;
                OnPropertyChanged(nameof(SelectedBooking));
                OnPropertyChanged(nameof(DetailsExtrasEditable));

                if (value != null && value.TripPackage != null)
                {
                    _syncingBookingSelection = true;
                    try
                    {
                        if (!ReferenceEquals(_selectedPackage, value.TripPackage))
                        {
                            _selectedPackage = value.TripPackage;
                            OnPropertyChanged(nameof(SelectedPackage));
                            NotifyPackageBrowsePanelState();
                        }
                    }
                    finally
                    {
                        _syncingBookingSelection = false;
                    }

                    ApplyBookingSnapshotToDetails(value);
                }
                else
                    UpdateSelectedPackageDetailsForCurrentContext();
            }
        }

        /// <summary>False când detaliile reflectă o rezervare — extra-urile nu mai recalculează totalul.</summary>
        public bool DetailsExtrasEditable => _selectedBooking == null;

        /// <summary>Suma extra-urilor bifate (pentru descompunerea prețului în panoul client).</summary>
        public double SelectedExtrasSubtotal => AvailableExtras.Where(x => x.IsSelected).Sum(x => x.Price);

        /// <summary>Afișează explicație când totalul stocat pe rezervare nu coincide cu baza + extra-uri (decoratori / istoric).</summary>
        public bool ShowBookingTotalDisclaimer =>
            _selectedBooking != null &&
            Math.Abs(TotalPrice - (BasePrice + SelectedExtrasSubtotal)) > 0.02;

        /// <summary>True dacă există cel puțin un extra bifat — pentru rândul „Subtotal extra-uri” în sumar.</summary>
        public bool HasSelectedExtrasForPriceBreakdown => SelectedExtrasSubtotal > 0.005;

        /// <summary>Panoul din dreapta are conținut pachet (browse sau pachet sincronizat din rezervare).</summary>
        public bool HasPackageDetails => SelectedPackage != null;

        /// <summary>Modul „pachete” fără pachet selectat — afișăm invitația de selecție.</summary>
        public bool ShowPackageBrowseEmptyState =>
            _packagesVisibility == Visibility.Visible &&
            _favoritesVisibility != Visibility.Visible &&
            _profileVisibility != Visibility.Visible &&
            _notificationsVisibility != Visibility.Visible &&
            _selectedPackage == null;

        /// <summary>Panoul din dreapta în modul Favorite, fără pachet selectat.</summary>
        public bool ShowFavoritesDetailsPlaceholder =>
            _favoritesVisibility == Visibility.Visible && _selectedPackage == null;

        /// <summary>Browse pachete (nu Favorite / nu Rezervări / nu Profil / nu Notificări).</summary>
        public bool IsBrowsePackagesSectionActive =>
            _packagesVisibility == Visibility.Visible &&
            _favoritesVisibility != Visibility.Visible &&
            _profileVisibility != Visibility.Visible &&
            _notificationsVisibility != Visibility.Visible;

        /// <summary>Flux pachet: Browse sau Favorite (fără Profil / fără Notificări).</summary>
        public bool ShowClientPackageFlowChrome =>
            (_packagesVisibility == Visibility.Visible || _favoritesVisibility == Visibility.Visible) &&
            _profileVisibility != Visibility.Visible &&
            _notificationsVisibility != Visibility.Visible;

        public bool ShowBackToPackagesHeaderButton =>
            _bookingsVisibility == Visibility.Visible ||
            _favoritesVisibility == Visibility.Visible ||
            _profileVisibility == Visibility.Visible ||
            _notificationsVisibility == Visibility.Visible;

        public bool HasFavoritePackages => FavoritePackages.Count > 0;

        public bool HasNoFavoritePackages => FavoritePackages.Count == 0;

        public int FavoriteCount => _favoritePackageIds.Count;

        public ObservableCollection<string> DestinationImageUrls { get; } = new();
        public ObservableCollection<string> HotelImageUrls { get; } = new();

        /// <summary>True when API returned at least one destination (city) image for the details strip.</summary>
        public bool HasDestinationGalleryImages => DestinationImageUrls.Count > 0;

        /// <summary>True when API returned at least one hotel thumbnail for the details strip.</summary>
        public bool HasHotelGalleryImages => HotelImageUrls.Count > 0;

        private const string ApiBaseUrl = "http://localhost:5280";
        private readonly HttpClient _apiHttp = new() { Timeout = TimeSpan.FromSeconds(25) };

        public ICommand ConfirmBookingCommand { get; set; }
        public ICommand ApplyFiltersCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand OpenAgencyMessagesCommand { get; }

        public ObservableCollection<string> PriceRangeOptions { get; } = new()
        {
            "Any",
            "Under €200",
            "€200 – €400",
            "€400 – €800",
            "Over €800"
        };

        public ObservableCollection<string> TripTypeFilterOptions { get; } = new();

        private int _agencyInboxUnread;
        public int AgencyInboxUnreadCount
        {
            get => _agencyInboxUnread;
            private set
            {
                if (_agencyInboxUnread != value)
                {
                    _agencyInboxUnread = value;
                    OnPropertyChanged(nameof(AgencyInboxUnreadCount));
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged(nameof(SearchText));
                    RebuildFilteredPackages();
                }
            }
        }

        public string FilterDestination
        {
            get => _filterDestination;
            set
            {
                if (_filterDestination != value)
                {
                    _filterDestination = value ?? "";
                    OnPropertyChanged(nameof(FilterDestination));
                }
            }
        }

        public string? SelectedPriceRange
        {
            get => _selectedPriceRange;
            set
            {
                if (_selectedPriceRange != value)
                {
                    _selectedPriceRange = string.IsNullOrWhiteSpace(value) ? "Any" : value;
                    OnPropertyChanged(nameof(SelectedPriceRange));
                }
            }
        }

        public string? SelectedTripTypeFilter
        {
            get => _selectedTripTypeFilter;
            set
            {
                if (_selectedTripTypeFilter != value)
                {
                    _selectedTripTypeFilter = string.IsNullOrWhiteSpace(value) ? "Any" : value;
                    OnPropertyChanged(nameof(SelectedTripTypeFilter));
                }
            }
        }

        public TripPackage? SelectedPackage
        {
            get => _selectedPackage;
            set
            {
                if (ReferenceEquals(_selectedPackage, value))
                    return;

                if (!_syncingBookingSelection)
                {
                    _selectedBooking = null;
                    OnPropertyChanged(nameof(SelectedBooking));
                    OnPropertyChanged(nameof(DetailsExtrasEditable));
                }

                _selectedPackage = value;
                OnPropertyChanged(nameof(SelectedPackage));
                NotifyPackageBrowsePanelState();
                UpdateSelectedPackageDetailsForCurrentContext();
            }
        }

        public double BasePrice
        {
            get => _basePrice;
            set
            {
                if (_basePrice != value)
                {
                    _basePrice = value;
                    OnPropertyChanged(nameof(BasePrice));
                }
            }
        }

        public double TotalPrice
        {
            get => _totalPrice;
            set
            {
                if (_totalPrice != value)
                {
                    _totalPrice = value;
                    OnPropertyChanged(nameof(TotalPrice));
                }
            }
        }

        public ClientViewModel(
            IBookingRepository bookingRepository,
            ITripPackageRepository tripPackageRepository,
            IUserRepository userRepository,
            IUserMessageRepository userMessages,
            IClientPackageFavoriteRepository favoritesRepository,
            BookingNotificationService notificationService,
            BookingAccessService bookingAccessService)
        {
            var currentUser = SessionManager.Instance.CurrentSession.CurrentUser
                ?? throw new InvalidOperationException("User not authenticated.");

            _bookingRepository = bookingRepository ?? throw new ArgumentNullException(nameof(bookingRepository));
            _tripPackageRepository = tripPackageRepository ?? throw new ArgumentNullException(nameof(tripPackageRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _userMessages = userMessages ?? throw new ArgumentNullException(nameof(userMessages));
            _favoritesRepository = favoritesRepository ?? throw new ArgumentNullException(nameof(favoritesRepository));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));

            _bookingService = new BookingAccessProxy(
                bookingAccessService ?? throw new ArgumentNullException(nameof(bookingAccessService)),
                currentUser);
            _notificationService.Attach(this);

            LogoutCommand = new RelayCommand(Logout);

            _currentClientUsername = currentUser.Username ?? "";
            if (SessionManager.Instance.CurrentSession.CurrentUser == null)
            {
                throw new InvalidOperationException("User not authenticated.");
            }


            if (string.IsNullOrWhiteSpace(_currentClientUsername))
            {
                throw new InvalidOperationException("No authenticated client session found.");
            }

            Packages = new ObservableCollection<TripPackage>();
            FavoritePackages = new ObservableCollection<TripPackage>();
            AvailableExtras = new ObservableCollection<OptionalExtra>();
            MyBookings = new ObservableCollection<Booking>();
            MyBookings.CollectionChanged += (_, __) =>
            {
                OnPropertyChanged(nameof(HasBookings));
                OnPropertyChanged(nameof(HasNoBookings));
            };

            DestinationImageUrls.CollectionChanged += (_, __) =>
                OnPropertyChanged(nameof(HasDestinationGalleryImages));
            HotelImageUrls.CollectionChanged += (_, __) =>
                OnPropertyChanged(nameof(HasHotelGalleryImages));

            ShowBookingsCommand = new RelayCommand(() => NavigateToBookings(null));
            ShowPackagesCommand = new RelayCommand(ShowPackages);
            ShowFavoritesCommand = new RelayCommand(ShowFavorites);
            ShowProfileCommand = new RelayCommand(ShowProfile);
            ShowNotificationsCommand = new RelayCommand(ShowNotifications);
            ClearBookingNotificationsCommand = new RelayCommand(ClearBookingNotifications);
            OpenBookingFromNotificationCommand = new RelayCommand<int?>(id => NavigateToBookings(id));
            SaveProfileCommand = new RelayCommand(SaveProfile);
            TogglePackageFavoriteCommand = new RelayCommand<TripPackage>(TogglePackageFavorite);
            ConfirmBookingCommand = new RelayCommand(ConfirmBooking);
            ApplyFiltersCommand = new RelayCommand(ApplyFilters);
            ClearFiltersCommand = new RelayCommand(ClearFilters);
            OpenAgencyMessagesCommand = new RelayCommand(OpenAgencyMessages);

            LoadFromDatabase();
            LoadMyBookings();
            RefreshAgencyInboxUnread();
        }

        private string ResolveDefaultAgentUsername()
        {
            var agent = _userRepository.GetAll()
                .FirstOrDefault(u => string.Equals(u.Role?.Name, "Agent", StringComparison.OrdinalIgnoreCase));
            return string.IsNullOrWhiteSpace(agent?.Username) ? "agent1" : agent.Username;
        }

        private void RefreshAgencyInboxUnread()
        {
            try
            {
                AgencyInboxUnreadCount = _userMessages.GetUnreadCount(_currentClientUsername);
            }
            catch
            {
                AgencyInboxUnreadCount = 0;
            }
        }

        private void ShowNotifications()
        {
            ProfileVisibility = Visibility.Collapsed;
            BookingsVisibility = Visibility.Collapsed;
            PackagesVisibility = Visibility.Collapsed;
            FavoritesVisibility = Visibility.Collapsed;
            NotificationsVisibility = Visibility.Visible;
            SelectedPackage = null;

            foreach (var n in BookingNotifications)
                n.IsRead = true;

            OnPropertyChanged(nameof(UnreadBookingNotificationCount));
        }

        private void ClearBookingNotifications()
        {
            BookingNotifications.Clear();
            OnPropertyChanged(nameof(UnreadBookingNotificationCount));
            OnPropertyChanged(nameof(HasBookingNotifications));
            OnPropertyChanged(nameof(HasNoBookingNotifications));
        }

        private void NavigateToBookings(int? selectBookingId)
        {
            ProfileVisibility = Visibility.Collapsed;
            NotificationsVisibility = Visibility.Collapsed;
            FavoritesVisibility = Visibility.Collapsed;
            PackagesVisibility = Visibility.Collapsed;
            BookingsVisibility = Visibility.Visible;

            Booking? pick = null;
            if (selectBookingId.HasValue)
                pick = MyBookings.FirstOrDefault(b => b.Id == selectBookingId.Value);

            SelectedBooking = pick ?? MyBookings.FirstOrDefault();
        }

        private void OpenAgencyMessages()
        {
            var agentUsername = ResolveDefaultAgentUsername();
            var win = new UserConversationWindow(
                _userMessages,
                _currentClientUsername,
                agentUsername,
                $"Mesaje — agenție ({agentUsername})");
            win.SetOwnerSafe();
            win.ShowDialog();
            RefreshAgencyInboxUnread();
        }

        private void ShowPackages()
        {
            ProfileVisibility = Visibility.Collapsed;
            NotificationsVisibility = Visibility.Collapsed;
            FavoritesVisibility = Visibility.Collapsed;
            PackagesVisibility = Visibility.Visible;
            BookingsVisibility = Visibility.Collapsed;
            // Detalii goale până alegi un pachet din listă (setter-ul golește și rezervarea curentă).
            SelectedPackage = null;
        }

        private void ShowFavorites()
        {
            ProfileVisibility = Visibility.Collapsed;
            NotificationsVisibility = Visibility.Collapsed;
            BookingsVisibility = Visibility.Collapsed;
            PackagesVisibility = Visibility.Collapsed;
            FavoritesVisibility = Visibility.Visible;
            SelectedPackage = null;
            ReloadFavoriteIdsFromDb();
        }

        private void ShowProfile()
        {
            BookingsVisibility = Visibility.Collapsed;
            PackagesVisibility = Visibility.Collapsed;
            FavoritesVisibility = Visibility.Collapsed;
            NotificationsVisibility = Visibility.Collapsed;
            ProfileVisibility = Visibility.Visible;
            SelectedPackage = null;
            LoadProfileFromDb();
        }

        private void LoadProfileFromDb()
        {
            try
            {
                RefreshAgencyInboxUnread();

                var user = _userRepository.GetByUsername(_currentClientUsername);
                if (user == null)
                {
                    ProfileUsername = _currentClientUsername;
                    ProfileRoleDisplay = "";
                    ProfileEmail = "";
                    ProfilePhone = "";
                    ProfileIsBlocked = false;
                    ProfileStatusHint = "Nu s-au putut încărca datele contului.";
                    ProfileAccountSummaryLine = "Reîncearcă sau contactează suportul agenției.";
                    ProfileBookingsCount = 0;
                    ProfileFavoritesCount = 0;
                    return;
                }

                _profileUserId = user.Id;
                ProfileUsername = user.Username ?? "";
                ProfileRoleDisplay = (user.Role?.Name ?? "").Trim();
                ProfileEmail = user.Email ?? "";
                ProfilePhone = user.PhoneNumber ?? "";
                ProfileIsBlocked = user.IsBlocked;
                ProfileStatusHint = user.IsBlocked
                    ? "Cont blocat — nu poți plasa rezervări noi. Contactează agenția."
                    : "";
                ProfileAccountSummaryLine = user.IsBlocked
                    ? "Acces limitat: nu poți salva modificări sau rezerva până la deblocare."
                    : "Cont activ — editează contactul mai jos. Parola se schimbă doar prin „Am uitat parola?” la autentificare (nu din acest ecran).";

                try
                {
                    ProfileBookingsCount = _bookingRepository.GetByClientUsername(_currentClientUsername).Count;
                }
                catch
                {
                    ProfileBookingsCount = MyBookings.Count;
                }

                try
                {
                    ProfileFavoritesCount = _favoritesRepository.GetFavoriteTripPackageIds(_currentClientUsername).Count;
                }
                catch
                {
                    ProfileFavoritesCount = FavoriteCount;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Profil",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void SaveProfile()
        {
            if (ProfileIsBlocked)
            {
                MessageBox.Show(
                    "Contul este blocat. Modificările nu pot fi salvate.",
                    "Profil",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            try
            {
                var email = (ProfileEmail ?? "").Trim();
                var phone = (ProfilePhone ?? "").Trim();

                if (email.Length > 0)
                {
                    var at = email.IndexOf('@', StringComparison.Ordinal);
                    if (at <= 0 || at == email.Length - 1 || email.IndexOf('@', at + 1) >= 0)
                    {
                        MessageBox.Show(
                            "Adresa de e-mail nu pare validă.",
                            "Profil",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }
                }

                var user = _userRepository.GetById(_profileUserId);
                if (user == null)
                {
                    MessageBox.Show(
                        "Contul nu a fost găsit.",
                        "Profil",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                if (email.Length > 0)
                {
                    var other = _userRepository.GetByEmail(email);
                    if (other != null && other.Id != user.Id)
                    {
                        MessageBox.Show(
                            "Acest e-mail este deja folosit de alt cont.",
                            "Profil",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }
                }

                user.Email = email.Length == 0 ? null : email;
                user.PhoneNumber = phone.Length == 0 ? null : phone;
                _userRepository.Update(user);

                var sessionUser = SessionManager.Instance.CurrentSession.CurrentUser;
                if (sessionUser != null && sessionUser.Id == user.Id)
                {
                    sessionUser.Email = user.Email;
                    sessionUser.PhoneNumber = user.PhoneNumber;
                }

                LoadProfileFromDb();
                MessageBox.Show(
                    "Profilul a fost salvat.",
                    "Profil",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Profil",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void ReloadFavoriteIdsFromDb()
        {
            try
            {
                _favoritePackageIds.Clear();
                foreach (var id in _favoritesRepository.GetFavoriteTripPackageIds(_currentClientUsername))
                    _favoritePackageIds.Add(id);

                SyncFavoriteFlagsOnAllPackages();
                RebuildFavoritePackagesCollection();
                OnPropertyChanged(nameof(FavoriteCount));
                OnPropertyChanged(nameof(HasFavoritePackages));
                OnPropertyChanged(nameof(HasNoFavoritePackages));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Nu s-au putut încărca favoritele: {ex.Message}",
                    "Favorite",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void SyncFavoriteFlagsOnAllPackages()
        {
            foreach (var p in _allPackages)
                p.IsFavorite = _favoritePackageIds.Contains(p.Id);
        }

        private void RebuildFavoritePackagesCollection()
        {
            FavoritePackages.Clear();
            foreach (var id in _favoritesRepository.GetFavoriteTripPackageIds(_currentClientUsername))
            {
                var pkg = _allPackages.FirstOrDefault(p => p.Id == id);
                if (pkg != null)
                    FavoritePackages.Add(pkg);
            }
        }

        private void TogglePackageFavorite(TripPackage? package)
        {
            if (package == null || package.Id <= 0)
                return;

            try
            {
                if (_favoritePackageIds.Contains(package.Id))
                {
                    _favoritesRepository.Remove(_currentClientUsername, package.Id);
                    _favoritePackageIds.Remove(package.Id);
                }
                else
                {
                    _favoritesRepository.Add(_currentClientUsername, package.Id);
                    _favoritePackageIds.Add(package.Id);
                }

                package.IsFavorite = _favoritePackageIds.Contains(package.Id);
                RebuildFavoritePackagesCollection();
                OnPropertyChanged(nameof(FavoriteCount));
                OnPropertyChanged(nameof(HasFavoritePackages));
                OnPropertyChanged(nameof(HasNoFavoritePackages));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Favorite",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void LoadFromDatabase()
        {
            _allPackages.Clear();
            Packages.Clear();

            try
            {
                var trips = _tripPackageRepository.GetAll();
                _allPackages.AddRange(trips);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Database error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            ReloadFavoriteIdsFromDb();

            RebuildTripTypeFilterOptions();
            _appliedDestination = (FilterDestination ?? "").Trim();
            _appliedPriceRange = string.IsNullOrWhiteSpace(SelectedPriceRange) ? "Any" : SelectedPriceRange!;
            _appliedTripType = string.IsNullOrWhiteSpace(SelectedTripTypeFilter) ? "Any" : SelectedTripTypeFilter!;
            RebuildFilteredPackages();

            if (AvailableExtras.Count == 0)
            {
                AvailableExtras.Add(new OptionalExtra { Name = "Airport Transfer", Price = 30 });
                AvailableExtras.Add(new OptionalExtra { Name = "Insurance", Price = 20 });
                AvailableExtras.Add(new OptionalExtra { Name = "Free Cancellation", Price = 25 });
                AvailableExtras.Add(new OptionalExtra { Name = "Guided Tour", Price = 40 });

                foreach (var extra in AvailableExtras)
                {
                    extra.PropertyChanged += Extra_PropertyChanged;
                }
            }
        }

        private void RebuildTripTypeFilterOptions()
        {
            var previous = (_selectedTripTypeFilter ?? "Any").Trim();
            TripTypeFilterOptions.Clear();
            TripTypeFilterOptions.Add("Any");

            var types = _allPackages
                .SelectMany(p => new[] { p.TripType, p.Category })
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var s in types)
                TripTypeFilterOptions.Add(s);

            var next = TripTypeFilterOptions.Any(x => string.Equals(x, previous, StringComparison.OrdinalIgnoreCase))
                ? TripTypeFilterOptions.First(x => string.Equals(x, previous, StringComparison.OrdinalIgnoreCase))
                : "Any";

            _selectedTripTypeFilter = next;
            OnPropertyChanged(nameof(SelectedTripTypeFilter));
        }

        private void ApplyFilters()
        {
            _appliedDestination = (FilterDestination ?? "").Trim();
            _appliedPriceRange = string.IsNullOrWhiteSpace(SelectedPriceRange) ? "Any" : SelectedPriceRange!.Trim();
            _appliedTripType = string.IsNullOrWhiteSpace(SelectedTripTypeFilter) ? "Any" : SelectedTripTypeFilter!.Trim();
            RebuildFilteredPackages();
        }

        private void ClearFilters()
        {
            _appliedDestination = "";
            _appliedPriceRange = "Any";
            _appliedTripType = "Any";

            _filterDestination = "";
            OnPropertyChanged(nameof(FilterDestination));

            _selectedPriceRange = "Any";
            OnPropertyChanged(nameof(SelectedPriceRange));

            _selectedTripTypeFilter = "Any";
            OnPropertyChanged(nameof(SelectedTripTypeFilter));

            if (!string.IsNullOrEmpty(_searchText))
            {
                _searchText = "";
                OnPropertyChanged(nameof(SearchText));
            }

            RebuildFilteredPackages();
        }

        private void RebuildFilteredPackages()
        {
            var list = GetFilteredTrips().ToList();
            Packages.Clear();
            foreach (var trip in list)
                Packages.Add(trip);

            FixSelectionAfterFilter();
        }

        private void FixSelectionAfterFilter()
        {
            var selId = SelectedPackage?.Id ?? 0;
            if (selId > 0 && Packages.Any(p => p.Id == selId))
                return;

            SelectedPackage = null;
        }

        private IEnumerable<TripPackage> GetFilteredTrips()
        {
            IEnumerable<TripPackage> q = _allPackages;

            var search = (SearchText ?? "").Trim();
            if (search.Length > 0)
            {
                q = q.Where(t =>
                    (t.Name ?? "").Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (t.Destination ?? "").Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (t.Country ?? "").Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (t.ShortDescription ?? "").Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            if (_appliedDestination.Length > 0)
            {
                q = q.Where(t =>
                    (t.Destination ?? "").Contains(_appliedDestination, StringComparison.OrdinalIgnoreCase) ||
                    (t.Country ?? "").Contains(_appliedDestination, StringComparison.OrdinalIgnoreCase) ||
                    (t.Name ?? "").Contains(_appliedDestination, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.Equals(_appliedPriceRange, "Any", StringComparison.OrdinalIgnoreCase))
            {
                q = _appliedPriceRange switch
                {
                    "Under €200" => q.Where(t => t.Price < 200),
                    "€200 – €400" => q.Where(t => t.Price >= 200 && t.Price < 400),
                    "€400 – €800" => q.Where(t => t.Price >= 400 && t.Price < 800),
                    "Over €800" => q.Where(t => t.Price >= 800),
                    _ => q
                };
            }

            if (!string.Equals(_appliedTripType, "Any", StringComparison.OrdinalIgnoreCase))
            {
                q = q.Where(t =>
                    string.Equals((t.TripType ?? "").Trim(), _appliedTripType, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals((t.Category ?? "").Trim(), _appliedTripType, StringComparison.OrdinalIgnoreCase));
            }

            return q;
        }

        private void Extra_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OptionalExtra.IsSelected))
            {
                if (_selectedBooking != null)
                    return;
                RecalculateTotalPrice();
            }
        }

        private void UpdateSelectedPackageDetailsForCurrentContext()
        {
            if (SelectedPackage == null)
            {
                ClearPackageDetailsUi();
                return;
            }

            if (_selectedBooking != null &&
                ReferenceEquals(_selectedBooking.TripPackage, SelectedPackage))
            {
                ApplyBookingSnapshotToDetails(_selectedBooking);
                return;
            }

            BasePrice = SelectedPackage.Price;

            foreach (var extra in AvailableExtras)
                extra.IsSelected = false;

            RecalculateTotalPrice();
            _ = LoadDetailsImagesAsync(SelectedPackage);
        }

        private void ApplyBookingSnapshotToDetails(Booking booking)
        {
            BasePrice = booking.BasePrice;
            TotalPrice = booking.TotalPrice;

            foreach (var extra in AvailableExtras)
            {
                extra.IsSelected = booking.SelectedExtras.Any(se =>
                    string.Equals(se, extra.Name, StringComparison.OrdinalIgnoreCase));
            }

            if (booking.TripPackage != null)
                _ = LoadDetailsImagesAsync(booking.TripPackage);

            RefreshPriceBreakdownUi();
        }

        private async Task LoadDetailsImagesAsync(TripPackage pkg)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                DestinationImageUrls.Clear();
                HotelImageUrls.Clear();
            });

            try
            {
                var city = (pkg.Destination ?? "").Trim();
                var country = (pkg.Country ?? "").Trim();

                if (city.Length == 0)
                    return;

                // Destination images
                var imgUrl =
                    $"{ApiBaseUrl}/api/destinations/images" +
                    $"?city={Uri.EscapeDataString(city)}" +
                    (country.Length == 0 ? "" : $"&country={Uri.EscapeDataString(country)}") +
                    $"&limit=6";

                using var imgResp = await _apiHttp.GetAsync(imgUrl);
                if (imgResp.IsSuccessStatusCode)
                {
                    var json = await imgResp.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("images", out var images) &&
                        images.ValueKind == JsonValueKind.Array)
                    {
                        var list = images.EnumerateArray()
                            .Select(x =>
                            {
                                // Preferă URL-ul principal (ex. Unsplash „regular” ~1080px), nu thumbUrl („small” ~400px),
                                // altfel preview-ul și zoom-ul arată pixelat.
                                var full = (x.TryGetProperty("url", out var u) ? (u.GetString() ?? "") : "").Trim();
                                if (full.Length > 0)
                                    return full;
                                return (x.TryGetProperty("thumbUrl", out var t) ? (t.GetString() ?? "") : "").Trim();
                            })
                            .Where(s => s.Length > 0)
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .Take(6)
                            .ToList();

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            foreach (var u in list) DestinationImageUrls.Add(u);
                        });
                    }
                }

                // Hotel thumbnails (best effort): use package season as dates.
                var checkIn = pkg.Season?.StartDate.Date ?? DateTime.Today.AddDays(14);
                var checkOut = pkg.Season?.EndDate.Date ?? checkIn.AddDays(5);
                if (checkOut <= checkIn) checkOut = checkIn.AddDays(3);

                var hotelsUrl =
                    $"{ApiBaseUrl}/api/destinations/hotels" +
                    $"?city={Uri.EscapeDataString(city)}" +
                    (country.Length == 0 ? "" : $"&country={Uri.EscapeDataString(country)}") +
                    $"&checkIn={Uri.EscapeDataString(checkIn.ToString("yyyy-MM-dd"))}" +
                    $"&checkOut={Uri.EscapeDataString(checkOut.ToString("yyyy-MM-dd"))}" +
                    $"&adults=2&limit=6";

                using var hResp = await _apiHttp.GetAsync(hotelsUrl);
                if (hResp.IsSuccessStatusCode)
                {
                    var json = await hResp.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("hotels", out var hotels) &&
                        hotels.ValueKind == JsonValueKind.Array)
                    {
                        var list = hotels.EnumerateArray()
                            .Select(x =>
                                (x.TryGetProperty("thumbnailUrl", out var t) ? (t.GetString() ?? "") : "").Trim())
                            .Where(s => s.Length > 0)
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .Take(6)
                            .ToList();

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            foreach (var u in list) HotelImageUrls.Add(u);
                        });
                    }
                }
            }
            catch
            {
                // ignore (API may be down)
            }
        }

        private void RecalculateTotalPrice()
        {
            double extrasTotal = AvailableExtras
                .Where(x => x.IsSelected)
                .Sum(x => x.Price);

            TotalPrice = BasePrice + extrasTotal;
            RefreshPriceBreakdownUi();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void RefreshPriceBreakdownUi()
        {
            OnPropertyChanged(nameof(SelectedExtrasSubtotal));
            OnPropertyChanged(nameof(ShowBookingTotalDisclaimer));
            OnPropertyChanged(nameof(HasSelectedExtrasForPriceBreakdown));
        }

        private void NotifyPackageBrowsePanelState()
        {
            OnPropertyChanged(nameof(HasPackageDetails));
            OnPropertyChanged(nameof(ShowPackageBrowseEmptyState));
            OnPropertyChanged(nameof(ShowFavoritesDetailsPlaceholder));
        }

        private void NotifyClientMainChrome()
        {
            OnPropertyChanged(nameof(IsBrowsePackagesSectionActive));
            OnPropertyChanged(nameof(ShowClientPackageFlowChrome));
            OnPropertyChanged(nameof(ShowBackToPackagesHeaderButton));
            OnPropertyChanged(nameof(ShowPackageBrowseEmptyState));
            OnPropertyChanged(nameof(ShowFavoritesDetailsPlaceholder));
            OnPropertyChanged(nameof(ShowProfileChrome));
            OnPropertyChanged(nameof(ShowNotificationsChrome));
            OnPropertyChanged(nameof(ShowClientRightPanel));
        }

        private void ClearPackageDetailsUi()
        {
            BasePrice = 0;
            TotalPrice = 0;
            foreach (var extra in AvailableExtras)
                extra.IsSelected = false;

            if (Application.Current?.Dispatcher != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    DestinationImageUrls.Clear();
                    HotelImageUrls.Clear();
                });
            }
            else
            {
                DestinationImageUrls.Clear();
                HotelImageUrls.Clear();
            }

            RefreshPriceBreakdownUi();
        }
        private void ConfirmBooking()
        {
            if (SelectedPackage == null)
            {
                MessageBox.Show("Please select a package first.",
                                "Booking",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            var selectedExtras = AvailableExtras
                .Where(x => x.IsSelected)
                .Select(x => x.Name)
                .ToList();

            ITripComponent decoratedTrip = new BaseTrip(SelectedPackage);

            foreach (var extra in AvailableExtras.Where(x => x.IsSelected))
            {
                switch (extra.Name)
                {
                    case "Airport Transfer":
                        decoratedTrip = new AirportTransferDecorator(decoratedTrip);
                        break;

                    case "Insurance":
                        decoratedTrip = new InsuranceDecorator(decoratedTrip);
                        break;

                    case "Free Cancellation":
                        decoratedTrip = new FreeCancellationDecorator(decoratedTrip);
                        break;

                    case "Guided Tour":
                        decoratedTrip = new GuidedTourDecorator(decoratedTrip);
                        break;
                }
            }

            var finalPrice = decoratedTrip.GetPrice();

            var booking = new Booking
            {
                BookingDate = DateTime.UtcNow,
                Client = new Client
                {
                    Username = _currentClientUsername
                },
                TripPackage = SelectedPackage,
                SelectedExtras = selectedExtras,
                BasePrice = SelectedPackage.Price,
                TotalPrice = finalPrice
            };

            booking.SubmitRequest();

            try
            {
                var validator = new BookingValidator();
                validator.ValidateAndThrow(booking);

                _bookingService.SubmitBooking(booking);
              

                MessageBox.Show(
                    $"Request submitted successfully!\n\nTrip: {SelectedPackage.Name}\nTotal (EUR): {finalPrice:N2} €\nStatus: Pending",
                    "Booking Request",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                foreach (var extra in AvailableExtras)
                {
                    extra.IsSelected = false;
                }

                RecalculateTotalPrice();
                LoadMyBookings();
                NavigateToBookings(null);
            }
            catch (ValidationException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Booking validation error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Booking unavailable",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void LoadMyBookings()
        {
            MyBookings.Clear();
            var bookings = _bookingService.GetBookingsForCurrentUser();
            HydrateTripPackagesForBookings(bookings);
            foreach (var booking in bookings)
            {
                MyBookings.Add(booking);
            }
        }

        private void HydrateTripPackagesForBookings(IEnumerable<Booking> bookings)
        {
            foreach (var booking in bookings)
            {
                var id = booking.TripPackage?.Id ?? 0;
                if (id <= 0)
                    continue;

                var full = _tripPackageRepository.GetById(id);
                if (full != null)
                    booking.TripPackage = full;
            }
        }
        private void AppendBookingNotification(BookingStatusChangedEvent e)
        {
            var item = BookingUpdateNotificationFactory.TryCreate(e);
            if (item == null)
                return;

            item.IsRead = _notificationsVisibility == Visibility.Visible;

            BookingNotifications.Insert(0, item);

            OnPropertyChanged(nameof(HasBookingNotifications));
            OnPropertyChanged(nameof(HasNoBookingNotifications));
            OnPropertyChanged(nameof(UnreadBookingNotificationCount));
        }

        public void Cleanup()
        {
            _notificationService.Detach(this);
        }
        public void Update(BookingStatusChangedEvent bookingEvent)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // filtrăm DOAR booking-urile clientului curent
                if (bookingEvent.Booking.Client?.Username != _currentClientUsername)
                    return;

                // găsim booking-ul în listă
                var existing = MyBookings
                    .FirstOrDefault(b => b.Id == bookingEvent.Booking.Id);

                if (existing != null)
                {
                    if (ReferenceEquals(SelectedBooking, existing))
                        SelectedBooking = null;
                    MyBookings.Remove(existing);
                }

                HydrateTripPackagesForBookings(new[] { bookingEvent.Booking });
                // adăugăm versiunea nouă (cu status updated)
                MyBookings.Insert(0, bookingEvent.Booking);

                var keepId = SelectedBooking?.Id;
                if (keepId.HasValue && keepId.Value == bookingEvent.Booking.Id)
                    SelectedBooking = bookingEvent.Booking;

                AppendBookingNotification(bookingEvent);
            });
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
                .FirstOrDefault(w => w is Views.ClientWindow);

            if (window != null)
                App.Mediator.Publish(new LogoutRequestedMessage(window));
        }
    }
}