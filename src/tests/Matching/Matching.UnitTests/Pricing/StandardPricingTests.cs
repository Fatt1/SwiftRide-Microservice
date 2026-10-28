using Matching.Application.Dtos;
using Matching.Application.Pricing.Stategies;
using Matching.UnitTests.Common.Builders;
using Xunit;

namespace Matching.UnitTests.Pricing;

public class StandardPricingTests
{
    [Fact]
    public void GetPrice_WithStandardDistanceAndTime_CalculatesBaseFareAccurately()
    {
        // Arrange
        var config = new PricingConfigBuilder()
            .WithRates(perKmRate: 10000, perMinRate: 1000)
            .Build();
        var strategy = new StandardPricing(config);
        var context = new PricingContext(
            DistanceKm: 5.0,
            EstimatedMinutes: 10,
            OrderTime: new DateTime(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc)
        );

        // Act
        var result = strategy.GetPrice(context);

        // Assert
        // DistanceFare: 5 * 10000 = 50000
        // TimeFare: 10 * 1000 = 10000
        // Expected BaseFare and FinalTotal = 60000
        Assert.Equal(50000, result.DistanceFare);
        Assert.Equal(10000, result.TimeFare);
        Assert.Equal(60000, result.BaseFare);
        Assert.Equal(60000, result.FinalTotal);
    }
}
