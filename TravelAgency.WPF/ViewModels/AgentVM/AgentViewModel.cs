using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using TravelAgency.Core.Data;
using TravelAgency.Core.Data.Repositories;
using TravelAgency.Core.Models;
using TravelAgency.Core.Models.Booking;
using TravelAgency.Core.Models.Users;
using TravelAgency.Core.Models.TripPkg.Package;
using TravelAgency.Core.Patterns.ChainOfResponsibility;
using TravelAgency.Core.Patterns.Iterator;
using TravelAgency.Core.Patterns.Observer;
using TravelAgency.Core.Services;
using TravelAgency.WPF.Messaging.Messages;
using TravelAgency.WPF.Commands;
using TravelAgency.WPF.Services;
using TravelAgency.WPF.Services.Navigation;
using TravelAgency.WPF.Views.Common;

namespace TravelAgency.WPF.ViewModels.AgentVM
{
    public class AgentNotificationItem
    {
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string AccentColor { get; set; } = "#6366F1";
    }

    public class AgentViewModel : ViewModelBase, IBookingObserver
    {
        private readonly BookingNotificationService _notificationService;
        private readonly BookingService _realBookingService;
        private readonly ITripPackageRepository _repo;
        private readonly TripCreationService _tripCreationService;
        private readonly IBookingAccessService _bookingService;
        private readonly AgentReportService _reportService = new();
        private Booking? _selectedBooking;
        private string _currentBookingFilter = "All";

        /// <summary>Filtru activ pe pagina Rezervări (pentru highlight pe chip-uri).</summary>
        public string ActiveBookingsFilter => _currentBookingFilter;

        private void SetActiveBookingsFilter(string filter)
        {
            if (string.Equals(_currentBookingFilter, filter, StringComparison.Ordinal))
                return;
            _currentBookingFilter = filter;
            OnPropertyChanged(nameof(ActiveBookingsFilter));
        }

        private readonly RelayCommand _approveBookingCommand;
        private readonly RelayCommand _rejectBookingCommand;
        private readonly IUserRepository _userRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly IBookingApprovalHandler _bookingApprovalChain;
        private readonly INavigationService _navigation;
        private readonly IUserMessageRepository _userMessages;
        private int _inboxUnreadCount;

        public ObservableCollection<TripPackage> Trips { get; } = new();
        public ObservableCollection<Booking> PendingBookings { get; set; }
        public ObservableCollection<Booking> AllBookings { get; } = new();
        public ObservableCollection<Booking> ReportPreviewBookings { get; } = new();

        // Dashboard data
        public ObservableCollection<Booking> RecentBookings { get; } = new();
        public ObservableCollection<TravelAgency.Core.Models.Users.User> RecentClients { get; } = new();

        /// <summary>Numărul de clienți afișați în listă (pentru antet / badge).</summary>
        public int RecentClientsCount => RecentClients.Count;

        public ObservableCollection<AgentNotificationItem> Notifications { get; } = new();

        // Dashboard "Recent bookings" table (tab-filtered)
        public ObservableCollection<Booking> DashboardBookings { get; } = new();

        private string _dashboardBookingFilter = "All";
        public string DashboardBookingFilter
        {
            get => _dashboardBookingFilter;
            set
            {
                if (Set(ref _dashboardBookingFilter, value))
                    RefreshDashboardBookings();
            }
        }

        public ICommand SetDashboardBookingFilterCommand { get; }

        private string _clientSearchText = "";
        public string ClientSearchText
        {
            get => _clientSearchText;
            set
            {
                if (Set(ref _clientSearchText, value))
                    LoadRecentClients();
            }
        }

        private ObservableCollection<Booking> _agentBookings = new();
        public ObservableCollection<Booking> AgentBookings
        {
            get => _agentBookings;
            set => Set(ref _agentBookings, value);
        }

        private string _selectedReportType = "Toate rezervările";
        public string SelectedReportType
        {
            get => _selectedReportType;
            set
            {
                if (Set(ref _selectedReportType, value))
                {
                    RefreshReportPreview();
                }
            }
        }

        private string _selectedExportFormat = "PDF";
        public string SelectedExportFormat
        {
            get => _selectedExportFormat;
            set => Set(ref _selectedExportFormat, value);
        }

        public ICollectionView TripsView
        {
            get => _tripsView;
            private set => Set(ref _tripsView, value);
        }
        private ICollectionView _tripsView = null!;
        public int TotalPackagesCount => Trips.Count;

        public int ActiveOffersCount => Trips.Count;

        public string AveragePriceText =>
            Trips.Count == 0 ? "0.00" : Trips.Average(t => t.Price).ToString("F2");

        public string TotalValueText =>
            Trips.Sum(t => t.Price).ToString("F2");


