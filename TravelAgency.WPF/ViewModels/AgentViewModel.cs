using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using TravelAgency.Core.Data;
using TravelAgency.Core.Data.Repositories;
using TravelAgency.Core.Models;
using TravelAgency.Core.Models.TripPkg.Package;
using TravelAgency.Core.Services;
using TravelAgency.WPF.Commands;
using System.Linq;

namespace TravelAgency.WPF.ViewModels
{
    public class AgentViewModel : ViewModelBase
    {
        private readonly ITripPackageRepository _repo;
        private readonly TripCreationService _tripCreationService;

        public ObservableCollection<TripPackage> Trips { get; } = new();

   
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

        public AgentViewModel()
            : this(new EfTripPackageRepository(), new TripCreationService())
        {
        }

        public AgentViewModel(ITripPackageRepository repo, TripCreationService tripCreationService)
        {
            _repo = repo;
            _tripCreationService = tripCreationService;

            CreateQuickCommand = new RelayCommand(CreateQuick);
            CreateCustomCommand = new RelayCommand(CreateCustom);
            CloneCommand = new RelayCommand(CloneSelected, () => SelectedTrip != null);
            ReloadCommand = new RelayCommand(LoadTrips);
            UpdateCommand = new RelayCommand(UpdateSelected, () => SelectedTrip != null);
            DeleteCommand = new RelayCommand(DeleteSelected, () => SelectedTrip != null);

            using (var db = TravelAgencyDbContextFactory.Create())
                db.Database.Migrate();

            LoadTrips();
        }

        private void LoadTrips()
        {
            Trips.Clear();

            foreach (var t in _repo.GetAll())
                Trips.Add(t);

            if (Trips.Count > 0 && SelectedTrip == null)
                SelectedTrip = Trips[0];

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

                var builder = new TravelAgency.Core.Builders.TripPackageBuilder();
                var director = new TravelAgency.Core.Builders.TripDirector(builder);

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
                clone.Name = SelectedTrip.Name + " (Clone)";

                _repo.Add(clone);
                Trips.Add(clone);
                SelectedTrip = clone;

                Status = $"Cloned: {clone.Name}";
            }
            catch (Exception ex)
            {
                Status = $"Error: {ex.Message}";
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
                Trips.Remove(SelectedTrip);
                SelectedTrip = null;

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

                var tripType = string.IsNullOrWhiteSpace(TripType) ? "Budget" : TripType;
                var transportType = string.IsNullOrWhiteSpace(TransportType) ? "Train" : TransportType;

                var name = string.IsNullOrWhiteSpace(Name)
                    ? SelectedTrip.Name
                    : Name.Trim();

                if (!double.TryParse(PriceText, out var price))
                {
                    Status = "Pret invalid.";
                    return;
                }

                var request = new TripRequest
                {
                    PackageName = name,
                    TripType = tripType,
                    Category = tripType,
                    TransportType = transportType,
                    BasePrice = price,
                    FinalPrice = price
                };

                var updatedTrip = _tripCreationService.CreateTrip(request);
                updatedTrip.Id = SelectedTrip.Id;

                _repo.Update(updatedTrip);

                LoadTrips();
                SelectedTrip = Trips.FirstOrDefault(x => x.Id == updatedTrip.Id);

                Status = "Trip updated successfully.";
            }
            catch (Exception ex)
            {
                Status = $"Error: {ex.Message}";
            }
        }
    }
}