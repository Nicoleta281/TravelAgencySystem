using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TravelAgency.Core.Adapters.GeoDb;
using TravelAgency.Core.Adapters.SerpApi;
using TravelAgency.Core.Data.Repositories;
using TravelAgency.Core.Interfaces;
using TravelAgency.Core.Models;
using TravelAgency.Core.Models.Locations;
using TravelAgency.Core.Models.TripPkg.Package;
using TravelAgency.Core.Services;

namespace TravelAgency.Core.Facade
{
    public class TravelPackageFacade
    {
        private readonly ILocationSearchProvider _locationProvider;
        private readonly IHotelSearchProvider _hotelProvider;
        private readonly TripCreationService _tripCreationService;
        private readonly ITripPackageRepository _repository;

        public TravelPackageFacade()
        {
            _locationProvider = new GeoDbLocationAdapter();
            _hotelProvider = new SerpApiHotelAdapter();
            _tripCreationService = new TripCreationService();
            _repository = new EfTripPackageRepository();
        }

        public async Task<List<LocationOption>> SearchLocationsAsync(string query, int limit = 10)
        {
            return await _locationProvider.SearchLocationsAsync(query, limit);
        }

        public async Task<List<HotelSearchOption>> SearchHotelsAsync(
            string destination,
            DateTime checkIn,
            DateTime checkOut,
            int adults)
        {
            return await _hotelProvider.SearchHotelsAsync(destination, checkIn, checkOut, adults);
        }

        public decimal CalculateFinalPrice(
            decimal basePrice,
            decimal discount,
            decimal vat,
            decimal extraCharges,
            decimal compositePrice)
        {
            decimal priceAfterDiscount = basePrice * (1 - discount / 100);
            decimal priceWithVat = priceAfterDiscount * (1 + vat / 100);

            return priceWithVat + extraCharges + compositePrice;
        }

        public TripPackage CreatePackage(TripRequest request)
        {
            return _tripCreationService.CreateTrip(request);
        }

        public TripPackage CreateAndSavePackage(TripRequest request)
        {
            var trip = _tripCreationService.CreateTrip(request);
            _repository.Add(trip);
            return trip;
        }

        public TripPackage CreateAndUpdatePackage(TripRequest request, int tripId)
        {
            var trip = _tripCreationService.CreateTrip(request);
            trip.Id = tripId;
            _repository.Update(trip);
            return trip;
        }
    }
}