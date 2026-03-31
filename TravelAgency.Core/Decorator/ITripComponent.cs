using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelAgency.Core.Decorator
{
    public interface ITripComponent
    {
        string GetDescription();
        double GetPrice();
    }
}
