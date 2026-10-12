using EventBus.Messages.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using Trip.Domain.Repositories;

namespace Trip.Infrastructure.Consumers;

public class PaymentCompletedConsumer(
    ITripRepository tripRepository,
    ILogger<PaymentCompletedConsumer> logger) 
    : IConsumer<PaymentCompletedEvent>
{
    public async Task Consume(ConsumeContext<PaymentCompletedEvent> context)
    {
        var message = context.Message;
        logger.LogInformation("Received PaymentCompletedEvent for TripId: {TripId}, PaymentId: {PaymentId}", 
            message.TripId, message.PaymentId);

        var trip = await tripRepository.FindByIdAsync(message.TripId);
        if (trip is null)
        {
            logger.LogWarning("Trip with ID '{TripId}' not found when processing PaymentCompletedEvent.", message.TripId);
            return;
        }

        // Cập nhật trạng thái chuyến đi sang Completed
        trip.MarkCompleted();
        await tripRepository.Update(trip);
        await tripRepository.SaveChangesAsync();

        logger.LogInformation("TripId: {TripId} marked as Completed successfully.", message.TripId);
    }
}
