using Microsoft.EntityFrameworkCore;
using Payment.Domain.Entities;
using Payment.Domain.Repositories;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure.Repositories;

public sealed class PaymentRepository(PaymentDbContext context)
    : PaymentRepositoryBase<PaymentTransaction>(context), IPaymentRepository
{
    public Task<PaymentTransaction?> GetByIdAsync(Guid id, CancellationToken ct) =>
        FindByCondition(x => x.Id == id, trackChanges: true).SingleOrDefaultAsync(ct);
    public Task<PaymentTransaction?> GetByTripIdAsync(Guid tripId, CancellationToken ct) =>
        FindByCondition(x => x.TripId == tripId, trackChanges: true).SingleOrDefaultAsync(ct);
    public Task<PaymentTransaction?> GetByIdempotencyKeyAsync(Guid key, CancellationToken ct) =>
        FindByCondition(x => x.IdempotencyKey == key, trackChanges: true).SingleOrDefaultAsync(ct);
    public void Add(PaymentTransaction payment) => Stage(payment);
    public void AddLedgerEntries(IEnumerable<LedgerEntry> entries) => context.LedgerEntries.AddRange(entries);
}
