using Payment.Domain.Entities;

namespace Payment.Domain.Repositories;

public interface ILedgerRepository
{
    Task<LedgerEntry?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<LedgerEntry>> GetAllAsync(CancellationToken ct);
    Task<IReadOnlyList<LedgerEntry>> GetByPaymentIdAsync(Guid paymentId, CancellationToken ct);
    void Add(LedgerEntry entry);
}
