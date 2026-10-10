using EventBus.Messages.Events;
using MassTransit;
using Payment.Application.Payments;
using Payment.Domain.Entities;
using Payment.Domain.Enums;

namespace Payment.Infrastructure.Messaging;

public sealed class PaymentEvents(IPublishEndpoint publisher) : IPaymentEvents
{
    public Task PaymentFinishedAsync(PaymentTransaction p, CancellationToken ct)
        => p.Status == PaymentStatus.Completed
            ? publisher.Publish(new PaymentCompletedEvent { PaymentId = p.Id, TripId = p.TripId,
                RiderId = p.RiderId, DriverId = p.DriverId, Amount = p.Amount,
                PaymentMethod = p.PaymentMethod, CorrelationId = p.IdempotencyKey }, ct)
            : publisher.Publish(new PaymentFailedEvent { TripId = p.TripId, RiderId = p.RiderId,
                Amount = p.Amount, Reason = p.FailureReason!, CorrelationId = p.IdempotencyKey }, ct);

}
