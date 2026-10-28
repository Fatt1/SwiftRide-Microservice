using Matching.Application.Abstractions;
using Matching.Application.Dtos;
using Matching.Application.Pricing.Stategies;
using Matching.Domain.Enums;
using Matching.UnitTests.Common.Builders;
using Xunit;

namespace Matching.UnitTests.Pricing;

public class PricingPipelineTests
{
    [Fact]
    public void Pipeline_WithAllFactorsCombined_CalculatesFinalPriceCorrectly()
    {
        // Arrange
        var config = new PricingConfigBuilder()
            .WithRates(perKmRate: 10000, perMinRate: 1000)
            .WithTimeSurge(fromHour: 7, toHour: 9, multiplier: 1.5, name: "Morning Peak")
            .WithWeatherSurge(multiplier: 1.2, name: "Rain Surge")
            .WithTollFee(10000)
            .WithPromoCode("SAVE10", factor: 0.9)
            .Build();

        IPricingStrategy pipeline = new PromotionPricing(
            new TollPricing(
                new WeatherSurgePricing(
                    new TimeSurgePricing(
                        new StandardPricing(config),
                        config),
                    config),
                config),
            config);

        // 5 km, 10 min, Peak (8:00 AM), Raining, HasToll, Promo "SAVE10"
        var context = new PricingContext(
            DistanceKm: 5.0,
            EstimatedMinutes: 10,
            OrderTime: new DateTime(2025, 1, 15, 8, 0, 0, DateTimeKind.Utc),
            IsRaining: true,
            PromoCode: "SAVE10",
            HasToll: true
        );

        // Act
        var result = pipeline.GetPrice(context);

        // Assert
        // 1. Standard: 5*10000 + 10*1000 = 60,000
        // 2. TimeSurge: 60,000 * 1.5 = 90,000
        // 3. WeatherSurge: 90,000 * 1.2 = 108,000
        // 4. Toll: 108,000 + 10,000 = 118,000
        // 5. Promo: 118,000 * 0.9 = 106,200 (Discount = 11,800)
        Assert.Equal(60000, result.BaseFare, precision: 2);
        Assert.Equal(108000, result.FareAfterSurge, precision: 2);
        Assert.Equal(10000, result.TollFee, precision: 2);
        Assert.Equal(11800, result.DiscountAmount, precision: 2);
        Assert.Equal(106200, result.FinalTotal, precision: 2);
        Assert.Equal(2, result.AppliedSurges.Count);
        Assert.Contains(result.AppliedSurges, s => s.Type == SurgeType.Time);
        Assert.Contains(result.AppliedSurges, s => s.Type == SurgeType.Weather);
    }
}
