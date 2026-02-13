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
    public class PremiumTripFactory : ITripComponentFactory
    {
        public ITransport CreateTransport() => new Plane();
        public IStay CreateStay() => new Hotel();
        public IExtraService CreateExtraService() => new Guide();
    }
}
