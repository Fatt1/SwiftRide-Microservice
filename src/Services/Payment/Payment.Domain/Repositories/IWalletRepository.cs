using Payment.Domain.Entities;

namespace Payment.Domain.Repositories;

public interface IWalletRepository
{
    Task<Wallet?> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlyList<Wallet>> GetAllAsync(CancellationToken ct);
    void Add(Wallet wallet);
}
