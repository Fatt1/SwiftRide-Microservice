using Matching.Application.Abstractions;
using Matching.Application.Configurations;
using Matching.Application.Dtos;
using Matching.Domain.Entities;

namespace Matching.Application.Pricing.Stategies;

public class StandardPricing : IPricingStrategy
{
    private readonly PricingConfig _config;

    public StandardPricing(PricingConfig config)
    {
        _config = config;
    }
    public PricingBreakdown GetPrice(PricingContext context)
    {
        var distanceFare = context.DistanceKm * _config.PerKmRate;
        var timeFare = context.EstimatedMinutes * _config.PerMinRate;
        var price = distanceFare + timeFare;
        return new PricingBreakdown
        {
            DistanceFare = distanceFare,
            TimeFare = timeFare,
            BaseFare = price,
            FinalTotal = price
        };

    }
}
