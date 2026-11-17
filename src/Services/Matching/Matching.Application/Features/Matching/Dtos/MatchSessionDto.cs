using Matching.Domain.Entities;

namespace Matching.Application.Features.Matching.Dtos;

public sealed record MatchSessionDto(
    string Id,
    Guid TripId,
    double PickupLat,
    double PickupLng,
    double DistanceKm,
    double EstimatedMinutes,
    PricingBreakdown PricingBreakdown,
    Guid? MatchedDriverId,
    List<DriverAttempt> DriverAttempts,
    string Status,
    DateTime CreatedAt,
    DateTime LastModifiedAt,
    Guid? OfferedDriverId = null, int MaxAllowedRejections = 3, int RejectCount = 0
);
