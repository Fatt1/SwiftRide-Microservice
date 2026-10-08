using Matching.Domain.Enums;

namespace Matching.Application.Configurations;

public class PricingConfig
{
    public const string SectionName = "PricingConfig";
    public double PerKmRate { get; set; } = 8000;
    public double PerMinRate { get; set; } = 500;
    public double TollFee { get; set; } = 10000;
    public double TaxRate { get; set; } = 0.10;
    public List<SurgeRule> SurgeRules { get; set; } = [];
    public List<PromoCodeRule> PromoCodes { get; set; } = [];
}

public class PromoCodeRule
{
    public string Code { get; set; } = default!;
    public string Description { get; set; } = default!;

    public double Factor { get; set; } = 1.0; // Hệ số giảm giá, ví dụ: 0.9 = giảm 10% giá

}

public class SurgeRule
{
    public string RuleId { get; set; } = Guid.NewGuid().ToString();
    public SurgeType Type { get; set; }
    public string Name { get; set; } = default!;
    public double Multiplier { get; set; } = 1.0;
    public SurgeCondition Condition { get; set; } = new();
}

public class SurgeCondition
{
    // Time condition
    public int? FromHour { get; set; }
    public int? ToHour { get; set; }

}

