using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelAgency.Core.Patterns.Prototypes
{
    public interface IPrototype<T>

    {
        T ShallowClone();
        T DeepClone();
    }
}
