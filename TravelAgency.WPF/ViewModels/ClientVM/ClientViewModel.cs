using FluentValidation;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using TravelAgency.Core.Data.Repositories;
using TravelAgency.Core.Models;
using TravelAgency.Core.Models.Booking;
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
        private double _basePrice;
        private double _totalPrice;
        private string _searchText = "";
        private Visibility _packagesVisibility = Visibility.Visible;
        private Visibility _bookingsVisibility = Visibility.Collapsed;
        private readonly IBookingAccessService _bookingService;
        private readonly string _currentClientUsername;
        private readonly IUserRepository _userRepository;
        private readonly IUserMessageRepository _userMessages;
        private readonly BookingNotificationService _notificationService;
        private readonly IBookingRepository _bookingRepository;
        private readonly ITripPackageRepository _tripPackageRepository;
        public Visibility PackagesVisibility
        {
            get => _packagesVisibility;
            set
            {
                if (_packagesVisibility != value)
                {
                    _packagesVisibility = value;
                    OnPropertyChanged(nameof(PackagesVisibility));
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
                }
            }
        }

        public ICommand ShowBookingsCommand { get; set; }
        public ICommand ShowPackagesCommand { get; set; }
        public ObservableCollection<TripPackage> Packages { get; set; }
        public ObservableCollection<OptionalExtra> AvailableExtras { get; set; }

        public ObservableCollection<Booking> MyBookings { get; set; }

        public ObservableCollection<string> DestinationImageUrls { get; } = new();
        public ObservableCollection<string> HotelImageUrls { get; } = new();

        private const string ApiBaseUrl = "http://localhost:5280";
        private readonly HttpClient _apiHttp = new() { Timeout = TimeSpan.FromSeconds(25) };

        public ICommand ConfirmBookingCommand { get; set; }
        public ICommand LogoutCommand { get; }
        public ICommand OpenAgencyMessagesCommand { get; }

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
                }
            }
        }

        public TripPackage? SelectedPackage
        {
            get => _selectedPackage;
            set
            {
                if (_selectedPackage != value)
                {
                    _selectedPackage = value;
                    OnPropertyChanged(nameof(SelectedPackage));
                    UpdateSelectedPackageDetails();
                }
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
            BookingNotificationService notificationService,
            BookingAccessService bookingAccessService)
        {
            var currentUser = SessionManager.Instance.CurrentSession.CurrentUser
                ?? throw new InvalidOperationException("User not authenticated.");

            _bookingRepository = bookingRepository ?? throw new ArgumentNullException(nameof(bookingRepository));
            _tripPackageRepository = tripPackageRepository ?? throw new ArgumentNullException(nameof(tripPackageRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _userMessages = userMessages ?? throw new ArgumentNullException(nameof(userMessages));
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
            AvailableExtras = new ObservableCollection<OptionalExtra>();
            MyBookings = new ObservableCollection<Booking>();

            ShowBookingsCommand = new RelayCommand(ShowBookings);
            ShowPackagesCommand = new RelayCommand(ShowPackages);
            ConfirmBookingCommand = new RelayCommand(ConfirmBooking);
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
        private void ShowBookings()
        {
            PackagesVisibility = Visibility.Collapsed;
            BookingsVisibility = Visibility.Visible;
        }

        private void ShowPackages()
        {
            PackagesVisibility = Visibility.Visible;
            BookingsVisibility = Visibility.Collapsed;
        }

        private void LoadFromDatabase()
        {
            Packages.Clear();

            try
            {
                var trips = _tripPackageRepository.GetAll();

                foreach (var trip in trips)
                {
                    Packages.Add(trip);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Database error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

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

            SelectedPackage = Packages.FirstOrDefault(p => p.AvailableSeats > 0) ?? Packages.FirstOrDefault();
        }

        private void Extra_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OptionalExtra.IsSelected))
            {
                RecalculateTotalPrice();
            }
        }

        private void UpdateSelectedPackageDetails()
        {
            if (SelectedPackage == null)
                return;

            BasePrice = SelectedPackage.Price;

            foreach (var extra in AvailableExtras)
            {
                extra.IsSelected = false;
            }

            RecalculateTotalPrice();

            // Async: load destination + hotel images for details panel.
            _ = LoadDetailsImagesAsync(SelectedPackage);
        }

        private async Task LoadDetailsImagesAsync(TripPackage pkg)
        {
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
                                var thumb = (x.TryGetProperty("thumbUrl", out var t) ? (t.GetString() ?? "") : "").Trim();
                                if (thumb.Length > 0) return thumb;
                                return (x.TryGetProperty("url", out var u) ? (u.GetString() ?? "") : "").Trim();
                            })
                            .Where(s => s.Length > 0)
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .Take(6)
                            .ToList();

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            DestinationImageUrls.Clear();
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
                            HotelImageUrls.Clear();
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
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
                    $"Request submitted successfully!\n\nTrip: {SelectedPackage.Name}\nTotal: € {finalPrice:F2}\nStatus: Pending",
                    "Booking Request",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                foreach (var extra in AvailableExtras)
                {
                    extra.IsSelected = false;
                }

                RecalculateTotalPrice();
                ShowBookings();
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
                    // înlocuim booking-ul vechi
                    MyBookings.Remove(existing);
                }

                HydrateTripPackagesForBookings(new[] { bookingEvent.Booking });
                // adăugăm versiunea nouă (cu status updated)
                MyBookings.Insert(0, bookingEvent.Booking);
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