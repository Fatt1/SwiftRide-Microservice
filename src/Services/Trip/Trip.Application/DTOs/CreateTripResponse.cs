namespace Trip.Application.DTOs;

public sealed record CreateTripResponse(
    Guid TripId,
    Guid RiderId,
    string Status,
    string PickupAddress,
    string DropoffAddress,
    decimal? EstimatedFare,
    decimal? DistanceKm,
    DateTimeOffset CreatedAt
);
