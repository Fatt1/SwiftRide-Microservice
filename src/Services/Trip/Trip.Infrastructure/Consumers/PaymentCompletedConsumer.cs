using EventBus.Messages.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using Trip.Domain.Repositories;
namespace Trip.Infrastructure.Consumers;

public class PaymentCompletedConsumer(ITripRepository tripRepository, ILogger<PaymentCompletedConsumer> logger) : IConsumer<PaymentCompletedEvent>
{
    public async Task Consume(ConsumeContext<PaymentCompletedEvent> context)
    {
        var m = context.Message;
        var trip = await tripRepository.FindByIdAsync(m.TripId);
        if (trip == null)
            return;
        trip.MarkCompleted();
        await tripRepository.Update(trip);
        await tripRepository.SaveChangesAsync();
        logger.LogInformation("Trip {TripId} completed with payment {PaymentId}", trip.Id, m.PaymentId);
    }
}
