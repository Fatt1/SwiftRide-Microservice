using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Shared.Enums.Payments;

namespace Payment.Application.Dtos;

public sealed record PaymentDto(Guid PaymentId, Guid? TripId, decimal Amount, string Currency,
    PaymentMethod PaymentMethod, PaymentStatus Status, DateTimeOffset? ProcessedAt, string? FailureReason,
    PaymentType Type, Guid UserId, Guid? DriverId, Guid IdempotencyKey)
{
    public static PaymentDto From(PaymentTransaction payment) => new(payment.Id, payment.TripId,
        payment.Amount, payment.Currency, payment.PaymentMethod, payment.Status, payment.ProcessedAt, payment.FailureReason,
        payment.Type, payment.RiderId, payment.DriverId, payment.IdempotencyKey);
}
