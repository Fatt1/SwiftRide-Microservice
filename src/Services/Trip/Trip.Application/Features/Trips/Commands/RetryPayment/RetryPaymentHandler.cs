using EventBus.Messages.Events;
using MassTransit;
using Shared;
using Shared.CQRS;
using Shared.Enums.Payments;
using Trip.Domain.Enums;
using Trip.Domain.Repositories;

namespace Trip.Application.Features.Trips.Commands.RetryPayment;

internal sealed class RetryPaymentHandler(
    ITripRepository tripRepository,
    IPublishEndpoint publishEndpoint) 
    : ICommandHandler<RetryPaymentCommand>
{
    public async Task<Result> Handle(RetryPaymentCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.PaymentMethod))
            return Result.Failure(new BadError("Phương thức thanh toán không hợp lệ."));

        if (request.PaymentMethod == PaymentMethod.Card &&
            (string.IsNullOrWhiteSpace(request.GatewayToken) || request.GatewayToken.Length > 128))
            return Result.Failure(new BadError("Cần có GatewayToken cho thanh toán bằng thẻ (tối đa 128 ký tự)."));

        var trip = await tripRepository.FindByIdAsync(request.TripId);
        if (trip is null)
        {
            return Result.Failure(new NotFoundError("Trip", request.TripId));
        }

        // Chỉ cho phép thanh toán lại khi chuyến bị lỗi thanh toán (PaymentFailed) hoặc chưa hoàn tất (DroppedOff)
        if (trip.Status != TripStatus.PaymentFailed && trip.Status != TripStatus.DroppedOff)
        {
            return Result.Failure(new ConflictError($"Chỉ có thể thanh toán lại khi trạng thái là 'PaymentFailed' hoặc 'DroppedOff'. Trạng thái hiện tại: '{trip.Status}'."));
        }

        var amount = trip.FinalFare ?? trip.EstimatedFare ?? 0m;

        // Bắn sự kiện sang Payment Service để thực hiện thanh toán lại
        await publishEndpoint.Publish(new TripDropOffEvent
        {
            TripId = trip.Id,
            RiderId = trip.RiderId,
            DriverId = trip.DriverId ?? Guid.Empty,
            Amount = amount,
            PaymentMethod = request.PaymentMethod,
            GatewayToken = request.GatewayToken,
            Currency = "VND"
        }, cancellationToken);

        await tripRepository.SaveChangesAsync();

        return Result.Success();
    }
}

