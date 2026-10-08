using EventBus.Messages.Events;
using MassTransit;
using MediatR;
using Payment.Application.Payments;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Payment.Infrastructure.Messaging;

public sealed class TripDropOffConsumer(IServiceScopeFactory scopes) : IConsumer<TripDropOffEvent>
{
    public async Task Consume(ConsumeContext<TripDropOffEvent> context)
    {
        var m = context.Message;
        await using var scope = scopes.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var result = await sender.Send(new CreatePaymentCommand(m.TripId, m.RiderId, m.DriverId,
            m.Amount, m.PaymentMethod, m.TripId, m.CorrelationId, m.GatewayToken, m.Currency), context.CancellationToken);
        if (result.IsFailure) throw new InvalidOperationException(result.Error!.Code);
        if (result.Value.Status == "Pending") throw new InvalidOperationException("Payment requires reconciliation.");
    }
}

public sealed class RefundRequestedConsumer(IServiceScopeFactory scopes) : IConsumer<RefundPaymentRequestedEvent>
{
    public async Task Consume(ConsumeContext<RefundPaymentRequestedEvent> context)
    {
        var m = context.Message;
        await using var scope = scopes.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var result = await sender.Send(new RefundPaymentCommand(m.PaymentId, m.IdempotencyKey, m.Reason), context.CancellationToken);
        if (result.IsFailure) throw new InvalidOperationException(result.Error!.Code);
        if (result.Value.Status == "Pending") throw new InvalidOperationException("Refund requires reconciliation.");
    }
}

public sealed class TripDropOffConsumerDefinition : ConsumerDefinition<TripDropOffConsumer>
{
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpoint,
        IConsumerConfigurator<TripDropOffConsumer> consumer, IRegistrationContext context)
        => endpoint.UseMessageRetry(r =>
        {
            r.Handle<NpgsqlException>(e => e.IsTransient);
            r.Intervals(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4));
        });
}

public sealed class RefundRequestedConsumerDefinition : ConsumerDefinition<RefundRequestedConsumer>
{
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpoint,
        IConsumerConfigurator<RefundRequestedConsumer> consumer, IRegistrationContext context)
        => endpoint.UseMessageRetry(r =>
        {
            r.Handle<NpgsqlException>(e => e.IsTransient);
            r.Intervals(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4));
        });
}
