using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravelAgency.Core.Interfaces;

namespace TravelAgency.Core.Factories.AbstractFactory
{
   public interface ITripComponentFactory

    {
        ITransport CreateTransport(string transportType);
        IStay CreateStay();
        IExtraService CreateExtraService();
    }
}
