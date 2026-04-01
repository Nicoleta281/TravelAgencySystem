using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelAgency.Core.Patterns.Decorator
{
    public class AirportTransferDecorator : TripDecorator
    {
        public AirportTransferDecorator(ITripComponent component)
            : base(component) { }

        public override string GetDescription()
        {
            return base.GetDescription() + ", Airport Transfer";
        }

        public override double GetPrice()
        {
            return base.GetPrice() + 30;
        }
    }
}
