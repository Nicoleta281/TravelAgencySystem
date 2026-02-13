using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TravelAgency.Core.Interfaces;

namespace TravelAgency.Core.Factories
{
   public interface ITripComponentFactory

    {
        ITransport CreateTransport();
        IStay CreateStay();
        IExtraService CreateExtraService();
    }
}
