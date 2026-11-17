using EventBus.Messages.Events;
using MassTransit;
using Matching.Domain.Repositories;
namespace Matching.Infrastructure.Consumers;

public sealed class TripDropOffConsumer(IDriverLocationRepository drivers) : IConsumer<TripDropOffEvent>
{
    public Task Consume(ConsumeContext<TripDropOffEvent> context) => drivers.ReleaseAsync(context.Message.DriverId, context.Message.TripId, ct: context.CancellationToken);
}
