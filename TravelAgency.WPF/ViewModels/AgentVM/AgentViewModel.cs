using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using TravelAgency.Core.Data;
using TravelAgency.Core.Data.Repositories;
using TravelAgency.Core.Models;
using TravelAgency.Core.Models.Booking;
using TravelAgency.Core.Models.TripPkg.Package;
using TravelAgency.Core.Services;
using TravelAgency.WPF.Commands;
using TravelAgency.WPF.Views;

namespace TravelAgency.WPF.ViewModels.AgentVM
{
    public class AgentViewModel : ViewModelBase
    {
        private readonly ITripPackageRepository _repo;
        private readonly TripCreationService _tripCreationService;
        private readonly IBookingAccessService _bookingService;
        private readonly AgentReportService _reportService = new();
        private Booking? _selectedBooking;

        public ObservableCollection<TripPackage> Trips { get; } = new();
        public ObservableCollection<Booking> PendingBookings { get; set; }

        public ObservableCollection<string> ReportTypes { get; } = new()
{
    "All Bookings",
    "Pending Bookings",
    "Confirmed Bookings",
    "Rejected Bookings"
};

        public ObservableCollection<string> ExportFormats { get; } = new()
{
    "PDF",
    "CSV",
    "TXT"
};

        private string _selectedReportType = "All Bookings";
        public string SelectedReportType
        {
            get => _selectedReportType;
            set => Set(ref _selectedReportType, value);
        }

        private string _selectedExportFormat = "PDF";
        public string SelectedExportFormat
        {
            get => _selectedExportFormat;
            set => Set(ref _selectedExportFormat, value);
        }

        public ICollectionView TripsView { get; }
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

        public ICommand ApproveBookingCommand { get; set; }
        public ICommand RejectBookingCommand { get; set; }
        public ICommand RefreshPendingBookingsCommand { get; set; }
        public ICommand LogoutCommand { get; }

        public ICommand ShowReportsCommand { get; }
        public ICommand ShowPackagesCommand { get; }
        public ICommand ShowBookingsCommand { get; }
        public ICommand GenerateReportCommand { get; }


        public AgentViewModel()
            : this(new EfTripPackageRepository(), new TripCreationService())
        {
        }



        public AgentViewModel(ITripPackageRepository repo, TripCreationService tripCreationService)
        {
            _repo = repo;
            _tripCreationService = tripCreationService;

            TripsView = CollectionViewSource.GetDefaultView(Trips);
            TripsView.Filter = FilterTrips;

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

            Trips.CollectionChanged += (_, __) => RefreshStats();
            using (var db = TravelAgencyDbContextFactory.Create())
                db.Database.Migrate();

            var currentUser = SessionManager.Instance.CurrentSession.CurrentUser
     ?? throw new InvalidOperationException("User not authenticated.");

            var bookingRepository = new EfBookingRepository();
            var realBookingService = new BookingAccessService(bookingRepository);
            _bookingService = new BookingAccessProxy(realBookingService, currentUser);

            PendingBookings = new ObservableCollection<Booking>();

            ApproveBookingCommand = new RelayCommand(ApproveSelectedBooking);
            RejectBookingCommand = new RelayCommand(RejectSelectedBooking);
            RefreshPendingBookingsCommand = new RelayCommand(LoadPendingBookings);

            LoadPendingBookings();
            LoadTrips();
        }
        private void LoadTrips()
        {
            Trips.Clear();

            foreach (var t in _repo.GetAll())
                Trips.Add(t);

            
            if (Trips.Count > 0 && SelectedTrip == null)
                SelectedTrip = Trips[0];
            RefreshStats();
            TripsView.Refresh();
            Status = $"Loaded {Trips.Count} trips from database.";
        }

