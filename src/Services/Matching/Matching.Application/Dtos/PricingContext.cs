namespace Matching.Application.Dtos;

public record PricingContext(
    double DistanceKm,
    int EstimatedMinutes,
    DateTime OrderTime,
    bool IsRaining = false,
    string? PromoCode = null,
    bool HasToll = false
);
