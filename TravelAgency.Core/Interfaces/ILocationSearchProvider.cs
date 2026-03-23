using System.Collections.Generic;
using System.Threading.Tasks;
using TravelAgency.Core.Models.Locations;

namespace TravelAgency.Core.Interfaces
{
    public interface ILocationSearchProvider
    {
        Task<List<LocationOption>> SearchLocationsAsync(string query, int limit = 10);
    }
}