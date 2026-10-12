namespace Trip.Application.DTOs;

public sealed record AppliedSurgeDto(
    string Type,
    string Name,
    double Multiplier
);

public sealed record PricingDetailsDto(
    double DistanceFare,
    double TimeFare,
    double TaxRate,
    double BaseFare,
    double RetentionFactor,
    double FareAfterSurge,
    double FareAfterDiscount,
    double TollFee,
    List<AppliedSurgeDto> AppliedSurges,
    double DiscountAmount,
    double TaxAmount,
    double FinalTotal,
    double DistanceKm,
    int EstimatedMinutes
);
