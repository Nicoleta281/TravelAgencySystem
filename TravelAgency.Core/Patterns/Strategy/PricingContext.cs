using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelAgency.Core.Patterns.Strategy
{
    public class PricingContext
    {
        private IPricingStrategy _pricingStrategy;

        public PricingContext(IPricingStrategy pricingStrategy)
        {
            _pricingStrategy = pricingStrategy;
        }

        public void SetStrategy(IPricingStrategy pricingStrategy)
        {
            _pricingStrategy = pricingStrategy;
        }

        public decimal CalculateFinalPrice(decimal basePrice, decimal extraCharges = 0)
        {
            return _pricingStrategy.CalculatePrice(basePrice, extraCharges);
        }
    }
}
