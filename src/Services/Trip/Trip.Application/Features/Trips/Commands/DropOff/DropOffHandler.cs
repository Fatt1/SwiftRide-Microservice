using EventBus.Messages.Events;
using MassTransit;
using Shared;
using Shared.CQRS;
using Shared.Enums.Payments;
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
        if (!Enum.IsDefined(request.PaymentMethod))
            return Result.Failure(new BadError("Payment method is invalid."));
        if (request.PaymentMethod == PaymentMethod.Card &&
            (string.IsNullOrWhiteSpace(request.GatewayToken) || request.GatewayToken.Length > 128))
            return Result.Failure(new BadError("GatewayToken is required for card payments (maximum 128 characters)."));

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

        // 1. Xác định giá cước cuối cùng: Lấy trực tiếp từ giá ước tính đã chốt lúc tạo chuyến
        var finalFare = trip.EstimatedFare ?? 0m;

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
            GatewayToken = request.GatewayToken,
            Currency = "VND"
        }, cancellationToken);

        // 4. Commit cả thay đổi của Trip và Outbox Message trong cùng 1 Database Transaction
        await tripRepository.SaveChangesAsync();

        return Result.Success();
    }
}
