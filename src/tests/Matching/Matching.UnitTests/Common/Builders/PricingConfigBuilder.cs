using Matching.Application.Configurations;
using Matching.Domain.Enums;

namespace Matching.UnitTests.Common.Builders;

public class PricingConfigBuilder
{
    private double _perKmRate = 8000;
    private double _perMinRate = 500;
    private double _tollFee = 10000;
    private double _taxRate = 0.10;
    private readonly List<SurgeRule> _surgeRules = [];
    private readonly List<PromoCodeRule> _promoCodes = [];

    public PricingConfigBuilder WithRates(double perKmRate, double perMinRate)
    {
        _perKmRate = perKmRate;
        _perMinRate = perMinRate;
        return this;
    }

    public PricingConfigBuilder WithTollFee(double tollFee)
    {
        _tollFee = tollFee;
        return this;
    }

    public PricingConfigBuilder WithTaxRate(double taxRate)
    {
        _taxRate = taxRate;
        return this;
    }

    public PricingConfigBuilder WithTimeSurge(int fromHour, int toHour, double multiplier, string name = "Peak Hour Surge")
    {
        _surgeRules.Add(new SurgeRule
        {
            Type = SurgeType.Time,
            Name = name,
            Multiplier = multiplier,
            Condition = new SurgeCondition
            {
                FromHour = fromHour,
                ToHour = toHour
            }
        });
        return this;
    }

    public PricingConfigBuilder WithWeatherSurge(double multiplier, string name = "Rain Surge")
    {
        _surgeRules.Add(new SurgeRule
        {
            Type = SurgeType.Weather,
            Name = name,
            Multiplier = multiplier,
            Condition = new SurgeCondition()
        });
        return this;
    }

    public PricingConfigBuilder WithPromoCode(string code, double factor, string description = "Promo Code Discount")
    {
        _promoCodes.Add(new PromoCodeRule
        {
            Code = code,
            Factor = factor,
            Description = description
        });
        return this;
    }

    public PricingConfig Build()
    {
        return new PricingConfig
        {
            PerKmRate = _perKmRate,
            PerMinRate = _perMinRate,
            TollFee = _tollFee,
            TaxRate = _taxRate,
            SurgeRules = [.. _surgeRules],
            PromoCodes = [.. _promoCodes]
        };
    }
}
