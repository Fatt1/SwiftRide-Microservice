using EventBus.Messages.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using Trip.Domain.Repositories;

namespace Trip.Infrastructure.Consumers;

public class PaymentFailedConsumer(
    ITripRepository tripRepository,
    ILogger<PaymentFailedConsumer> logger) 
    : IConsumer<PaymentFailedEvent>
{
    public async Task Consume(ConsumeContext<PaymentFailedEvent> context)
    {
        var message = context.Message;
        logger.LogWarning("Received PaymentFailedEvent for TripId: {TripId}, Reason: {Reason}", 
            message.TripId, message.Reason);

        var trip = await tripRepository.FindByIdAsync(message.TripId);
        if (trip is null)
        {
            logger.LogWarning("Trip with ID '{TripId}' not found when processing PaymentFailedEvent.", message.TripId);
            return;
        }

        // Cập nhật trạng thái chuyến đi sang PaymentFailed kèm lý do
        trip.MarkPaymentFailed(message.Reason);
        await tripRepository.Update(trip);
        await tripRepository.SaveChangesAsync();

        logger.LogInformation("TripId: {TripId} marked as PaymentFailed with Reason: {Reason}", 
            message.TripId, message.Reason);
    }
}
