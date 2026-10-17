using Shared.CQRS;
using Trip.Application.DTOs;

namespace Trip.Application.Features.Trips.Queries.GetAllTrips;

public sealed record GetAllTripsQuery(
    Guid? RiderId = null,
    Guid? DriverId = null,
    string? Status = null
) : IQuery<IReadOnlyList<TripDto>>;
