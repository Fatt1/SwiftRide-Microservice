using Microsoft.EntityFrameworkCore;
using Payment.Domain.Entities;
using Payment.Domain.Repositories;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure.Repositories;

public sealed class WalletRepository(PaymentDbContext context)
    : PaymentRepositoryBase<Wallet>(context), IWalletRepository
{
    public Task<Wallet?> GetByUserIdAsync(Guid userId, CancellationToken ct) =>
        FindByCondition(x => x.UserId == userId, trackChanges: true).SingleOrDefaultAsync(ct);
    public void Add(Wallet wallet) => Stage(wallet);
}
