namespace Trip.Application.DTOs;

public record TripDto(
    Guid Id,
    Guid RiderId,
    Guid? DriverId,
    string Status,
    string PickupAddress,
    string DropoffAddress,
    double PickupLat,
    double PickupLng,
    double DropoffLat,
    double DropoffLng,
    decimal? EstimatedFare,
    decimal? FinalFare,
    decimal? DistanceKm,
    DateTimeOffset CreatedAt,
    int RejectCount = 0,
    string? MatchSessionId = null,
    Guid? OfferedDriverId = null,
    string? FailureReason = null,
    int EstimatedMinutes = 0,
    int MaxAllowedRejections = 0
);

