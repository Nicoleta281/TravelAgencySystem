using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelAgency.Core.Decorator
{
    public abstract class TripDecorator : ITripComponent
    {
        protected ITripComponent _component;

        public TripDecorator(ITripComponent component)
        {
            _component = component;
        }

        public virtual string GetDescription()
        {
            return _component.GetDescription();
        }

        public virtual double GetPrice()
        {
            return _component.GetPrice();
        }
    }
}
