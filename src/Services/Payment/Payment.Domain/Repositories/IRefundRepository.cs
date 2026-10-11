using Payment.Domain.Entities;

namespace Payment.Domain.Repositories;

public interface IRefundRepository
{
    Task<Refund?> GetCompletedByPaymentIdAsync(Guid paymentId, CancellationToken ct);
    void Add(Refund refund);
}
