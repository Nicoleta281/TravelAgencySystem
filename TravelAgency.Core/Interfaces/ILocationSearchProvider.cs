using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TravelAgency.Core.Models.Locations;

namespace TravelAgency.Core.Interfaces
{
    public interface ILocationSearchProvider
    {
        Task<List<LocationOption>> SearchLocationsAsync(string query, int limit = 10, CancellationToken cancellationToken = default);

        Task<List<CountryOption>> SearchCountriesAsync(string query, int limit = 10, CancellationToken cancellationToken = default);

        Task<List<LocationOption>> GetCitiesByCountryCodeAsync(string countryCode, int limit = 20, CancellationToken cancellationToken = default);
    }
}