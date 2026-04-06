namespace TravelAgency.Core.Patterns.Strategy
{
    public class FullPricingStrategy : IPricingStrategy
    {
        private readonly decimal _discountPercent;
        private readonly decimal _vatPercent;

        public FullPricingStrategy(decimal discountPercent, decimal vatPercent)
        {
            _discountPercent = discountPercent;
            _vatPercent = vatPercent;
        }

        public decimal CalculatePrice(decimal basePrice, decimal extraCharges = 0)
        {
            decimal discountedPrice = basePrice - (basePrice * _discountPercent / 100m);
            decimal vatAmount = discountedPrice * _vatPercent / 100m;

            return discountedPrice + vatAmount + extraCharges;
        }
    }
}