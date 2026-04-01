using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelAgency.Core.Patterns.Decorator
{
    public class GuidedTourDecorator : TripDecorator
    {
        public GuidedTourDecorator(ITripComponent component)
            : base(component) { }

        public override string GetDescription()
        {
            return base.GetDescription() + ", Guided Tour";
        }

        public override double GetPrice()
        {
            return base.GetPrice() + 40;
        }
    }
}
