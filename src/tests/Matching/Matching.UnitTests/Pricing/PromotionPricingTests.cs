using Matching.Application.Dtos;
using Matching.Application.Pricing.Stategies;
using Matching.UnitTests.Common.Builders;
using Xunit;

namespace Matching.UnitTests.Pricing;

public class PromotionPricingTests
{
    [Fact]
    public void GetPrice_WithValidPromoCode_AppliesDiscountAndUpdatesFinalTotal()
    {
        // Arrange
        var config = new PricingConfigBuilder()
            .WithRates(perKmRate: 10000, perMinRate: 1000)
            .WithPromoCode("SWIFT20", factor: 0.8, description: "20% Off")
            .Build();
        var inner = new StandardPricing(config);
        var strategy = new PromotionPricing(inner, config);

        var context = new PricingContext(
            DistanceKm: 5.0,
            EstimatedMinutes: 10,
            OrderTime: DateTime.UtcNow,
            PromoCode: "SWIFT20"
        );

        // Act
        var result = strategy.GetPrice(context);

        // Assert
        // Base = 60000, Factor = 0.8 => DiscountAmount = 12000, Final = 48000
        Assert.Equal(0.8, result.RetentionFactor);
        Assert.Equal(12000, result.DiscountAmount, precision: 2);
        Assert.Equal(48000, result.FinalTotal, precision: 2);
        Assert.Equal(48000, result.FareAfterDiscount, precision: 2);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("UNKNOWN_PROMO")]
    public void GetPrice_WithInvalidOrMissingPromoCode_DoesNotApplyDiscount(string? promoCode)
    {
        // Arrange
        var config = new PricingConfigBuilder()
            .WithRates(perKmRate: 10000, perMinRate: 1000)
            .WithPromoCode("SWIFT20", factor: 0.8)
            .Build();
        var inner = new StandardPricing(config);
        var strategy = new PromotionPricing(inner, config);

        var context = new PricingContext(
            DistanceKm: 5.0,
            EstimatedMinutes: 10,
            OrderTime: DateTime.UtcNow,
            PromoCode: promoCode
        );

        // Act
        var result = strategy.GetPrice(context);

        // Assert
        Assert.Equal(1.0, result.RetentionFactor);
        Assert.Equal(0, result.DiscountAmount);
        Assert.Equal(60000, result.FinalTotal);
    }
}
