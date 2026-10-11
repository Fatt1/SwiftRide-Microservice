using Shared;
using Shared.CQRS;
using Trip.Domain.Repositories;

namespace Trip.Application.Features.Trips.Commands.CreateTrip;

internal sealed class CreateTripHandler(ITripRepository tripRepository) 
    : ICommandHandler<CreateTripCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateTripCommand request, CancellationToken cancellationToken)
    {
        var trip = Domain.Entities.Trip.Create(
            request.RiderId,
            request.PickupAddress,
            request.DropoffAddress,
            request.PickupLat,
            request.PickupLng,
            request.DropoffLat,
            request.DropoffLng);

        await tripRepository.CreateAsync(trip);

        return Result.Success(trip.Id);
    }
}
