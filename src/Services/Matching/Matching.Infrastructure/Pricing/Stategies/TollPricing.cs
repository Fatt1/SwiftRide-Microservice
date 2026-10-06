using Matching.Application.Abstractions;
using Matching.Application.Configurations;
using Matching.Application.Dtos;
using Matching.Domain.Entities;

namespace Matching.Infrastructure.Pricing.Stategies;

public class TollPricing : IPricingStrategy
{

    private readonly IPricingStrategy _inner;
    private readonly PricingConfig _config;
    public TollPricing(IPricingStrategy inner, PricingConfig config)
    {
        _inner = inner;
        _config = config;
    }
    public PricingBreakdown GetPrice(PricingContext context)
    {
        var breakdown = _inner.GetPrice(context);

        var tollFee = context.HasToll ? _config.TollFee : 0m;

        breakdown.TollFee = tollFee;
        breakdown.FinalTotal += tollFee;
        return breakdown;
    }
}
