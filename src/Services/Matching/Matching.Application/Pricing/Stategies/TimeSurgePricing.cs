using Matching.Application.Abstractions;
using Matching.Application.Configurations;
using Matching.Application.Dtos;
using Matching.Domain.Entities;
using Matching.Domain.Enums;

namespace Matching.Application.Pricing.Stategies;

public class TimeSurgePricing : IPricingStrategy
{
    private readonly PricingConfig _config;
    private readonly IPricingStrategy _inner;

    public TimeSurgePricing(IPricingStrategy inner, PricingConfig config)
    {
        _config = config;
        _inner = inner;
    }
    public PricingBreakdown GetPrice(PricingContext context)
    {
        // 1. Lấy kết quả từ lớp bên trong
        var breakdown = _inner.GetPrice(context);

        // 2. Tìm rule giờ phù hợp trong cấu hình JSON
        var matchedRule = _config.SurgeRules.FirstOrDefault(r =>
            r.Type == SurgeType.Time &&
            r.Condition.FromHour <= context.OrderTime.Hour &&
            context.OrderTime.Hour <= r.Condition.ToHour);

        // 3. Áp dụng rule nếu tìm được
        if (matchedRule != null)
        {
            breakdown.FinalTotal *= matchedRule.Multiplier;
            breakdown.AppliedSurges.Add(new AppliedSurge
            {
                Type = SurgeType.Time,
                Name = matchedRule.Name,
                Multiplier = matchedRule.Multiplier
            });

            breakdown.FareAfterSurge = breakdown.FinalTotal;
        }
        return breakdown;
    }
}
