using EventBus.Messages.Events;
using MassTransit;
using Matching.Domain.Enums;
using Matching.Domain.Repositories;

namespace Matching.Infrastructure.Consumers;

public sealed class TripAcceptedConsumer(IMatchingRepository sessions) : IConsumer<TripAcceptedEvent>
{
    public async Task Consume(ConsumeContext<TripAcceptedEvent> context)
    {
        var message = context.Message;
        var session = await sessions.GetByTripIdAsync(message.TripId, context.CancellationToken);
        if (session is null)
            return;
        if (session.OfferedDriverId != message.DriverId)
            return;
        if (session.Status != MatchSessionStatus.Searching)
            return;

        var attempt = session.DriverAttempts.Single(x => x.DriverId == message.DriverId);
        attempt.Outcome = "Accepted";
        session.MarkMatched(attempt.DriverId);
        await sessions.UpdateAsync(session, context.CancellationToken);
    }
}
