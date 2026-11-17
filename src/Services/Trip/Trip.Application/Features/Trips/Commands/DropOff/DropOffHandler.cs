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
            return Result.Failure(new NotFoundError("Trip", request.TripId));

        if (trip.Status != TripStatus.PickedUp)
            return Result.Failure(new ConflictError($"Dropoff requires PickedUp (status: {trip.Status})."));
        if (!trip.EstimatedFare.HasValue || trip.EstimatedFare.Value <= 0 || !trip.DriverId.HasValue)
            return Result.Failure(new ConflictError("A valid quote and assigned driver are required for payment."));


        // 2. Cập nhật chuyến đi sang DroppedOff và lưu giá tiền
        trip.MarkDroppedOff();
        trip.BeginPayment();
        await tripRepository.Update(trip);

        // 3. Bắn sự kiện sang Payment Service qua RabbitMQ (MassTransit) để trừ tiền khách
        await publishEndpoint.Publish(new TripDropOffEvent
        {
            IdempotencyKey = Guid.NewGuid(),
            TripId = trip.Id,
            RiderId = trip.RiderId,
            DriverId = trip.DriverId.Value,
            Amount = trip.EstimatedFare.Value,
            PaymentMethod = request.PaymentMethod,
            GatewayToken = request.GatewayToken,
            Currency = "VND"
        }, cancellationToken);

        // 4. Commit cả thay đổi của Trip và Outbox Message trong cùng 1 Database Transaction
        await tripRepository.SaveChangesAsync();

        return Result.Success();
    }
}
