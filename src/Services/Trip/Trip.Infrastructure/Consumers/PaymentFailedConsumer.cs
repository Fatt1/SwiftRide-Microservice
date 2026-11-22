using EventBus.Messages.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using Trip.Domain.Repositories;
namespace Trip.Infrastructure.Consumers;

public class PaymentFailedConsumer(ITripRepository tripRepository, ILogger<PaymentFailedConsumer> logger) : IConsumer<PaymentFailedEvent>
{
    public async Task Consume(ConsumeContext<PaymentFailedEvent> context)
    {
        var message = context.Message;
        var trip = await tripRepository.FindByIdAsync(message.TripId);
        if (trip == null)
            return;

        // Failures can arrive after a later attempt has started. Keep Trip pending.
        logger.LogWarning("Payment failed for trip {TripId}, payment request {PaymentRequest}: {Reason}",
            trip.Id, message.CorrelationId, message.Reason);

        trip.MarkFailed(message.Reason);
        await tripRepository.Update(trip);
    }
}

public sealed class PaymentFaultConsumer(ILogger<PaymentFaultConsumer> logger) : IConsumer<Fault<TripDropOffEvent>>
{
    public Task Consume(ConsumeContext<Fault<TripDropOffEvent>> context)
    {
        var message = context.Message.Message;
        // An exhausted transport retry does not prove that the payment failed.
        logger.LogError("Payment request {PaymentRequest} for trip {TripId} exhausted automatic retries; review is required",
            message.IdempotencyKey, message.TripId);
        return Task.CompletedTask;
    }
}
