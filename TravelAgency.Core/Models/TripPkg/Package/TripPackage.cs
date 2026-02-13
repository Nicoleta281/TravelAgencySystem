using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravelAgency.Core.Factories;
using TravelAgency.Core.Interfaces;

namespace TravelAgency.Core.Models.TripPkg.Package
{
    public class TripPackage
    {

        public TripPackage(TransportFactory factory)
    {
        Transport = factory.CreateTransport();
    }


    public string Name { get; set; }
        public double Price { get; set; }
        public Season Season { get; set; }

        public List<TripDay> Days { get; set; } = new();

        public ITransport Transport { get; set; }
        public IStay Stay { get; set; }
        public List<IExtraService> ExtraServices { get; set; } = new();

        public void AddDay(TripDay day) { }
        public void AddExtraService(IExtraService service) { }


}
}
