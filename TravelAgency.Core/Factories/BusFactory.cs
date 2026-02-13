using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravelAgency.Core.Interfaces;
using TravelAgency.Core.Models.TripPkg.Transport;

namespace TravelAgency.Core.Factories
{
    public class BusFactory : TransportFactory
    {
        public override ITransport CreateTransport()
        {
            return new Bus();
        }
    }
}
