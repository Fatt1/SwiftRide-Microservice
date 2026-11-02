using Matching.Application.Abstractions;
using Matching.Application.Configurations;
using Matching.Application.Dtos;
using Matching.Domain.Entities;

namespace Matching.Application.Decorators.Pricing;

public abstract class PricingDecorator : IPricingStrategy
{
    protected readonly IPricingStrategy Inner;
    protected readonly PricingConfig Config;
    protected PricingDecorator(IPricingStrategy inner, PricingConfig config)
    {
        Inner = inner;
        Config = config;
    }

    public virtual PricingBreakdown GetPrice(PricingContext context)
    {
        return Inner.GetPrice(context);
    }
}
