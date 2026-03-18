

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TravelAgency.Core.Models.Locations;

namespace TravelAgency.Core.Interfaces
{
    public interface IHotelSearchProvider
    {
        Task<List<HotelSearchOption>> SearchHotelsAsync(
            string destination,
            DateTime checkInDate,
            DateTime checkOutDate,
            int adults);
    }
}