        private void CreateQuick()
        {
            try
            {
                var tripType = string.IsNullOrWhiteSpace(TripType) ? "Budget" : TripType;
                var transportType = string.IsNullOrWhiteSpace(TransportType) ? "Train" : TransportType;

                var name = string.IsNullOrWhiteSpace(Name)
                    ? $"{tripType} {transportType} Trip"
                    : Name.Trim();

                if (!double.TryParse(PriceText, out var price))
                    price = tripType == "Premium" ? 2000 : 1000;

                var request = new TripRequest
                {
                    PackageName = name,
                    TripType = tripType,
                    Category = tripType,
                    TransportType = transportType,
                    BasePrice = price,
                    FinalPrice = price
                };

                var trip = _tripCreationService.CreateTrip(request);

                _repo.Add(trip);
                Trips.Add(trip);
                SelectedTrip = trip;

                Status = $"Created (Quick): {trip.Name} | {trip.TransportName} | {trip.StayName}";
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
                var season = new Season
                {
                    Name = "Summer",
                    StartDate = new DateTime(DateTime.Now.Year, 6, 1),
                    EndDate = new DateTime(DateTime.Now.Year, 8, 31)
                };

                if (!double.TryParse(PriceText, out var price))
                    price = 1200;

                var request = new TripRequest
                {
                    PackageName = string.IsNullOrWhiteSpace(Name) ? "Builder Trip" : Name.Trim(),
                    TripType = string.IsNullOrWhiteSpace(TripType) ? "Premium" : TripType,
                    Category = string.IsNullOrWhiteSpace(TripType) ? "Premium" : TripType,
                    ShortDescription = "Created with Builder",
                    Destination = "Rome",
                    Country = "Italy",
                    StartDate = season.StartDate,
                    EndDate = season.EndDate,
                    NumberOfDays = (season.EndDate - season.StartDate).Days,
                    TransportType = string.IsNullOrWhiteSpace(TransportType) ? "Plane" : TransportType,
                    DepartureCity = "Chisinau",
                    AccommodationType = "Hotel",
                    AccommodationName = "Hotel Roma",
                    MealPlan = "Breakfast",
                    AvailableSeats = 10,
                    AirportTransfer = true,
                    TravelInsurance = true,
                    TourGuide = true,
                    FreeCancellation = false,
                    BasePrice = price,
                    DiscountPercent = 0,
                    VatPercent = 0,
                    ExtraCharges = 0,
                    FinalPrice = price
                };

                var builder = new Core.Patterns.Builders.TripPackageBuilder();
                var director = new Core.Patterns.Builders.TripDirector(builder);

                var trip = director.Make(request);

                

                

                _repo.Add(trip);

          
                Trips.Add(trip);
                SelectedTrip = trip;

                Status = $"Created (Custom): {trip.Name} | {trip.TransportName} | {trip.StayName}";
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

                var window = new CreatePackageWindow(SelectedTrip);
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
                }
            }
        }

        private void LoadPendingBookings()
        {
            PendingBookings.Clear();

            var bookings = _bookingService.GetPendingBookings();

            foreach (var booking in bookings)
            {
                PendingBookings.Add(booking);
            }
        }

        private void ApproveSelectedBooking()
        {
            if (SelectedBooking == null)
            {
                MessageBox.Show("Please select a booking request first.",
                                "Approve Booking",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            _bookingService.ApproveBooking(SelectedBooking);

            LoadPendingBookings();

            MessageBox.Show("Booking request approved successfully.",
                            "Approve Booking",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
        }

        private void RejectSelectedBooking()
        {
            if (SelectedBooking == null)
            {
                MessageBox.Show("Please select a booking request first.",
                                "Reject Booking",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            _bookingService.RejectBooking(SelectedBooking);

            LoadPendingBookings();

            MessageBox.Show("Booking request rejected successfully.",
                            "Reject Booking",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
        }
        private Visibility _packagesVisibility = Visibility.Visible;
        public Visibility PackagesVisibility
        {
            get => _packagesVisibility;
            set => Set(ref _packagesVisibility, value);
        }

        private Visibility _reportsVisibility = Visibility.Collapsed;
        public Visibility ReportsVisibility
        {
            get => _reportsVisibility;
            set => Set(ref _reportsVisibility, value);
        }

        private void ShowReports()
        {
            PackagesVisibility = Visibility.Collapsed;
            ReportsVisibility = Visibility.Visible;
        }

        private void ShowPackages()
        {
            PackagesVisibility = Visibility.Visible;
            ReportsVisibility = Visibility.Collapsed;
        }

        private void ShowBookings()
        {
            PackagesVisibility = Visibility.Collapsed;
            ReportsVisibility = Visibility.Collapsed;
        }

        private void GenerateReport()
        {
            try
            {
                var allBookings = PendingBookings.ToList();

                var outputPath = _reportService.GenerateReport(
                    SelectedReportType,
                    SelectedExportFormat,
                    allBookings,
                    "Agent");

                MessageBox.Show(
                    $"Report generated:\n{outputPath}",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Logout()
        {
            var currentWindow = Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.IsActive);

            SessionManager.Instance.CurrentSession.EndSession();

            var loginWindow = new LoginWindow();
            loginWindow.Show();

            currentWindow?.Close();
        }

    }
}