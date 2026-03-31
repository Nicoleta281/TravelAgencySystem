using FluentValidation;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using TravelAgency.Core.Data.Repositories;
using TravelAgency.Core.Decorator;
using TravelAgency.Core.Models;
using TravelAgency.Core.Models.Booking;
using TravelAgency.Core.Models.TripPkg.Package;
using TravelAgency.Core.Models.Users;
using TravelAgency.Core.Validators;
using TravelAgency.WPF.Commands;

namespace TravelAgency.WPF.ViewModels.ClientVM
{
    public class ClientWindowViewModel : INotifyPropertyChanged
    {
        private TripPackage? _selectedPackage;
        private double _basePrice;
        private double _totalPrice;
        private string _searchText = "";
        private Visibility _packagesVisibility = Visibility.Visible;
        private Visibility _bookingsVisibility = Visibility.Collapsed;
        private readonly IBookingRepository _bookingRepository;
        private readonly string _currentClientUsername;

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

        public ICommand ConfirmBookingCommand { get; set; }

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

        public ClientWindowViewModel()
        {
            _bookingRepository = new EfBookingRepository();
            _currentClientUsername = "client1";

            Packages = new ObservableCollection<TripPackage>();
            AvailableExtras = new ObservableCollection<OptionalExtra>();
            MyBookings = new ObservableCollection<Booking>();

            ShowBookingsCommand = new RelayCommand(ShowBookings);
            ShowPackagesCommand = new RelayCommand(ShowPackages);
            ConfirmBookingCommand = new RelayCommand(ConfirmBooking);

            LoadFromDatabaseOrSample();
            LoadMyBookings();
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

        private void LoadFromDatabaseOrSample()
        {
            Packages.Clear();

            try
            {
                var repo = new EfTripPackageRepository();
                var trips = repo.GetAll();

                foreach (var trip in trips)
                {
                    Packages.Add(trip);
                }
            }
            catch
            {
                
            }

            if (Packages.Count == 0)
            {
                // fallback: date demo, ca înainte
                Packages.Add(new TripPackage
                {
                    Id = 1,
                    Name = "Paris City Break",
                    Price = 499,
                    TransportDisplayName = "Plane",
                    StayDisplayName = "Hotel",
                    ShortDescription = "A relaxing city break in Paris.",
                    Destination = "Paris",
                    Country = "France"
                });

                Packages.Add(new TripPackage
                {
                    Id = 2,
                    Name = "Venice Weekend",
                    Price = 249,
                    TransportDisplayName = "Bus",
                    StayDisplayName = "Hostel",
                    ShortDescription = "A short and charming Venice trip.",
                    Destination = "Venice",
                    Country = "Italy"
                });

                Packages.Add(new TripPackage
                {
                    Id = 3,
                    Name = "Swiss Alps Adventure",
                    Price = 799,
                    TransportDisplayName = "Plane",
                    StayDisplayName = "Hotel",
                    ShortDescription = "An unforgettable alpine adventure.",
                    Destination = "Zermatt",
                    Country = "Switzerland"
                });
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

            SelectedPackage = Packages.FirstOrDefault();
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

                _bookingRepository.Add(booking);
                LoadMyBookings();

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
        }

        private void LoadMyBookings()
        {
            MyBookings.Clear();

            var bookings = _bookingRepository.GetByClientUsername(_currentClientUsername);

            foreach (var booking in bookings)
            {
                MyBookings.Add(booking);
            }
        }
    }
}