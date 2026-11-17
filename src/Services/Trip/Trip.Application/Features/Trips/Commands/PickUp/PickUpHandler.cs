using Shared;
using Shared.CQRS;
using Trip.Domain.Enums;
using Trip.Domain.Repositories;

namespace Trip.Application.Features.Trips.Commands.PickUp;

internal sealed class PickUpHandler(ITripRepository tripRepository)
    : ICommandHandler<PickUpCommand>
{
    public async Task<Result> Handle(PickUpCommand request, CancellationToken cancellationToken)
    {
        var trip = await tripRepository.FindByIdAsync(request.TripId);
        if (trip is null)
            return Result.Failure(new NotFoundError("Trip", request.TripId));

        if (trip.Status != TripStatus.DriverAccepted)
            return Result.Failure(new ConflictError($"Pickup requires DriverAccepted (status: {trip.Status})."));

        trip.MarkPickedUp();

        await tripRepository.Update(trip);
        await tripRepository.SaveChangesAsync();

        return Result.Success();
    }
}
