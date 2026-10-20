using EventBus.Messages.Events;
using MassTransit;
using Shared;
using Shared.CQRS;
using Trip.Domain.Enums;
using Trip.Domain.Repositories;

namespace Trip.Application.Features.Trips.Commands.DriverReject;

internal sealed class DriverRejectHandler(
    ITripRepository tripRepository,
    IPublishEndpoint publishEndpoint)
    : ICommandHandler<DriverRejectCommand>
{
    public async Task<Result> Handle(DriverRejectCommand request, CancellationToken cancellationToken)
    {
        var trip = await tripRepository.FindByIdAsync(request.TripId);
        if (trip is null)
        {
            return Result.Failure(new NotFoundError("Trip", request.TripId));
        }

        if (trip.Status != TripStatus.Requested)
        {
            return Result.Failure(new ConflictError($"Không thể từ chối chuyến ở trạng thái '{trip.Status}'."));
        }

        // Bắn sự kiện DriverRejectEvent để Notification Service gửi WebSocket thông báo tới Rider
        await publishEndpoint.Publish(new DriverRejectEvent
        {
            TripId = trip.Id,
            DriverId = request.DriverId,
            RiderId = trip.RiderId,
            Reason = "Tài xế đã từ chối nhận chuyến xe này."
        }, cancellationToken);

        await tripRepository.SaveChangesAsync();

        return Result.Success();
    }
}

