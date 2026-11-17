using EventBus.Messages.Events;
using MassTransit;
using Shared;
using Shared.CQRS;
using Trip.Domain.Enums;
using Trip.Domain.Repositories;
namespace Trip.Application.Features.Trips.Commands.CancelTrip;
internal sealed class CancelTripHandler(ITripRepository tripRepository, IPublishEndpoint publisher) : ICommandHandler<CancelTripCommand>
{
    public async Task<Result> Handle(CancelTripCommand request, CancellationToken ct)
    {
        var trip = await tripRepository.FindByIdAsync(request.TripId);
        if (trip is null)
            return Result.Failure(new NotFoundError("Trip", request.TripId));

        if (trip.Status is not (TripStatus.Requested or TripStatus.DriverAccepted))
            return Result.Failure(new ConflictError($"Cannot cancel a trip in status {trip.Status}."));

        var driverId = trip.OfferedDriverId ?? trip.DriverId;
        trip.Cancel(); await tripRepository.Update(trip);
        await publisher.Publish(new TripCancelledEvent { TripId = trip.Id, DriverId = driverId}, ct);
        await tripRepository.SaveChangesAsync(); return Result.Success();
    }
}
