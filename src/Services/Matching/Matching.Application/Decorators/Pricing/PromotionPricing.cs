using Matching.Application.Abstractions;
using Matching.Application.Configurations;
using Matching.Application.Dtos;
using Matching.Domain.Entities;

namespace Matching.Application.Decorators.Pricing;

public class PromotionPricing : PricingDecorator
{
    public PromotionPricing(IPricingStrategy inner, PricingConfig config) : base(inner, config)
    {
    }
    public override PricingBreakdown GetPrice(PricingContext context)
    {
        var breakdown = base.GetPrice(context);
        if (!string.IsNullOrWhiteSpace(context.PromoCode))
        {
            var promo = base.Config.PromoCodes
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
