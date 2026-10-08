using Matching.Application.Abstractions;
using Matching.Application.Configurations;
using Matching.Application.Dtos;
using Matching.Domain.Entities;

namespace Matching.Application.Pricing.Stategies;

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
        if (!string.IsNullOrWhiteSpace(context.PromoCode))
        {
            var promo = _config.PromoCodes
                .FirstOrDefault(p => string.Equals(p.Code, context.PromoCode, StringComparison.OrdinalIgnoreCase));

            if (promo != null)
            {
                breakdown.RetentionFactor = promo.Factor;
                breakdown.DiscountAmount = breakdown.FinalTotal * (1 - promo.Factor);
                breakdown.FinalTotal *= promo.Factor;
            }
        }

        breakdown.FareAfterDiscount = breakdown.FinalTotal;
        return breakdown;


    }
}
