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

        // 1. Xác định giá cước cuối cùng: Ưu tiên giá tài xế nhập nếu có, nếu không thì lấy giá ước tính đã chốt lúc tạo chuyến
        var finalFare = (request.FinalFare.HasValue && request.FinalFare.Value > 0)
            ? request.FinalFare.Value
            : (trip.EstimatedFare ?? 0m);

        // 2. Cập nhật chuyến đi sang DroppedOff và lưu giá tiền
        trip.MarkDroppedOff(finalFare);
        await tripRepository.Update(trip);

        // 3. Bắn sự kiện sang Payment Service qua RabbitMQ (MassTransit) để trừ tiền khách
        await publishEndpoint.Publish(new TripDropOffEvent
        {
            TripId = trip.Id,
            RiderId = trip.RiderId,
            DriverId = trip.DriverId ?? Guid.Empty,
            Amount = finalFare,
            PaymentMethod = request.PaymentMethod,
            Currency = "VND"
        }, cancellationToken);

        // 4. Commit cả thay đổi của Trip và Outbox Message trong cùng 1 Database Transaction
        await tripRepository.SaveChangesAsync();

        return Result.Success();
    }
}
