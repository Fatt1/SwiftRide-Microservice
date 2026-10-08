using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Payment.Application.Payments;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Persistence;

public sealed class PaymentRepository(PaymentDbContext db) : IPaymentRepository
{
    private IDbContextTransaction? transaction;

    public async Task<IAsyncDisposable> LockAsync(string resource, CancellationToken ct)
    {
        await db.Database.OpenConnectionAsync(ct);
        var key = BitConverter.ToInt64(SHA256.HashData(Encoding.UTF8.GetBytes(resource)), 0);
        try { await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_lock({key})", ct); }
        catch
        {
            await db.Database.CloseConnectionAsync();
            throw;
        }
        return new AsyncAction(async () =>
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_unlock({key})");
            await db.Database.CloseConnectionAsync();
        });
    }

    public async Task<IAsyncDisposable> BeginAsync(CancellationToken ct)
    {
        transaction = await db.Database.BeginTransactionAsync(ct);
        var current = transaction;
        return new AsyncAction(async () =>
        {
            await current.DisposeAsync();
            if (transaction == current) transaction = null;
        });
    }

    public Task<PaymentTransaction?> FindAsync(Guid id, CancellationToken ct)
        => db.Payments.Include(p => p.Refunds).SingleOrDefaultAsync(p => p.Id == id, ct);
    public Task<PaymentTransaction?> FindByTripAsync(Guid tripId, CancellationToken ct)
        => db.Payments.Include(p => p.Refunds).SingleOrDefaultAsync(p => p.TripId == tripId, ct);
    public Task<PaymentTransaction?> FindByKeyAsync(Guid key, CancellationToken ct)
        => db.Payments.Include(p => p.Refunds).SingleOrDefaultAsync(p => p.IdempotencyKey == key, ct);

    public async Task<IReadOnlyList<Wallet>> LockWalletsAsync(IEnumerable<Guid> users, CancellationToken ct)
    {
        var result = new List<Wallet>();
        // Stable ordering avoids deadlocks when payments share wallets.
        foreach (var user in users.Distinct().Order())
        {
            var wallet = await db.Wallets.FromSqlInterpolated(
                $"SELECT * FROM \"Wallets\" WHERE \"UserId\" = {user} FOR UPDATE").SingleOrDefaultAsync(ct);
            if (wallet is null) continue;
            await db.Entry(wallet).ReloadAsync(ct);
            result.Add(wallet);
        }
        return result;
    }

    public void Add(PaymentTransaction payment) => db.Payments.Add(payment);
    public void AddRefund(Refund refund) => db.Refunds.Add(refund);
    public void AddLedger(LedgerEntry entry) => db.LedgerEntries.Add(entry);
    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
    public async Task CommitAsync(CancellationToken ct)
    {
        await db.SaveChangesAsync(ct);
        await transaction!.CommitAsync(ct);
    }

    private sealed class AsyncAction(Func<Task> action) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => new(action());
    }
}
