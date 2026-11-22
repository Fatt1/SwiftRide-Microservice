namespace Payment.Application.Abstractions;

public interface IPaymentTransaction
{
    // Null means an existing transaction is owned by the surrounding handler or consumer.
    Task<IAsyncDisposable?> BeginOwnedTransactionAsync();
    Task EndTransactionAsync();
    Task RollBackTransactionAsync();
    Task<int> SaveChangesAsync();
    Task LockPaymentsAsync(IEnumerable<Guid> ids, CancellationToken ct);
    Task LockWalletsAsync(IEnumerable<Guid> userIds, CancellationToken ct);
}
