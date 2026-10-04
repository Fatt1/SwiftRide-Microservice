using Matching.Domain.Enums;

namespace Matching.Application.Configurations;

public class PricingConfig
{
    public const string SectionName = "PricingConfig";

    public decimal BaseFare { get; set; } = 15000;
    public decimal PerKmRate { get; set; } = 8000;
    public decimal PerMinRate { get; set; } = 500;
    public decimal TaxRate { get; set; } = 0.10m;
    public SurgeApplyMode SurgeApplyMode { get; set; } = SurgeApplyMode.Multiply;
    public List<SurgeRule> SurgeRules { get; set; } = [];
    public List<PromoCodeConfig> PromoCodes { get; set; } = [];
}

public class SurgeRule
{
    public string RuleId { get; set; } = Guid.NewGuid().ToString();
    public SurgeType Type { get; set; }
    public string Name { get; set; } = default!;
    public decimal Multiplier { get; set; } = 1.0m;
    public SurgeCondition Condition { get; set; } = new();
}

public class SurgeCondition
{
    // Time condition
    public int? FromHour { get; set; }
    public int? ToHour { get; set; }

    // Zone condition
    public string? ZoneName { get; set; }
    public double? CenterLat { get; set; }
    public double? CenterLng { get; set; }
    public double? RadiusKm { get; set; }

    // Weather condition
    public WeatherCondition? WeatherCondition { get; set; }
}

public class PromoCodeConfig
{
    public string Code { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal Factor { get; set; } = 1.0m; // 0.9 = 10% discount
}
