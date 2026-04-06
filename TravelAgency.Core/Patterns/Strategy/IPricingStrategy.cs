namespace TravelAgency.Core.Patterns.Strategy
{
    public interface IPricingStrategy
    {
        decimal CalculatePrice(decimal basePrice, decimal extraCharges = 0);
    }
}