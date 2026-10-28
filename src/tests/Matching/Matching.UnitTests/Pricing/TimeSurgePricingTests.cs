using Matching.Application.Dtos;
using Matching.Application.Pricing.Stategies;
using Matching.Domain.Enums;
using Matching.UnitTests.Common.Builders;
using Xunit;

namespace Matching.UnitTests.Pricing;

public class TimeSurgePricingTests
{
    [Fact]
    public void GetPrice_DuringPeakHours_AppliesSurgeMultiplier()
    {
        // Arrange
        var config = new PricingConfigBuilder()
            .WithRates(perKmRate: 10000, perMinRate: 1000)
            .WithTimeSurge(fromHour: 7, toHour: 9, multiplier: 1.5, name: "Morning Peak")
            .Build();
        var inner = new StandardPricing(config);
        var strategy = new TimeSurgePricing(inner, config);

        // 8 AM is within peak hours [7, 9]
        var context = new PricingContext(
            DistanceKm: 5.0,
            EstimatedMinutes: 10,
            OrderTime: new DateTime(2025, 1, 15, 8, 30, 0, DateTimeKind.Utc)
        );

        // Act
        var result = strategy.GetPrice(context);

        // Assert
        // Base = 60000, Multiplier = 1.5 => 90000
        Assert.Equal(90000, result.FinalTotal);
        Assert.Equal(90000, result.FareAfterSurge);
        Assert.Contains(result.AppliedSurges, s => s.Type == SurgeType.Time && s.Multiplier == 1.5);
    }

    [Fact]
    public void GetPrice_OutsidePeakHours_DoesNotApplySurgeMultiplier()
    {
        // Arrange
        var config = new PricingConfigBuilder()
            .WithRates(perKmRate: 10000, perMinRate: 1000)
            .WithTimeSurge(fromHour: 7, toHour: 9, multiplier: 1.5, name: "Morning Peak")
            .Build();
        var inner = new StandardPricing(config);
        var strategy = new TimeSurgePricing(inner, config);

        // 14:00 (2 PM) is outside peak hours
        var context = new PricingContext(
            DistanceKm: 5.0,
            EstimatedMinutes: 10,
            OrderTime: new DateTime(2025, 1, 15, 14, 0, 0, DateTimeKind.Utc)
        );

        // Act
        var result = strategy.GetPrice(context);

        // Assert
        Assert.Equal(60000, result.FinalTotal);
        Assert.DoesNotContain(result.AppliedSurges, s => s.Type == SurgeType.Time);
    }
}
