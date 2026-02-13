using TravelAgency.Core.Interfaces;
using TravelAgency.Core.Models.TripPkg.Transport;

namespace TravelAgency.Core.Factories
{
    public class TrainFactory : TransportFactory
    {
        public override ITransport CreateTransport()
        {
            return new Train();
        }
    }
}

