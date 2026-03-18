using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TravelAgency.Core.Interfaces;
using TravelAgency.Core.Models.Locations;

namespace TravelAgency.Core.Services
{
    public class HotelSearchService
    {
        private readonly IHotelSearchProvider _hotelSearchProvider;

        public HotelSearchService(IHotelSearchProvider hotelSearchProvider)
        {
            _hotelSearchProvider = hotelSearchProvider;
        }

        public Task<List<HotelSearchOption>> SearchHotelsAsync(
            string destination,
            DateTime checkInDate,
            DateTime checkOutDate,
            int adults)
        {
            return _hotelSearchProvider.SearchHotelsAsync(
                destination,
                checkInDate,
                checkOutDate,
                adults);
        }
    }
}