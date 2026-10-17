using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.CQRS;
using Trip.Application.DTOs;
using Trip.Domain.Enums;
using Trip.Domain.Repositories;

namespace Trip.Application.Features.Trips.Queries.GetAllTrips;

internal sealed class GetAllTripsHandler(ITripRepository tripRepository)
    : IQueryHandler<GetAllTripsQuery, IReadOnlyList<TripDto>>
{
    public async Task<Result<IReadOnlyList<TripDto>>> Handle(GetAllTripsQuery request, CancellationToken cancellationToken)
    {
        var query = tripRepository.FindAll(trackChanges: false);

        if (request.RiderId.HasValue && request.RiderId.Value != Guid.Empty)
        {
            query = query.Where(t => t.RiderId == request.RiderId.Value);
        }

        if (request.DriverId.HasValue && request.DriverId.Value != Guid.Empty)
        {
            query = query.Where(t => t.DriverId == request.DriverId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<TripStatus>(request.Status, true, out var status))
        {
            query = query.Where(t => t.Status == status);
        }

        var trips = await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(trip => new TripDto(
                trip.Id,
                trip.RiderId,
                trip.DriverId,
                trip.Status.ToString(),
                trip.PickupAddress,
                trip.DropoffAddress,
                trip.PickupLat,
                trip.PickupLng,
                trip.DropoffLat,
                trip.DropoffLng,
                trip.EstimatedFare,
                trip.FinalFare,
                trip.DistanceKm,
                trip.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<TripDto>>(trips);
    }
}
