using Shared;
using Shared.CQRS;
using Trip.Application.DTOs;
using Trip.Domain.Repositories;

namespace Trip.Application.Features.Trips.Queries.GetTripById;

internal sealed class GetTripByIdHandler(ITripRepository tripRepository) 
    : IQueryHandler<GetTripByIdQuery, TripDto>
{
    public async Task<Result<TripDto>> Handle(GetTripByIdQuery request, CancellationToken cancellationToken)
    {
        var trip = await tripRepository.FindByIdAsync(request.Id);
        if (trip is null)
        {
            return Result.Failure<TripDto>(new NotFoundError("Trip", request.Id));
        }

        // Chuyển đổi (Map) từ Entity sang DTO để gửi về cho Client
        var dto = new TripDto(
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
        );

        return Result.Success(dto);
    }
}
