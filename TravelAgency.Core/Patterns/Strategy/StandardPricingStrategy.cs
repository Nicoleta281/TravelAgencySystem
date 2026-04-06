using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelAgency.Core.Patterns.Strategy
{
    public class StandardPricingStrategy : IPricingStrategy
    {
        public decimal CalculatePrice(decimal basePrice, decimal extraCharges = 0)
        {
            return basePrice + extraCharges;
        }
    }
}
