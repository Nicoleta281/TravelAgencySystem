using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using TravelAgency.Core.Data;
using TravelAgency.Core.Data.Repositories;
using TravelAgency.Core.Models.TripPkg.Package;
using TravelAgency.Core.Services;
using TravelAgency.WPF.Commands;

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
                }
            }
        }

        // Form fields
        private string _tripType = "Budget";
        public string TripType { get => _tripType; set => Set(ref _tripType, value); }

        private string _transportType = "Train";
        public string TransportType { get => _transportType; set => Set(ref _transportType, value); }

        private string _name = "";
        public string Name { get => _name; set => Set(ref _name, value); }

        private string _priceText = "1000";
        public string PriceText { get => _priceText; set => Set(ref _priceText, value); }

        private string _status = "Ready.";
        public string Status { get => _status; set => Set(ref _status, value); }

        // Commands
        public ICommand CreateQuickCommand { get; }
        public ICommand CreateCustomCommand { get; }
        public ICommand CloneCommand { get; }
        public ICommand ReloadCommand { get; }

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

            // migrate at startup (ok for now)
            using (var db = TravelAgencyDbContextFactory.Create())
                db.Database.Migrate();

            LoadTrips();
        }

        private void LoadTrips()
        {
            Trips.Clear();
            foreach (var t in _repo.GetAll())
                Trips.Add(t);

            Status = $"Loaded {Trips.Count} trips from database.";
        }

        private void CreateQuick()
        {
            var tripType = string.IsNullOrWhiteSpace(TripType) ? "Budget" : TripType;
            var transportType = string.IsNullOrWhiteSpace(TransportType) ? "Train" : TransportType;

            var name = string.IsNullOrWhiteSpace(Name)
                ? $"{tripType} {transportType} Trip"
                : Name.Trim();

            if (!double.TryParse(PriceText, out var price))
                price = tripType == "Premium" ? 2000 : 1000;

            var trip = _tripCreationService.CreateTrip(tripType, transportType, name, price);

            _repo.Add(trip);
            Trips.Add(trip);
            SelectedTrip = trip;

            Status = $"Created (Quick): {trip.Name} | {trip.TransportName} | {trip.StayName}";
        }

        private void CreateCustom()
        {
            var season = new Season
            {
                Name = "Summer",
                StartDate = new DateTime(DateTime.Now.Year, 6, 1),
                EndDate = new DateTime(DateTime.Now.Year, 8, 31)
            };

            if (!double.TryParse(PriceText, out var price))
                price = 1200;

            var trip = new TravelAgency.Core.Builders.TripPackageBuilder()
                .WithName(string.IsNullOrWhiteSpace(Name) ? "Builder Trip" : Name.Trim())
                .WithPrice(price)
                .WithSeason(season)
                .WithTransport(new TravelAgency.Core.Models.TripPkg.Transport.Plane())
                .WithStay(new TravelAgency.Core.Models.TripPkg.Stay.Hotel { Name = "Hotel Roma", Address = "Center" })
                .WithExtra(new TravelAgency.Core.Models.TripPkg.Services.Guide())
                .WithExtra(new TravelAgency.Core.Models.TripPkg.Services.Breakfast())
                .WithDay(new TripDay())
                .Build();

            _repo.Add(trip);
            Trips.Add(trip);
            SelectedTrip = trip;

            Status = $"Created (Custom): {trip.Name} | {trip.TransportName} | {trip.StayName}";
        }

        private void CloneSelected()
        {
            if (SelectedTrip == null) return;

            var clone = SelectedTrip.DeepClone();
            clone.Name = SelectedTrip.Name + " (Clone)";

            _repo.Add(clone);
            Trips.Add(clone);
            SelectedTrip = clone;

            Status = $"Cloned: {clone.Name}";
        }
    }
}
