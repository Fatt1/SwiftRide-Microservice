using EventBus.Messages.Events;
using MassTransit;
using Shared;
using Shared.CQRS;
using Trip.Domain.Enums;
using Trip.Domain.Repositories;

namespace Trip.Application.Features.Trips.Commands.DropOff;

internal sealed class DropOffHandler(
    ITripRepository tripRepository,
    IPublishEndpoint publishEndpoint) 
    : ICommandHandler<DropOffCommand>
{
    public async Task<Result> Handle(DropOffCommand request, CancellationToken cancellationToken)
    {
        var trip = await tripRepository.FindByIdAsync(request.TripId);
        if (trip is null)
        {
            return Result.Failure(new NotFoundError("Trip", request.TripId));
        }

        // Quy tắc: Chỉ được trả khách khi chuyến đang ở trạng thái PickedUp (đã đón khách)
        if (trip.Status != TripStatus.PickedUp)
        {
            return Result.Failure(new ConflictError($"Chỉ có thể trả khách khi chuyến ở trạng thái 'PickedUp', hiện tại là '{trip.Status}'."));
        }

        // 1. Cập nhật chuyến đi sang DroppedOff và lưu giá tiền
        trip.MarkDroppedOff(request.FinalFare);
        await tripRepository.Update(trip);
        await tripRepository.SaveChangesAsync();

        // 2. Bắn sự kiện sang Payment Service qua RabbitMQ (MassTransit)
        await publishEndpoint.Publish(new TripDropOffEvent
        {
            TripId = trip.Id,
            RiderId = trip.RiderId,
            DriverId = trip.DriverId ?? Guid.Empty,
            Amount = request.FinalFare,
            PaymentMethod = request.PaymentMethod,
            Currency = "VND"
        }, cancellationToken);

        return Result.Success();
    }
}
