using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TravelAgency.Core.Data.Repositories;
using TravelAgency.Core.Interfaces;
using TravelAgency.Core.Models;
using TravelAgency.Core.Models.Locations;
using TravelAgency.Core.Models.TripPkg.Package;
using TravelAgency.Core.Patterns.Adapters;
using TravelAgency.Core.Patterns.Adapters.GeoDb;
using TravelAgency.Core.Patterns.Adapters.SerpApi;
using TravelAgency.Core.Services;

namespace TravelAgency.Core.Patterns.Facades
{
    public class TravelPackageFacade
    {
        private readonly ILocationSearchProvider _locationProvider;
        private readonly IHotelSearchProvider _hotelProvider;
        private readonly ITripPackageRepository _tripRepository;
        private readonly TripCreationService _tripCreationService;

        public TravelPackageFacade()
        {
            _locationProvider = new GeoDbLocationAdapter();
            _hotelProvider = new SerpApiHotelAdapter();
            _tripRepository = new EfTripPackageRepository();
            _tripCreationService = new TripCreationService();
        }

        public TravelPackageFacade(
            ILocationSearchProvider locationProvider,
            IHotelSearchProvider hotelProvider,
            ITripPackageRepository tripRepository,
            TripCreationService tripCreationService)
        {
            _locationProvider = locationProvider;
            _hotelProvider = hotelProvider;
            _tripRepository = tripRepository;
            _tripCreationService = tripCreationService;
        }

        public async Task<List<LocationOption>> SearchLocationsAsync(string query, int maxResults = 10)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<LocationOption>();

            return await _locationProvider.SearchLocationsAsync(query, maxResults);
        }

        public async Task<List<HotelSearchOption>> SearchHotelsAsync(
            string destination,
            DateTime checkIn,
            DateTime checkOut,
            int adults = 2)
        {
            if (string.IsNullOrWhiteSpace(destination))
                return new List<HotelSearchOption>();

            if (checkOut <= checkIn)
                throw new InvalidOperationException("Check-out date must be after check-in date.");

            return await _hotelProvider.SearchHotelsAsync(destination, checkIn, checkOut, adults);
        }

        public TripPackage CreatePackage(TripRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return _tripCreationService.CreateTrip(request);
        }

        public TripPackage CreateAndSavePackage(TripRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var trip = _tripCreationService.CreateTrip(request);
            _tripRepository.Add(trip);

            return trip;
        }

        public TripPackage CreateAndUpdatePackage(TripRequest request, int existingId)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var updatedTrip = _tripCreationService.CreateTrip(request);
            updatedTrip.Id = existingId;

            _tripRepository.Update(updatedTrip);

            return updatedTrip;
        }

        public void DeletePackage(int id)
        {
            _tripRepository.Delete(id);
        }

        public IEnumerable<TripPackage> GetAllPackages()
        {
            return _tripRepository.GetAll();
        }
    }
}