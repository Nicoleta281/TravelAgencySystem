using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravelAgency.Core.Interfaces;

namespace TravelAgency.Core.Models
{
    public class TripAssembly
    {
        public ITransport Transport { get; }
        public IStay Stay { get; }
        public IExtraService ExtraService { get; }

        public TripAssembly(ITransport t, IStay s, IExtraService e)
        {
            Transport = t;
            Stay = s;
            ExtraService = e;
        }
    }
}
