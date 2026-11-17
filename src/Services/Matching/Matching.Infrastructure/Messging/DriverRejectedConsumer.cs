using EventBus.Messages.Events;
using MassTransit;
using Matching.Domain.Enums;
using Matching.Domain.Repositories;

namespace Matching.Infrastructure.Consumers;

public sealed class DriverRejectedConsumer(IMatchingRepository sessions, IDriverLocationRepository drivers) : IConsumer<DriverRejectEvent>
{
    public async Task Consume(ConsumeContext<DriverRejectEvent> context)
    {
        var message = context.Message;
        var session = await sessions.GetByTripIdAsync(message.TripId, context.CancellationToken);
        if (session is not null
            && session.OfferedDriverId == message.DriverId
            && session.Status == MatchSessionStatus.Searching)
        {
            var attempt = session.DriverAttempts.Single(x => x.DriverId == message.DriverId);
            attempt.Outcome = "Rejected";
            session.OfferedDriverId = null;
            // Use Trip's failure decision instead of comparing a separate rejection limit here.
            if (message.TripFailed)
                session.MarkNoDriver();
            await sessions.UpdateAsync(session, context.CancellationToken);
        }

        // Also retry this release after a duplicate event or a previous partial failure.
        await drivers.ReleaseAsync(message.DriverId, message.TripId, context.CancellationToken);
        // Matching is called manually after rejection, never automatically here.
    }
}
