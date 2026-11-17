using EventBus.Messages.Events;
using MassTransit;
using Matching.Domain.Enums;
using Matching.Domain.Repositories;
namespace Matching.Infrastructure.Consumers;

public sealed class TripCancelledConsumer(IMatchingRepository sessions, IDriverLocationRepository drivers) : IConsumer<TripCancelledEvent>
{
    public async Task Consume(ConsumeContext<TripCancelledEvent> context)
    {
        var message = context.Message;
        var session = await sessions.GetByTripIdAsync(message.TripId, context.CancellationToken);
        if (session == null)
            return;
        var driverId = message.DriverId ?? session.OfferedDriverId;
        if (session.Status != MatchSessionStatus.Cancelled)
        {
            session.Status = MatchSessionStatus.Cancelled;
            await sessions.UpdateAsync(session, context.CancellationToken);
        }
        if (driverId.HasValue)
            await drivers.ReleaseAsync(driverId.Value, message.TripId, context.CancellationToken);
    }
}
