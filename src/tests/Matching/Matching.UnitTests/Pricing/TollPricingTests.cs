using Matching.Application.Dtos;
using Matching.Application.Pricing.Stategies;
using Matching.UnitTests.Common.Builders;
using Xunit;

namespace Matching.UnitTests.Pricing;

public class TollPricingTests
{
    [Fact]
    public void GetPrice_WhenTripHasToll_AddsTollFeeToTotal()
    {
        // Arrange
        var config = new PricingConfigBuilder()
            .WithRates(perKmRate: 10000, perMinRate: 1000)
            .WithTollFee(15000)
            .Build();
        var inner = new StandardPricing(config);
        var strategy = new TollPricing(inner, config);

        var context = new PricingContext(
            DistanceKm: 5.0,
            EstimatedMinutes: 10,
            OrderTime: DateTime.UtcNow,
            HasToll: true
        );

        // Act
        var result = strategy.GetPrice(context);

        // Assert
        // Base = 60000 + Toll 15000 = 75000
        Assert.Equal(15000, result.TollFee);
        Assert.Equal(75000, result.FinalTotal);
    }

    [Fact]
    public void GetPrice_WhenTripDoesNotHaveToll_TollFeeIsZero()
    {
        // Arrange
        var config = new PricingConfigBuilder()
            .WithRates(perKmRate: 10000, perMinRate: 1000)
            .WithTollFee(15000)
            .Build();
        var inner = new StandardPricing(config);
        var strategy = new TollPricing(inner, config);

        var context = new PricingContext(
            DistanceKm: 5.0,
            EstimatedMinutes: 10,
            OrderTime: DateTime.UtcNow,
            HasToll: false
        );

        // Act
        var result = strategy.GetPrice(context);

        // Assert
        Assert.Equal(0, result.TollFee);
        Assert.Equal(60000, result.FinalTotal);
    }
}
