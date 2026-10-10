namespace Payment.Application.Abstractions;

public interface IPaymentUnitOfWork
{
    Task LockWalletsAsync(IEnumerable<Guid> userIds, CancellationToken ct);
    Task<T> ExecuteAsync<T>(IEnumerable<Guid> lockKeys, Func<CancellationToken, Task<T>> operation, CancellationToken ct);
}