        private TripPackage? _selectedTrip;
        public TripPackage? SelectedTrip
        {
            get => _selectedTrip;
            set
            {
                if (Set(ref _selectedTrip, value))
                {
                    ((RelayCommand)CloneCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)UpdateCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)DeleteCommand).RaiseCanExecuteChanged();

                    if (value != null)
                    {
                        Name = value.Name;
                        PriceText = value.Price.ToString();
                        TransportType = string.IsNullOrWhiteSpace(value.TransportName) ? "Train" : value.TransportName;
                    }
                }
            }
        }
        // Form fields
        private string _tripType = "Budget";
        public string TripType
        {
            get => _tripType;
            set => Set(ref _tripType, value);
        }

        private string _transportType = "Train";
        public string TransportType
        {
            get => _transportType;
            set => Set(ref _transportType, value);
        }

        private string _name = "";
        public string Name
        {
            get => _name;
            set => Set(ref _name, value);
        }

        private string _priceText = "1000";
        public string PriceText
        {
            get => _priceText;
            set => Set(ref _priceText, value);
        }

        private string _status = "Ready.";
        public string Status
        {
            get => _status;
            set => Set(ref _status, value);
        }

        // Commands
        public ICommand CreateQuickCommand { get; }
        public ICommand CreateCustomCommand { get; }
        public ICommand CloneCommand { get; }
        public ICommand ReloadCommand { get; }

        public ICommand UpdateCommand { get;  }
        public ICommand DeleteCommand { get; }

        public ICommand ApproveBookingCommand => _approveBookingCommand;
        public ICommand RejectBookingCommand => _rejectBookingCommand;
        public ICommand RefreshPendingBookingsCommand { get; set; }
        public ICommand LogoutCommand { get; }

        public ICommand ShowReportsCommand { get; }
        public ICommand ShowPackagesCommand { get; }
        public ICommand ShowBookingsCommand { get; }
        public ICommand GenerateReportCommand { get; }

        public ICommand ShowAllBookingsCommand { get; }
        public ICommand ShowPendingBookingsCommand { get; }
        public ICommand ShowConfirmedBookingsCommand { get; }
        public ICommand ShowDashboardCommand { get; }
        public ICommand ShowRejectedBookingsCommand { get; }
        public ICommand ShowClientsCommand { get; }
        public ICommand OpenClientMessageCommand { get; }
        public ICommand SetReportTypeCommand { get; }
        public ICommand SetExportFormatCommand { get; }

        public int InboxUnreadCount
        {
            get => _inboxUnreadCount;
            private set => Set(ref _inboxUnreadCount, value);
        }

        private string _navSection = "Dashboard";
        public string NavSection
        {
            get => _navSection;
            set => Set(ref _navSection, value);
        }


        public AgentViewModel(
            ITripPackageRepository repo,
            TripCreationService tripCreationService,
            IBookingRepository bookingRepository,
            IUserRepository userRepository,
            IUserMessageRepository userMessages,
            BookingNotificationService notificationService,
            BookingService bookingService,
            BookingAccessService bookingAccessService,
            BookingApprovalChainFactory approvalChainFactory,
            INavigationService navigation)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _tripCreationService = tripCreationService ?? throw new ArgumentNullException(nameof(tripCreationService));
            _bookingRepository = bookingRepository ?? throw new ArgumentNullException(nameof(bookingRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _userMessages = userMessages ?? throw new ArgumentNullException(nameof(userMessages));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _realBookingService = bookingService ?? throw new ArgumentNullException(nameof(bookingService));
            _bookingApprovalChain = (approvalChainFactory ?? throw new ArgumentNullException(nameof(approvalChainFactory))).Create();
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));

            RecreateTripsView();

            CreateQuickCommand = new RelayCommand(CreateQuick);
            CreateCustomCommand = new RelayCommand(CreateCustom);
            CloneCommand = new RelayCommand(CloneSelected, () => SelectedTrip != null);
            ReloadCommand = new RelayCommand(LoadTrips);
            UpdateCommand = new RelayCommand(UpdateSelected, () => SelectedTrip != null);
            DeleteCommand = new RelayCommand(DeleteSelected, () => SelectedTrip != null);
            ShowReportsCommand = new RelayCommand(ShowReports);
            ShowPackagesCommand = new RelayCommand(ShowPackages);
            ShowBookingsCommand = new RelayCommand(ShowBookings);
            GenerateReportCommand = new RelayCommand(GenerateReport);
            LogoutCommand = new RelayCommand(Logout);
            ShowClientsCommand = new RelayCommand(ShowClients);
            OpenClientMessageCommand = new RelayCommand<User?>(OpenClientMessage, u => u != null && !string.IsNullOrWhiteSpace(u.Username));
            SetReportTypeCommand = new RelayCommand<string>(s =>
            {
                if (!string.IsNullOrWhiteSpace(s))
                    SelectedReportType = s;
            });
            SetExportFormatCommand = new RelayCommand<string>(s =>
            {
                if (!string.IsNullOrWhiteSpace(s))
                    SelectedExportFormat = s;
            });

            IsDashboardVisible = true;
            IsPackagesVisible = false;
            IsBookingsVisible = false;
            IsReportsVisible = false;
            IsClientsVisible = false;

            DashboardVisibility = Visibility.Visible;
            PackagesVisibility = Visibility.Collapsed;
            ReportsVisibility = Visibility.Collapsed;
            ClientsVisibility = Visibility.Collapsed;

            Trips.CollectionChanged += (_, __) => RefreshStats();
            RecentClients.CollectionChanged += (_, __) => OnPropertyChanged(nameof(RecentClientsCount));

            var currentUser = SessionManager.Instance.CurrentSession.CurrentUser
                ?? throw new InvalidOperationException("User not authenticated.");

            _bookingService = new BookingAccessProxy(
                bookingAccessService ?? throw new ArgumentNullException(nameof(bookingAccessService)),
                currentUser);

            _notificationService.Attach(this);

            PendingBookings = new ObservableCollection<Booking>();

            _approveBookingCommand = new RelayCommand(ApproveSelectedBooking, () => CanApproveOrRejectBooking());
            _rejectBookingCommand = new RelayCommand(RejectSelectedBooking, () => CanApproveOrRejectBooking());

            RefreshPendingBookingsCommand = new RelayCommand(LoadPendingRequests);

            ShowAllBookingsCommand = new RelayCommand(LoadAllBookings);
            ShowPendingBookingsCommand = new RelayCommand(LoadPendingBookings);
            ShowConfirmedBookingsCommand = new RelayCommand(LoadConfirmedBookings);
            ShowDashboardCommand = new RelayCommand(ShowDashboard);
            ShowRejectedBookingsCommand = new RelayCommand(LoadRejectedBookings);
            SetDashboardBookingFilterCommand = new RelayCommand<string>(filter =>
                DashboardBookingFilter = string.IsNullOrWhiteSpace(filter) ? "All" : filter);

            NavSection = "Dashboard";

            LoadPendingRequests();
            LoadTrips();
            LoadReportBookings();
            LoadAllBookings();

            LoadRecentClients();
            RefreshRecentBookings();
            RefreshDashboardBookings();
            RefreshInboxUnread();
        }

        private void RefreshInboxUnread()
        {
            var me = SessionManager.Instance.CurrentSession.CurrentUser?.Username;
            if (string.IsNullOrWhiteSpace(me))
            {
                InboxUnreadCount = 0;
                return;
            }

            try
            {
                InboxUnreadCount = _userMessages.GetUnreadCount(me);
            }
            catch
            {
                InboxUnreadCount = 0;
            }
        }

        private void OpenClientMessage(User? client)
        {
            if (client == null || string.IsNullOrWhiteSpace(client.Username))
                return;

            var me = SessionManager.Instance.CurrentSession.CurrentUser;
            if (me == null || string.IsNullOrWhiteSpace(me.Username))
                return;

            var win = new UserConversationWindow(
                _userMessages,
                me.Username,
                client.Username,
                $"Mesaje — {client.Username}");
            win.SetOwnerSafe();
            win.ShowDialog();
            RefreshInboxUnread();
        }
        private void LoadTrips()
        {
            try
            {
                Trips.Clear();

                foreach (var t in _repo.GetAll())
                    Trips.Add(t);

                if (Trips.Count > 0 && SelectedTrip == null)
                    SelectedTrip = Trips[0];

                RefreshStats();
                RecreateTripsView();
                var withCover = Trips.Count(t => !string.IsNullOrWhiteSpace(t.CoverImageUrl));
                Status = $"Loaded {Trips.Count} trips from database. Covers: {withCover}/{Trips.Count}.";

                // Only hydrate covers if some are missing/invalid.
                // Never overwrite user-selected covers just to create "variety".
                if (Trips.Any(t => string.IsNullOrWhiteSpace(t.CoverImageUrl) || IsProbablyUnsupportedCoverUrlStatic(t.CoverImageUrl)))
                    _ = HydrateMissingCoverImagesAsync();

                // If API is still starting, first image render may fall back to defaults.
                // Force a refresh once the API becomes reachable so images appear without user filtering.
                _ = RefreshTripsViewOnceApiReadyAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("LoadTrips failed: " + ex);
                Status = "LoadTrips failed: " + ex.Message;
            }
        }

        private void RecreateTripsView()
        {
            var view = CollectionViewSource.GetDefaultView(Trips);
            view.Filter = FilterTrips;
            TripsView = view;
        }

        private const string ApiBaseUrl = "http://localhost:5280";
        private static readonly HttpClient _coverHttp = new() { Timeout = TimeSpan.FromSeconds(10) };

        private async Task RefreshTripsViewOnceApiReadyAsync()
        {
            try
            {
                // Give layout a moment and let the API bootstrap if WPF started it.
                await Task.Delay(500);

                // Wait up to ~6s for API to be reachable.
                for (var i = 0; i < 12; i++)
                {
                    try
                    {
                        using var resp = await _coverHttp.GetAsync($"{ApiBaseUrl}/api/debug/keys");
                        if (resp.IsSuccessStatusCode)
                            break;
                    }
                    catch
                    {
                        // ignore and retry
                    }

                    await Task.Delay(500);
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        // Rebind the view; avoids the buggy Remove/Insert loop that can skip items.
                        RecreateTripsView();
                        TripsView.Refresh();
                    }
                    catch
                    {
                        // ignore
                    }
                });

                // Some ImageSource converters (network/proxy loads) need a second refresh after the API
                // is fully warmed up, otherwise banners can stay blank until the user types in Search.
                await Task.Delay(800);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        TripsView.Refresh();
                    }
                    catch
                    {
                        // ignore
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("RefreshTripsViewOnceApiReadyAsync failed: " + ex);
            }
        }

        private async Task HydrateMissingCoverImagesAsync()
        {
            try
            {
                // Small concurrency limit so we don't spam the API/Unsplash.
                using var gate = new SemaphoreSlim(3, 3);

                var snapshot = Trips.ToList();

                static bool IsProbablyUnsupportedCoverUrl(string? url)
                {
                    if (string.IsNullOrWhiteSpace(url))
                        return false;

                    try
                    {
                        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
                            return false;

                        // We no longer use Wikimedia/Wikipedia as an image source.
                        // Existing rows may still have old covers pointing there; rehydrate them to Unsplash.
                        var host = (uri.Host ?? "").ToLowerInvariant();
                        if (host.Contains("wikimedia.org") || host.Contains("wikipedia.org"))
                            return true;

                        // Unsplash-only: any other remote host is considered invalid for covers.
                        // (Other sources often return webp/avif and break WPF decoding.)
                        if (!host.EndsWith("unsplash.com"))
                            return true;

                        // Common CDN patterns: ?fm=webp / ?format=webp / ?auto=format (often yields webp/avif)
                        var q = (uri.Query ?? "").ToLowerInvariant();
                        if (q.Contains("fm=webp") || q.Contains("fm=avif") || q.Contains("format=webp") || q.Contains("format=avif"))
                            return true;
                        if (q.Contains("auto=format"))
                            return true;

                        var path = uri.AbsolutePath ?? "";
                        var ext = Path.GetExtension(path);
                        if (string.IsNullOrWhiteSpace(ext))
                            return false;

                        ext = ext.Trim().ToLowerInvariant();
                        return ext is ".webp" or ".avif" or ".svg";
                    }
                    catch
                    {
                        return false;
                    }
                }

                static bool IsSupportedCandidateUrl(string? url)
                {
                    if (string.IsNullOrWhiteSpace(url))
                        return false;

                    try
                    {
                        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
                            return false;

                        var q = (uri.Query ?? "").ToLowerInvariant();
                        if (q.Contains("fm=webp") || q.Contains("fm=avif") || q.Contains("format=webp") || q.Contains("format=avif"))
                            return false;
                        if (q.Contains("auto=format"))
                            return false; // may negotiate webp/avif; we'll pick normalized URLs instead

                        var path = uri.AbsolutePath ?? "";
                        var ext = Path.GetExtension(path);
                        if (string.IsNullOrWhiteSpace(ext))
                            return true; // Unknown extension; allow (Unsplash/Wiki sometimes serve without)

                        ext = ext.Trim().ToLowerInvariant();
                        return ext is ".jpg" or ".jpeg" or ".png";
                    }
                    catch
                    {
                        return false;
                    }
                }

                static string NormalizeCoverUrlForWpf(string url)
                {
                    try
                    {
                        var s = (url ?? "").Trim();
                        if (s.Length == 0)
                            return s;

                        if (!Uri.TryCreate(s, UriKind.Absolute, out var uri))
                            return s;

                        if (uri.Host.EndsWith("unsplash.com", StringComparison.OrdinalIgnoreCase))
                        {
                            s = s.Replace("auto=format", "auto=compress", StringComparison.OrdinalIgnoreCase);
                            if (s.Contains("fm=webp", StringComparison.OrdinalIgnoreCase))
                                s = s.Replace("fm=webp", "fm=jpg", StringComparison.OrdinalIgnoreCase);
                            if (s.Contains("fm=avif", StringComparison.OrdinalIgnoreCase))
                                s = s.Replace("fm=avif", "fm=jpg", StringComparison.OrdinalIgnoreCase);
                            if (!s.Contains("fm=", StringComparison.OrdinalIgnoreCase))
                                s += (s.Contains('?', StringComparison.Ordinal) ? "&" : "?") + "fm=jpg";
                        }

                        return s;
                    }
                    catch
                    {
                        return (url ?? "").Trim();
                    }
                }

                // If a cover is already an Unsplash URL but in an unsafe format (auto=format/webp),
                // normalize it in-place (same image) instead of fetching a different one.
                static bool TryNormalizeExistingCoverInPlace(TripPackage trip, out string normalized)
                {
                    normalized = "";
                    var current = (trip.CoverImageUrl ?? "").Trim();
                    if (current.Length == 0)
                        return false;

                    var norm = NormalizeCoverUrlForWpf(current);
                    if (string.Equals(current, norm, StringComparison.Ordinal))
                        return false;

                    normalized = norm;
                    return normalized.Length > 0;
                }

                var tasks = snapshot
                    .Where(t =>
                        t != null &&
                        (string.IsNullOrWhiteSpace(t.CoverImageUrl) ||
                         IsProbablyUnsupportedCoverUrl(t.CoverImageUrl)) &&
                        (!string.IsNullOrWhiteSpace(t.Destination) || !string.IsNullOrWhiteSpace(t.Name)))
                    .Select(async trip =>
                    {
                        await gate.WaitAsync();
                        try
                        {
                            if (TryNormalizeExistingCoverInPlace(trip, out var normalizedCover))
                            {
                                trip.CoverImageUrl = normalizedCover;
                                _repo.Update(trip);

                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    var idx = Trips.IndexOf(trip);
                                    if (idx >= 0)
                                    {
                                        Trips.RemoveAt(idx);
                                        Trips.Insert(idx, trip);
                                    }
                                    TripsView.Refresh();
                                });

                                return;
                            }

                            var rawDest = (trip.Destination ?? "").Trim();
                            var rawCountry = (trip.Country ?? "").Trim();

                            // Handle cases where Destination is stored as "City, Country".
                            string city;
                            string? country;
                            if (rawDest.Contains(',', StringComparison.Ordinal) && string.IsNullOrWhiteSpace(rawCountry))
                            {
                                var parts = rawDest.Split(',', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                                city = parts.Length > 0 ? parts[0] : rawDest;
                                country = parts.Length > 1 ? parts[1] : null;
                            }
                            else
                            {
                                city = rawDest;
                                country = string.IsNullOrWhiteSpace(rawCountry) ? null : rawCountry;
                            }

                            // Fallback for older rows: if Destination is empty, try to infer from Name:
                            // "City Break - Odesa" -> "Odesa"
                            if (string.IsNullOrWhiteSpace(city))
                            {
                                var name = (trip.Name ?? "").Trim();
                                if (!string.IsNullOrWhiteSpace(name))
                                {
                                    var idx = name.LastIndexOf('-');
                                    if (idx >= 0 && idx < name.Length - 1)
                                        city = name[(idx + 1)..].Trim();
                                    if (string.IsNullOrWhiteSpace(city))
                                        city = name;
                                }
                            }

                            if (string.IsNullOrWhiteSpace(city))
                                return;

                            var url =
                                $"{ApiBaseUrl}/api/destinations/images" +
                                $"?city={Uri.EscapeDataString(city)}" +
                                (string.IsNullOrWhiteSpace(country) ? "" : $"&country={Uri.EscapeDataString(country)}") +
                                $"&limit=12" +
                                $"&seed={trip.Id}";

                            using var resp = await _coverHttp.GetAsync(url);
                            if (!resp.IsSuccessStatusCode)
                                return;

                            var json = await resp.Content.ReadAsStringAsync();
                            using var doc = JsonDocument.Parse(json);
                            if (!doc.RootElement.TryGetProperty("images", out var images) ||
                                images.ValueKind != JsonValueKind.Array ||
                                images.GetArrayLength() == 0)
                                return;

                            // Pick a deterministic "random" image per package so cards don't all look identical.
                            var count = images.GetArrayLength();
                            var start = Math.Abs(trip.Id.GetHashCode()) % count;

                            var currentCover = (trip.CoverImageUrl ?? "").Trim();
                            string cover = "";
                            for (var i = 0; i < count; i++)
                            {
                                var idx = (start + i) % count;
                                var item = images[idx];

                                // Prefer thumbUrl (lighter) for card banners, fallback to url.
                                var candidate =
                                    (item.TryGetProperty("thumbUrl", out var thumbEl) ? (thumbEl.GetString() ?? "") : "").Trim();
                                if (candidate.Length == 0)
                                    candidate =
                                        (item.TryGetProperty("url", out var urlEl) ? (urlEl.GetString() ?? "") : "").Trim();

                                if (candidate.Length == 0)
                                    continue;

                                // WPF doesn't support webp/avif/svg out of the box.
                                // Skip those so cards reliably render images.
                                if (!IsSupportedCandidateUrl(candidate))
                                    continue;

                                if (currentCover.Length > 0 &&
                                    string.Equals(currentCover, candidate, StringComparison.OrdinalIgnoreCase))
                                    continue;

                                cover = NormalizeCoverUrlForWpf(candidate);
                                break;
                            }
                            if (cover.Length == 0)
                                return;

                            // Persist
                            trip.CoverImageUrl = cover;
                            _repo.Update(trip);

                            // Force UI refresh (TripPackage doesn't implement INotifyPropertyChanged).
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                var idx = Trips.IndexOf(trip);
                                if (idx >= 0)
                                {
                                    Trips.RemoveAt(idx);
                                    Trips.Insert(idx, trip);
                                }
                                TripsView.Refresh();
                            });
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Hydrate cover failed: " + ex);
                        }
                        finally
                        {
                            gate.Release();
                        }
                    });

                await Task.WhenAll(tasks);

                // Force a full view rebind so ImageBrush converters re-evaluate without requiring user search/filter.
                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        RecreateTripsView();
                    }
                    catch
                    {
                        // ignore
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("HydrateMissingCoverImagesAsync failed: " + ex);
            }
        }

        // Small helper for gating hydrate in LoadTrips (keeps the static local function in sync).
        private static bool IsProbablyUnsupportedCoverUrlStatic(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;
            try
            {
                if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
                    return false;

                var host = (uri.Host ?? "").ToLowerInvariant();
                if (host.Contains("wikimedia.org") || host.Contains("wikipedia.org"))
                    return true;

                if (!host.EndsWith("unsplash.com"))
                    return true;

                var q = (uri.Query ?? "").ToLowerInvariant();
                if (q.Contains("fm=webp") || q.Contains("fm=avif") || q.Contains("format=webp") || q.Contains("format=avif"))
                    return true;
                if (q.Contains("auto=format"))
                    return true;

                var path = uri.AbsolutePath ?? "";
                var ext = Path.GetExtension(path);
                if (string.IsNullOrWhiteSpace(ext))
                    return false;

                ext = ext.Trim().ToLowerInvariant();
                return ext is ".webp" or ".avif" or ".svg";
            }
            catch
            {
                return false;
            }
        }

        public void SelectTripById(int tripId)
        {
            if (tripId <= 0)
                return;

            var match = Trips.FirstOrDefault(t => t.Id == tripId);
            if (match != null)
                SelectedTrip = match;
        }
        private void LoadBookings()
        {
            var bookings = _bookingRepository.GetAll().ToList();

            var bookingCollection = new BookingCollection(bookings);
            var iterator = bookingCollection.CreateAllIterator();

            var result = new List<Booking>();

            while (iterator.HasNext())
            {
                result.Add(iterator.Next());
            }

            HydrateTripPackagesForBookings(result);
            AgentBookings = new ObservableCollection<Booking>(result);
        }

        /// <summary>
        /// Din DB, rezervarea vine cu un TripPackage „stub” (Id + Name). Pentru coperte în UI,
        /// încărcăm pachetul complet (CoverImageUrl, destinație etc.).
        /// </summary>
        private void HydrateTripPackagesForBookings(IEnumerable<Booking> bookings)
        {
            foreach (var booking in bookings)
            {
                var id = booking.TripPackage?.Id ?? 0;
                if (id <= 0)
                    continue;

                var full = _repo.GetById(id);
                if (full != null)
                    booking.TripPackage = full;
            }
        }

        private void LoadBookingsFromIterator(IIterator<Booking> iterator)
        {
            var result = new List<Booking>();

            while (iterator.HasNext())
            {
                result.Add(iterator.Next());
            }

            HydrateTripPackagesForBookings(result);
            AgentBookings = new ObservableCollection<Booking>(result);
        }
        private void LoadAllBookings()
        {
            SetActiveBookingsFilter("All");

            var bookings = _bookingRepository.GetAll().ToList();
            var bookingCollection = new BookingCollection(bookings);
            var iterator = bookingCollection.CreateAllIterator();

            LoadBookingsFromIterator(iterator);
        }

        private void LoadPendingBookings()
        {
            SetActiveBookingsFilter("Pending");

            var bookings = _bookingRepository.GetAll().ToList();
            var bookingCollection = new BookingCollection(bookings);
            var iterator = bookingCollection.CreatePendingIterator();

            LoadBookingsFromIterator(iterator);
        }

        private void LoadConfirmedBookings()
        {
            SetActiveBookingsFilter("Confirmed");

            var bookings = _bookingRepository.GetAll().ToList();
            var bookingCollection = new BookingCollection(bookings);
            var iterator = bookingCollection.CreateConfirmedIterator();

            LoadBookingsFromIterator(iterator);
        }

        private void LoadRejectedBookings()
        {
            SetActiveBookingsFilter("Rejected");

            var bookings = _bookingRepository.GetAll().ToList();
            var bookingCollection = new BookingCollection(bookings);
            var iterator = bookingCollection.CreateRejectedIterator();

            LoadBookingsFromIterator(iterator);
        }

        private void CreateQuick()
        {
            try
            {
                var quick = _navigation.CreateQuickCreatePackageWindow();
                var result = quick.ShowDialog();
                if (result != true || quick.CreatedTrip == null)
                    return;

                // Quick-create does not force cover selection; immediately open full editor
                // so the user can pick the cover image and details.
                var createdId = quick.CreatedTrip.Id;
                var created = _repo.GetById(createdId) ?? quick.CreatedTrip;
                var edit = _navigation.CreateEditPackageWindow(created);
                var editResult = edit.ShowDialog();

                LoadTrips();
                SelectedTrip = Trips.FirstOrDefault(x => x.Id == createdId) ?? Trips.LastOrDefault();
                Status = editResult == true
                    ? "Package created and updated successfully."
                    : "Package created successfully.";
            }
            catch (Exception ex)
            {
                Status = $"Error: {ex.Message}";
            }
        }
        private void CreateCustom()
        {
            try
            {
                var window = _navigation.CreateNewPackageWindow();
                var result = window.ShowDialog();
                if (result == true)
                {
                    LoadTrips();
                    SelectedTrip = Trips.LastOrDefault();
                    Status = "Package created successfully.";
                }
            }
            catch (Exception ex)
            {
                Status = $"Error: {ex.Message}";
            }
        }

        private void CloneSelected()
        {
            try
            {
                if (SelectedTrip == null)
                    return;

                var clone = SelectedTrip.DeepClone();
                clone.Id = 0;
                clone.Name = SelectedTrip.Name + " (Clone)";

                _repo.Add(clone);

                LoadTrips();
                SelectedTrip = Trips.LastOrDefault();

                Status = $"Cloned: {clone.Name}";
            }
            catch (Exception ex)
            {
                Status = $"Error: {ex.Message}";
                MessageBox.Show(ex.Message, "Clone Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteSelected()
        {
            try
            {
                if (SelectedTrip == null)
                    return;

                var id = SelectedTrip.Id;

                _repo.Delete(id);

                LoadTrips(); 

                SelectedTrip = Trips.FirstOrDefault();

                Status = "Trip deleted successfully.";
            }
            catch (Exception ex)
            {
                Status = $"Error: {ex.Message}";
            }
        }

        private void UpdateSelected()
        {
            try
            {
                if (SelectedTrip == null)
                    return;

                int selectedId = SelectedTrip.Id;

                var window = _navigation.CreateEditPackageWindow(SelectedTrip);
                var result = window.ShowDialog();

                if (result == true)
                {
                    LoadTrips();
                    SelectedTrip = Trips.FirstOrDefault(x => x.Id == selectedId);
                    Status = "Trip updated successfully.";
                }
            }
            catch (Exception ex)
            {
                Status = $"Error: {ex.Message}";
            }
        }

        private void RefreshStats()
        {
            OnPropertyChanged(nameof(TotalPackagesCount));
            OnPropertyChanged(nameof(ActiveOffersCount));
            OnPropertyChanged(nameof(AveragePriceText));
            OnPropertyChanged(nameof(TotalValueText));
        }

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (Set(ref _searchText, value))
                {
                    TripsView.Refresh();
                }
            }
        }

        private bool FilterTrips(object obj)
        {
            if (obj is not TripPackage trip)
                return false;

            if (string.IsNullOrWhiteSpace(SearchText))
                return true;

            string search = SearchText.Trim().ToLower();

            return
                !string.IsNullOrWhiteSpace(trip.Name) && trip.Name.ToLower().Contains(search) ||
                !string.IsNullOrWhiteSpace(trip.TransportName) && trip.TransportName.ToLower().Contains(search) ||
                !string.IsNullOrWhiteSpace(trip.StayName) && trip.StayName.ToLower().Contains(search) ||
                trip.Season != null && !string.IsNullOrWhiteSpace(trip.Season.Name) && trip.Season.Name.ToLower().Contains(search);
        }

        public Booking? SelectedBooking
        {
            get => _selectedBooking;
            set
            {
                if (_selectedBooking != value)
                {
                    _selectedBooking = value;
                    OnPropertyChanged(nameof(SelectedBooking));
                    _approveBookingCommand.RaiseCanExecuteChanged();
                    _rejectBookingCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private bool CanApproveOrRejectBooking() =>
            SelectedBooking != null &&
            string.Equals(SelectedBooking.StatusName, "Pending", StringComparison.OrdinalIgnoreCase);

        private void LoadPendingRequests()
        {
            PendingBookings.Clear();

            var bookings = _bookingService.GetPendingBookings();
            HydrateTripPackagesForBookings(bookings);

            foreach (var booking in bookings)
            {
                PendingBookings.Add(booking);
            }
        }

        private void LoadReportBookings()
        {
            AllBookings.Clear();

            var allBookings = _bookingRepository.GetAll().ToList();
            HydrateTripPackagesForBookings(allBookings);

            foreach (var booking in allBookings)
            {
                AllBookings.Add(booking);
            }

            RefreshReportPreview();
        }

        private async void ApproveSelectedBooking()
        {
            if (SelectedBooking == null)
            {
                MessageBox.Show("Selectează mai întâi o cerere de rezervare din listă.",
                                "Aprobare rezervare",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            var context = new BookingApprovalContext(SelectedBooking);
            var approvalResult = _bookingApprovalChain.Handle(context);

            if (!approvalResult.IsApproved)
            {
                MessageBox.Show(approvalResult.Message,
                                "Aprobare blocată",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            try
            {
                var bookingToApprove = SelectedBooking;
                bookingToApprove.IsBeingRemoved = true;

                await Task.Delay(350);

                _realBookingService.ConfirmBooking(bookingToApprove);
                SelectedBooking = null;

                MessageBox.Show("Rezervarea a fost aprobată cu succes.",
                                "Aprobare rezervare",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message,
                                "Eroare stare",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
            }
        }

        private async void RejectSelectedBooking()
        {
            if (SelectedBooking == null)
            {
                MessageBox.Show("Selectează mai întâi o cerere de rezervare din listă.",
                                "Respingere rezervare",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            try
            {
                var bookingToReject = SelectedBooking;
                bookingToReject.IsBeingRemoved = true;

                await Task.Delay(350);

                _realBookingService.RejectBooking(bookingToReject);
                SelectedBooking = null;

                MessageBox.Show("Rezervarea a fost respinsă.",
                                "Respingere rezervare",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message,
                                "Eroare stare",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
            }
        }

        private Visibility _packagesVisibility = Visibility.Visible;
        public Visibility PackagesVisibility
        {
            get => _packagesVisibility;
            set => Set(ref _packagesVisibility, value);
        }

        private Visibility _dashboardVisibility = Visibility.Visible;
        public Visibility DashboardVisibility
        {
            get => _dashboardVisibility;
            set => Set(ref _dashboardVisibility, value);
        }

        private Visibility _reportsVisibility = Visibility.Collapsed;
        public Visibility ReportsVisibility
        {
            get => _reportsVisibility;
            set => Set(ref _reportsVisibility, value);
        }

        private Visibility _clientsVisibility = Visibility.Collapsed;
        public Visibility ClientsVisibility
        {
            get => _clientsVisibility;
            set => Set(ref _clientsVisibility, value);
        }

        private void ShowDashboard()
        {
            DashboardVisibility = Visibility.Visible;
            PackagesVisibility = Visibility.Collapsed;
            ReportsVisibility = Visibility.Collapsed;
            ClientsVisibility = Visibility.Collapsed;

            IsDashboardVisible = true;
            IsPackagesVisible = false;
            IsBookingsVisible = false;
            IsReportsVisible = false;
            IsClientsVisible = false;

            NavSection = "Dashboard";
            RefreshDashboardBookings();
        }

        private void ShowReports()
        {
            DashboardVisibility = Visibility.Collapsed;
            PackagesVisibility = Visibility.Collapsed;
            ReportsVisibility = Visibility.Visible;
            ClientsVisibility = Visibility.Collapsed;

            IsDashboardVisible = false;
            IsPackagesVisible = false;
            IsBookingsVisible = false;
            IsReportsVisible = true;
            IsClientsVisible = false;

            NavSection = "Reports";
            RefreshReportPreview();
        }

        private void ShowPackages()
        {
            DashboardVisibility = Visibility.Collapsed;
            PackagesVisibility = Visibility.Visible;
            ReportsVisibility = Visibility.Collapsed;
            ClientsVisibility = Visibility.Collapsed;

            IsDashboardVisible = false;
            IsPackagesVisible = true;
            IsBookingsVisible = false;
            IsReportsVisible = false;
            IsClientsVisible = false;

            NavSection = "Packages";

            // When the packages view is first shown, templates are created and converters run.
            // If the API/proxy is still warming up, converters may fall back to defaults.
            // Trigger a delayed hard refresh (same effect as user filtering) so images appear automatically.
            _ = RefreshTripsViewOnceApiReadyAsync();
        }

        private void ShowBookings()
        {
            DashboardVisibility = Visibility.Collapsed;
            PackagesVisibility = Visibility.Collapsed;
            ReportsVisibility = Visibility.Collapsed;
            ClientsVisibility = Visibility.Collapsed;

            IsDashboardVisible = false;
            IsPackagesVisible = false;
            IsBookingsVisible = true;
            IsReportsVisible = false;
            IsClientsVisible = false;

            NavSection = "Bookings";
            LoadAllBookings();
        }

        private void ShowClients()
        {
            DashboardVisibility = Visibility.Collapsed;
            PackagesVisibility = Visibility.Collapsed;
            ReportsVisibility = Visibility.Collapsed;
            ClientsVisibility = Visibility.Visible;

            IsDashboardVisible = false;
            IsPackagesVisible = false;
            IsBookingsVisible = false;
            IsReportsVisible = false;
            IsClientsVisible = true;

            NavSection = "Clients";
            LoadRecentClients();
            RefreshInboxUnread();
        }

        private void GenerateReport()
        {
            try
            {
                var bookingsToExport = ReportPreviewBookings.ToList();

                var outputPath = _reportService.GenerateReport(
                    SelectedReportType,
                    SelectedExportFormat,
                    bookingsToExport,
                    "Agent");

                MessageBox.Show(
                    $"Raportul a fost generat:\n{outputPath}",
                    "Succes",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Eroare:\n{ex.Message}",
                    "Eroare",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        private void RefreshReportPreview()
        {
            ReportPreviewBookings.Clear();

            var filtered = SelectedReportType switch
            {
                "În așteptare" or "Pending Bookings" => AllBookings
                    .Where(b => string.Equals(b.Status?.Name, "Pending", StringComparison.OrdinalIgnoreCase)),

                "Confirmate" or "Confirmed Bookings" => AllBookings
                    .Where(b => string.Equals(b.Status?.Name, "Confirmed", StringComparison.OrdinalIgnoreCase)),

                "Respinse" or "Rejected Bookings" => AllBookings
                    .Where(b => string.Equals(b.Status?.Name, "Rejected", StringComparison.OrdinalIgnoreCase)),

                _ => AllBookings
            };

            foreach (var booking in filtered)
            {
                ReportPreviewBookings.Add(booking);
            }
        }

        public void Update(BookingStatusChangedEvent bookingEvent)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (!string.Equals(bookingEvent.NewStatus, "Pending", StringComparison.OrdinalIgnoreCase))
                {
                    var toRemove = PendingBookings.FirstOrDefault(b => b.Id == bookingEvent.Booking.Id);

                    if (toRemove != null)
                        PendingBookings.Remove(toRemove);
                }

                var existing = AllBookings.FirstOrDefault(b => b.Id == bookingEvent.Booking.Id);

                if (existing != null)
                {
                    AllBookings.Remove(existing);
                }

                HydrateTripPackagesForBookings(new[] { bookingEvent.Booking });
                AllBookings.Insert(0, bookingEvent.Booking);

                AddNotificationForBookingEvent(bookingEvent);
                RefreshRecentBookings();
                RefreshReportPreview();

                if (IsBookingsVisible)
                {
                    switch (_currentBookingFilter)
                    {
                        case "Pending":
                            LoadPendingBookings();
                            break;
                        case "Confirmed":
                            LoadConfirmedBookings();
                            break;
                        case "Rejected":
                            LoadRejectedBookings();
                            break;
                        default:
                            LoadAllBookings();
                            break;
                    }

                    _approveBookingCommand.RaiseCanExecuteChanged();
                    _rejectBookingCommand.RaiseCanExecuteChanged();
                }
            });
        }

        private void RefreshRecentBookings()
        {
            RecentBookings.Clear();

            foreach (var b in AllBookings
                         .OrderByDescending(x => x.BookingDate)
                         .Take(8))
            {
                RecentBookings.Add(b);
            }
        }

        private void RefreshDashboardBookings()
        {
            DashboardBookings.Clear();

            IEnumerable<Booking> source = AllBookings;

            source = DashboardBookingFilter switch
            {
                "Pending" => source.Where(b => string.Equals(b.Status?.Name, "Pending", StringComparison.OrdinalIgnoreCase)),
                "Confirmed" => source.Where(b => string.Equals(b.Status?.Name, "Confirmed", StringComparison.OrdinalIgnoreCase)),
                "Cancelled" => source.Where(b =>
                    string.Equals(b.Status?.Name, "Cancelled", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(b.Status?.Name, "Canceled", StringComparison.OrdinalIgnoreCase)),
                _ => source
            };

            foreach (var b in source
                         .OrderByDescending(x => x.BookingDate)
                         .Take(6))
            {
                DashboardBookings.Add(b);
            }
        }

        private void LoadRecentClients()
        {
            RecentClients.Clear();

            var query = _userRepository.GetAll()
                .Where(u => string.Equals(u.Role?.Name, "Client", StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(ClientSearchText))
            {
                query = query.Where(u =>
                    (u.Username ?? "").Contains(ClientSearchText, StringComparison.OrdinalIgnoreCase) ||
                    (u.Email ?? "").Contains(ClientSearchText, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var u in query.Take(8))
                RecentClients.Add(u);
        }

        private void AddNotificationForBookingEvent(BookingStatusChangedEvent bookingEvent)
        {
            var title = bookingEvent.NewStatus switch
            {
                "Confirmed" => "Rezervare confirmată",
                "Rejected" => "Rezervare respinsă",
                "Pending" => "Rezervare în așteptare",
                _ => "Rezervare actualizată"
            };

            var message = $"{bookingEvent.Booking?.Client?.Username ?? "Client"} • {bookingEvent.Booking?.TripPackage?.Name ?? "Pachet"}";

            var accent = bookingEvent.NewStatus switch
            {
                "Confirmed" => "#16A34A",
                "Rejected" => "#F43F5E",
                "Pending" => "#F59E0B",
                _ => "#6366F1"
            };

            Notifications.Insert(0, new AgentNotificationItem
            {
                Title = title,
                Message = message,
                Timestamp = DateTime.Now,
                AccentColor = accent
            });

            while (Notifications.Count > 10)
                Notifications.RemoveAt(Notifications.Count - 1);
        }

        private bool _isDashboardVisible;
public bool IsDashboardVisible
{
    get => _isDashboardVisible;
    set => Set(ref _isDashboardVisible, value);
}

private bool _isPackagesVisible;
public bool IsPackagesVisible
{
    get => _isPackagesVisible;
    set => Set(ref _isPackagesVisible, value);
}

private bool _isBookingsVisible;
public bool IsBookingsVisible
{
    get => _isBookingsVisible;
    set => Set(ref _isBookingsVisible, value);
}

private bool _isReportsVisible;
public bool IsReportsVisible
{
    get => _isReportsVisible;
    set => Set(ref _isReportsVisible, value);
}

private bool _isClientsVisible;
public bool IsClientsVisible
{
    get => _isClientsVisible;
    set => Set(ref _isClientsVisible, value);
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
                .FirstOrDefault(w => w is TravelAgency.WPF.Views.Agent.AgentWindow);

            if (window != null)
                App.Mediator.Publish(new LogoutRequestedMessage(window));
        }

    }
}