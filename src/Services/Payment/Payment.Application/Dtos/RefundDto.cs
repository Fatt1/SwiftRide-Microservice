using Payment.Domain.Entities;
using Payment.Domain.Enums;

namespace Payment.Application.Dtos;

public sealed record RefundDto(Guid RefundId, Guid PaymentId, decimal Amount, string Reason,
    RefundStatus Status, DateTimeOffset? ProcessedAt)
{
    public static RefundDto From(Refund refund) => new(refund.Id, refund.PaymentId, refund.Amount,
        refund.Reason, refund.Status, refund.ProcessedAt);
}
