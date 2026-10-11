using Microsoft.EntityFrameworkCore;
using Payment.Application.Abstractions;
using Payment.Domain.Entities;
using Payment.Domain.Repositories;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure.Repositories;

public sealed class PaymentRepository(PaymentDbContext context)
    : PaymentRepositoryBase<PaymentTransaction>(context), IPaymentRepository, IPaymentTransaction
{
    public async Task<IAsyncDisposable?> BeginOwnedTransactionAsync() =>
        context.Database.CurrentTransaction is null ? await BeginTransactionAsync() : null;
    public Task LockPaymentsAsync(IEnumerable<Guid> ids, CancellationToken ct) => LockAsync("payment", ids, ct);
    public Task LockWalletsAsync(IEnumerable<Guid> ids, CancellationToken ct) => LockAsync("wallet", ids, ct);

    private async Task LockAsync(string scope, IEnumerable<Guid> ids, CancellationToken ct)
    {
        // Protect absent rows too, before a payment or wallet is created.
        foreach (var id in ids.Distinct().Order())
        {
            var key = scope + ":" + id.ToString("N");
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))", ct);
        }
    }

    public Task<PaymentTransaction?> GetByIdAsync(Guid id, CancellationToken ct) =>
        FindByCondition(x => x.Id == id, trackChanges: true).SingleOrDefaultAsync(ct);
    public Task<PaymentTransaction?> GetByTripIdAsync(Guid tripId, CancellationToken ct) =>
        FindByCondition(x => x.TripId == tripId, trackChanges: true).SingleOrDefaultAsync(ct);
    public Task<PaymentTransaction?> GetByIdempotencyKeyAsync(Guid key, CancellationToken ct) =>
        FindByCondition(x => x.IdempotencyKey == key, trackChanges: true).SingleOrDefaultAsync(ct);
    public void Add(PaymentTransaction payment) => Stage(payment);
    public void AddLedgerEntries(IEnumerable<LedgerEntry> entries) => context.LedgerEntries.AddRange(entries);
}
