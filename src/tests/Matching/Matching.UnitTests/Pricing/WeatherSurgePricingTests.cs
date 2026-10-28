using Matching.Application.Dtos;
using Matching.Application.Pricing.Stategies;
using Matching.Domain.Enums;
using Matching.UnitTests.Common.Builders;
using Xunit;

namespace Matching.UnitTests.Pricing;

public class WeatherSurgePricingTests
{
    [Fact]
    public void GetPrice_WhenRaining_AppliesWeatherSurgeMultiplier()
    {
        // Arrange
        var config = new PricingConfigBuilder()
            .WithRates(perKmRate: 10000, perMinRate: 1000)
            .WithWeatherSurge(multiplier: 1.3, name: "Heavy Rain")
            .Build();
        var inner = new StandardPricing(config);
        var strategy = new WeatherSurgePricing(inner, config);

        var context = new PricingContext(
            DistanceKm: 5.0,
            EstimatedMinutes: 10,
            OrderTime: DateTime.UtcNow,
            IsRaining: true
        );

        // Act
        var result = strategy.GetPrice(context);

        // Assert
        // Base = 60000, Multiplier = 1.3 => 78000
        Assert.Equal(78000, result.FinalTotal);
        Assert.Equal(78000, result.FareAfterSurge);
        Assert.Contains(result.AppliedSurges, s => s.Type == SurgeType.Weather && s.Multiplier == 1.3);
    }

    [Fact]
    public void GetPrice_WhenNotRaining_DoesNotApplyWeatherSurge()
    {
        // Arrange
        var config = new PricingConfigBuilder()
            .WithRates(perKmRate: 10000, perMinRate: 1000)
            .WithWeatherSurge(multiplier: 1.3, name: "Heavy Rain")
            .Build();
        var inner = new StandardPricing(config);
        var strategy = new WeatherSurgePricing(inner, config);

        var context = new PricingContext(
            DistanceKm: 5.0,
            EstimatedMinutes: 10,
            OrderTime: DateTime.UtcNow,
            IsRaining: false
        );

        // Act
        var result = strategy.GetPrice(context);

        // Assert
        Assert.Equal(60000, result.FinalTotal);
        Assert.DoesNotContain(result.AppliedSurges, s => s.Type == SurgeType.Weather);
    }
}
