using Matching.Domain.Enums;

namespace Matching.Application.Dtos;

public record PricingContext(
    decimal DistanceKm,
    int EstimatedMinutes,
    DateTime OrderTime,
    WeatherCondition? Weather = null,
    string? PromoCode = null,
    bool HasToll = false
);
