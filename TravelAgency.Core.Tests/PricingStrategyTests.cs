using TravelAgency.Core.Patterns.Strategy;
using Xunit;

namespace TravelAgency.Core.Tests;

public class PricingStrategyTests
{
    [Fact]
    public void StandardStrategy_AddsExtras()
    {
        var ctx = new PricingContext(new StandardPricingStrategy());
        Assert.Equal(130m, ctx.CalculateFinalPrice(100m, 30m));
    }

    [Fact]
    public void FullPricingStrategy_AppliesDiscountPercentAndVat()
    {
        // 10% discount on base, 20% VAT on discounted, + extras
        var ctx = new PricingContext(new FullPricingStrategy(10m, 20m));
        // base 100 -> 90 after 10% off -> +18 VAT -> 108 + 5 extras = 113
        Assert.Equal(113m, ctx.CalculateFinalPrice(100m, 5m));
    }

    [Fact]
    public void Context_CanSwapStrategy()
    {
        var ctx = new PricingContext(new StandardPricingStrategy());
        Assert.Equal(100m, ctx.CalculateFinalPrice(100m, 0m));
        ctx.SetStrategy(new DiscountPricingStrategy(0.1m));
        Assert.Equal(90m, ctx.CalculateFinalPrice(100m, 0m));
    }
}
