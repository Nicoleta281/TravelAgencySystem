using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelAgency.Core.Patterns.Strategy
{
    public class DiscountPricingStrategy : IPricingStrategy
    {
        private readonly decimal _discountPercent;

        public DiscountPricingStrategy(decimal discountPercent)
        {
            _discountPercent = discountPercent;
        }

        public decimal CalculatePrice(decimal basePrice, decimal extraCharges = 0)
        {
            decimal discountedPrice = basePrice - (basePrice * _discountPercent);
            return discountedPrice + extraCharges;
        }
    }
}