using MassTransit;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure.Messaging;

public sealed class TripDropOffConsumerDefinition : ConsumerDefinition<TripDropOffConsumer>
{
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<TripDropOffConsumer> consumerConfigurator, IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry =>
        {
            retry.Ignore<ArgumentException>();
            retry.Intervals(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15));
        });
        endpointConfigurator.UseEntityFrameworkOutbox<PaymentDbContext>(context);
    }
}
