using Payment.Domain.Entities;

namespace Payment.Domain.Repositories;

public interface IPaymentRepository
{
    Task<PaymentTransaction?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<PaymentTransaction?> GetByTripIdAsync(Guid tripId, CancellationToken ct);
    Task<PaymentTransaction?> GetByIdempotencyKeyAsync(Guid key, CancellationToken ct);
    void Add(PaymentTransaction payment);
    void AddLedgerEntries(IEnumerable<LedgerEntry> entries);
}
