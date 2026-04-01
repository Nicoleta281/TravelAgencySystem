using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelAgency.Core.Patterns.Decorator
{
    public class FreeCancellationDecorator : TripDecorator
    {
        public FreeCancellationDecorator(ITripComponent component)
            : base(component) { }

        public override string GetDescription()
        {
            return base.GetDescription() + ", Free Cancellation";
        }

        public override double GetPrice()
        {
            return base.GetPrice() + 25;
        }
    }
}
