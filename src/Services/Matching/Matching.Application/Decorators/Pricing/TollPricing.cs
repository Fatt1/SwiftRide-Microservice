using Matching.Application.Abstractions;
using Matching.Application.Configurations;
using Matching.Application.Dtos;
using Matching.Domain.Entities;

namespace Matching.Application.Decorators.Pricing;

public class TollPricing : PricingDecorator
{
    public TollPricing(IPricingStrategy inner, PricingConfig config) : base(inner, config)
    {
    }
    public override PricingBreakdown GetPrice(PricingContext context)
    {
        var breakdown = base.GetPrice(context);
        var tollFee = context.HasToll ? Config.TollFee : 0;
        breakdown.TollFee = tollFee;
        breakdown.FinalTotal += tollFee;
        return breakdown;
    }

}
