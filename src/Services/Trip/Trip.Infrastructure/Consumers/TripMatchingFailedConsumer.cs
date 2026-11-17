using EventBus.Messages.Events;
using MassTransit;
using Trip.Domain.Repositories;
using Trip.Domain.Enums;
namespace Trip.Infrastructure.Consumers;

public sealed class TripMatchingFailedConsumer(ITripRepository trips) : IConsumer<TripMatchingFailedEvent>
{
    public async Task Consume(ConsumeContext<TripMatchingFailedEvent> context)
    {
        var m = context.Message;
        var trip = await trips.FindByIdAsync(m.TripId);
        if (trip == null || trip.Status != TripStatus.Requested || trip.OfferedDriverId.HasValue)
            return;
        trip.MarkFailed(m.Reason);
        await trips.Update(trip);
        await trips.SaveChangesAsync();
    }
}
