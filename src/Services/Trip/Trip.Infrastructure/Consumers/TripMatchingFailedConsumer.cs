using EventBus.Messages.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using Trip.Domain.Enums;
using Trip.Domain.Repositories;

namespace Trip.Infrastructure.Consumers;

public class TripMatchingFailedConsumer(
    ITripRepository tripRepository,
    ILogger<TripMatchingFailedConsumer> logger) 
    : IConsumer<TripMatchingFailedEvent>
{
    public async Task Consume(ConsumeContext<TripMatchingFailedEvent> context)
    {
        var message = context.Message;
        logger.LogWarning("Received TripMatchingFailedEvent for TripId: {TripId}", message.TripId);

        var trip = await tripRepository.FindByIdAsync(message.TripId);
        if (trip is null)
        {
            logger.LogWarning("Trip with ID '{TripId}' not found when processing TripMatchingFailedEvent.", message.TripId);
            return;
        }

        if (trip.Status == TripStatus.Requested)
        {
            trip.RejectByDriver();
            await tripRepository.Update(trip);
            await tripRepository.SaveChangesAsync();

            logger.LogInformation("TripId: {TripId} marked as DriverRejected due to matching attempts exhaustion.", message.TripId);
        }
        else
        {
            logger.LogWarning("TripId: {TripId} is in status '{Status}', skipping DriverRejected update.", message.TripId, trip.Status);
        }
    }
}
