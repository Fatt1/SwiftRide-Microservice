using Matching.Application.Abstractions;
using Matching.Application.Configurations;
using Matching.Application.Dtos;
using Matching.Domain.Entities;
using Matching.Domain.Enums;

namespace Matching.Application.Decorators.Pricing;

public class WeatherSurgePricing : PricingDecorator
{
    public WeatherSurgePricing(IPricingStrategy inner, PricingConfig config) : base(inner, config)
    {
    }
    public override PricingBreakdown GetPrice(PricingContext context)
    {
        var breakdown = Inner.GetPrice(context);

        if (!context.IsRaining)
        {
            return breakdown;
        }

        var matchedRule = Config.SurgeRules.FirstOrDefault(r =>
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
