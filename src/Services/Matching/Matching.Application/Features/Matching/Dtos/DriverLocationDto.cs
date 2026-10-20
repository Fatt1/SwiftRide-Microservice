namespace Matching.Application.Features.Matching.Dtos;

public sealed record DriverLocationDto(
    string Id,
    Guid DriverId,
    string FullName,
    double Latitude,
    double Longitude,
    bool IsAvailable,
    DateTime UpdatedAt
);
