namespace Payment.Application.Abstractions;

public interface IPaymentTransaction
{
    // Null means the caller must leave transaction ownership to the consumer.
    Task<IAsyncDisposable?> BeginOwnedTransactionAsync();
    Task EndTransactionAsync();
    Task RollBackTransactionAsync();
    Task<int> SaveChangesAsync();
    Task LockPaymentsAsync(IEnumerable<Guid> ids, CancellationToken ct);
    Task LockWalletsAsync(IEnumerable<Guid> userIds, CancellationToken ct);
}
