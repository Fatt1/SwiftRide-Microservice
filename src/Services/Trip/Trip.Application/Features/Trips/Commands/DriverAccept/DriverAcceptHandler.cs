using EventBus.Messages.Events;
using MassTransit;
using Shared;
using Shared.CQRS;
using Trip.Domain.Enums;
using Trip.Domain.Repositories;
namespace Trip.Application.Features.Trips.Commands.DriverAccept;

internal sealed class DriverAcceptHandler(ITripRepository tripRepository, IPublishEndpoint publishEndpoint)
    : ICommandHandler<DriverAcceptCommand>
{
    public async Task<Result> Handle(DriverAcceptCommand request, CancellationToken ct)
    {
        var trip = await tripRepository.FindByIdAsync(request.TripId);
        if (trip is null)
            return Result.Failure(new NotFoundError("Trip", request.TripId));

        if (trip.OfferedDriverId != request.DriverId)
            return Result.Failure(new ConflictError($"Only the current offered driver can accept this trip (status: {trip.Status})."));

        if (trip.Status != TripStatus.Requested)
            return Result.Failure(new ConflictError($"Cannot accept a trip in status {trip.Status}."));

        trip.AcceptByDriver(request.DriverId);
        await tripRepository.Update(trip);
        await publishEndpoint.Publish(new TripAcceptedEvent
        {
            TripId = trip.Id,
            DriverId = request.DriverId,
            RiderId = trip.RiderId,
            MatchSessionId = trip.MatchSessionId!,
        }, ct);
        await tripRepository.SaveChangesAsync();
        return Result.Success();
    }
}
