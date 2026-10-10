using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Payment.Application.Abstractions;

namespace Payment.Infrastructure.Persistence;

public sealed class PaymentUnitOfWork(PaymentDbContext context, ILogger<PaymentUnitOfWork> logger) : IPaymentUnitOfWork
{
    public async Task<T> ExecuteAsync<T>(IEnumerable<Guid> lockKeys,
        Func<CancellationToken, Task<T>> operation, CancellationToken ct)
    {
        var ownsTransaction = context.Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction
            ? await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct) : null;
        try
        {
            logger.LogInformation("Bắt đầu transaction thanh toán; tự quản lý transaction: {OwnsTransaction}", ownsTransaction);
            await LockAsync("payment", lockKeys, ct);
            var result = await operation(ct);
            logger.LogInformation("Đang lưu thay đổi thanh toán và bản ghi outbox");
            var savedEntries = await context.SaveChangesAsync(ct);
            if (transaction is not null)
            {
                await transaction.CommitAsync(ct);
                logger.LogInformation("Đã commit transaction thanh toán; số bản ghi đã lưu: {SavedEntries}", savedEntries);
            }
            else
                logger.LogInformation("Đã lưu thay đổi thanh toán ({SavedEntries} bản ghi); đang chờ consumer commit transaction", savedEntries);
            return result;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Transaction thanh toán gặp lỗi; tự quản lý transaction: {OwnsTransaction}", ownsTransaction);
            if (transaction is not null)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                logger.LogWarning("Đã rollback transaction thanh toán");
            }
            throw;
        }
    }

    public Task LockWalletsAsync(IEnumerable<Guid> userIds, CancellationToken ct) => LockAsync("wallet", userIds, ct);

    private async Task LockAsync(string scope, IEnumerable<Guid> keys, CancellationToken ct)
    {
        // Transaction-scoped locks also protect missing rows before a driver wallet is created.
        foreach (var key in keys.Distinct().Order())
        {
            var lockKey = scope + ":" + key.ToString("N");
            logger.LogDebug("Đang chờ khóa {LockScope} với ID {LockId}", scope, key);
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))", ct);
            logger.LogDebug("Đã lấy khóa {LockScope} với ID {LockId}", scope, key);
        }
    }
}
