using Contracts.Domain;
using Matching.Domain.Enums;

namespace Matching.Domain.Entities;

public class PricingConfig : EntityBase<Guid>
{
    public decimal BaseFare { get; set; } = 10000;
    public decimal PerKmRate { get; set; } = 5000;
    public decimal PerMinRate { get; set; } = 500;
    public decimal TaxRate { get; set; } = 0.10m;
    public SurgeApplyMode SurgeApplyMode { get; set; } = SurgeApplyMode.Multiply;
    public List<SurgeRule> SurgeRules { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public PricingConfig() { }
}

public class SurgeRule
{
    public Guid RuleId { get; set; } = Guid.NewGuid();
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
    public string? WeatherCondition { get; set; } // "rain", "storm"
}

public class PromoCodeConfig
{
    public string Code { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal Factor { get; set; } = 1.0m; // 0.9 = 10% discount
}
