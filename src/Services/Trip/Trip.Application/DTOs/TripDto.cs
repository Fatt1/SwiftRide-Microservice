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
    DateTimeOffset CreatedAt
);
