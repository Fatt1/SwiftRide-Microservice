using Payment.Domain.Entities;

namespace Payment.Domain.Repositories;

public interface IWalletRepository
{
    Task<Wallet?> GetByUserIdAsync(Guid userId, CancellationToken ct);
    void Add(Wallet wallet);
}
