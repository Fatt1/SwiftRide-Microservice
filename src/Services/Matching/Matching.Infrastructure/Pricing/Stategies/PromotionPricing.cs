using Matching.Application.Abstractions;
using Matching.Application.Configurations;
using Matching.Application.Dtos;
using Matching.Domain.Entities;

namespace Matching.Infrastructure.Pricing.Stategies;

public class PromotionPricing : IPricingStrategy
{
    private readonly IPricingStrategy _inner;
    private readonly PricingConfig _config;
    public PromotionPricing(IPricingStrategy inner, PricingConfig config)
    {
        _inner = inner;
        _config = config;
    }
    public PricingBreakdown GetPrice(PricingContext context)
    {
        var breakdown = _inner.GetPrice(context);
        breakdown.RetentionFactor = _config.RetentionFactor;
        breakdown.DiscountAmount = breakdown.FinalTotal * (1 - _config.RetentionFactor);
        breakdown.FinalTotal *= _config.RetentionFactor;
        breakdown.FareAfterDiscount = breakdown.FinalTotal;
        return breakdown;


    }
}
