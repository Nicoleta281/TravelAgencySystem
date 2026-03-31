using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelAgency.Core.Decorator
{
    public class InsuranceDecorator : TripDecorator
    {
        public InsuranceDecorator(ITripComponent component)
            : base(component) { }

        public override string GetDescription()
        {
            return base.GetDescription() + ", Insurance";
        }

        public override double GetPrice()
        {
            return base.GetPrice() + 20;
        }
    }
}
