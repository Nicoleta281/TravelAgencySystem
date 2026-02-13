using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravelAgency.Core.Interfaces;
using TravelAgency.Core.Models.TripPkg.Services;
using TravelAgency.Core.Models.TripPkg.Stay;
using TravelAgency.Core.Models.TripPkg.Transport;

namespace TravelAgency.Core.Factories
{
    public class BudgetTripFactory : ITripComponentFactory
    {
        public ITransport CreateTransport() => new Bus();
        public IStay CreateStay() => new Hostel();
        public IExtraService CreateExtraService() => new Breakfast();
    }
}
