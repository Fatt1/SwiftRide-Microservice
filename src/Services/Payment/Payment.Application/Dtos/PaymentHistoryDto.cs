using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Shared.Enums.Payments;

namespace Payment.Application.Dtos;

public sealed record PaymentHistoryDto(Guid PaymentId, Guid TripId, Guid RiderId, Guid DriverId,
    decimal Amount, string Currency, PaymentMethod PaymentMethod, PaymentStatus Status,
    DateTimeOffset CreatedAt, DateTimeOffset? ProcessedAt, string? FailureReason)
{
    public static PaymentHistoryDto From(PaymentTransaction payment) => new(payment.Id, payment.TripId,
        payment.RiderId, payment.DriverId, payment.Amount, payment.Currency, payment.PaymentMethod,
        payment.Status, payment.CreatedAt, payment.ProcessedAt, payment.FailureReason);
}
