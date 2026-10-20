using EventBus.Messages.Events;
using MassTransit;
using Shared;
using Shared.CQRS;
using Trip.Domain.Enums;
using Trip.Domain.Repositories;

namespace Trip.Application.Features.Trips.Commands.DriverAccept;

internal sealed class DriverAcceptHandler(
    ITripRepository tripRepository,
    IPublishEndpoint publishEndpoint) 
    : ICommandHandler<DriverAcceptCommand>
{
    public async Task<Result> Handle(DriverAcceptCommand request, CancellationToken cancellationToken)
    {
        var trip = await tripRepository.FindByIdAsync(request.TripId);
        if (trip is null)
        {
            return Result.Failure(new NotFoundError("Trip", request.TripId));
        }

        if (trip.Status != TripStatus.Requested)
        {
            return Result.Failure(new ConflictError($"Không thể nhận chuyến ở trạng thái '{trip.Status}'."));
        }

        trip.AcceptByDriver(request.DriverId);

        await tripRepository.Update(trip);

        // Bắn sự kiện TripAcceptedEvent sang Matching và Notification Service qua RabbitMQ (MassTransit)
        await publishEndpoint.Publish(new TripAcceptedEvent
        {
            TripId = trip.Id,
            DriverId = request.DriverId,
            RiderId = trip.RiderId
        }, cancellationToken);

        await tripRepository.SaveChangesAsync();

        return Result.Success();
    }
}
