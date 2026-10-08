using Matching.Application.Abstractions;
using Matching.Application.Configurations;
using Matching.Application.Dtos;
using Matching.Domain.Entities;
using Matching.Domain.Enums;

namespace Matching.Application.Pricing.Stategies;

public class WeatherSurgePricing : IPricingStrategy
{
    private readonly IPricingStrategy _inner;
    private readonly PricingConfig _config;
    public WeatherSurgePricing(IPricingStrategy inner, PricingConfig config)
    {
        _inner = inner;
        _config = config;
    }

    public PricingBreakdown GetPrice(PricingContext context)
    {
        var breakdown = _inner.GetPrice(context);

        if (!context.IsRaining)
        {
            return breakdown;
        }

        var matchedRule = _config.SurgeRules.FirstOrDefault(r =>
           r.Type == SurgeType.Weather
          );

        if (matchedRule != null)
        {
            breakdown.FinalTotal *= matchedRule.Multiplier;

            breakdown.AppliedSurges.Add(new AppliedSurge
            {
                Type = SurgeType.Weather,
                Name = matchedRule.Name,
                Multiplier = matchedRule.Multiplier
            });



            breakdown.FareAfterSurge = breakdown.FinalTotal;
        }

        return breakdown;
    }
}
