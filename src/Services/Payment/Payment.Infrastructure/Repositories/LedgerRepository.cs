using Microsoft.EntityFrameworkCore;
using Payment.Domain.Entities;
using Payment.Domain.Repositories;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure.Repositories;

public sealed class LedgerRepository(PaymentDbContext context) : ILedgerRepository
{
    public Task<LedgerEntry?> GetByIdAsync(Guid id, CancellationToken ct) =>
        context.LedgerEntries.AsNoTracking().SingleOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IReadOnlyList<LedgerEntry>> GetAllAsync(CancellationToken ct) =>
        await context.LedgerEntries.AsNoTracking()
            .OrderByDescending(e => e.CreatedAt).ToListAsync(ct);

    public async Task<IReadOnlyList<LedgerEntry>> GetByPaymentIdAsync(Guid paymentId, CancellationToken ct) =>
        await context.LedgerEntries.AsNoTracking()
            .Where(e => e.PaymentId == paymentId)
            .OrderBy(e => e.CreatedAt).ThenBy(e => e.Id).ToListAsync(ct);

    public void Add(LedgerEntry entry) => context.LedgerEntries.Add(entry);
}
