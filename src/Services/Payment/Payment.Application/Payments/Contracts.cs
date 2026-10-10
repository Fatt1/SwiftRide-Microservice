using Payment.Domain.Entities;
using Shared.CQRS;
using Shared.Enums.Payments;
using Microsoft.Extensions.Options;

namespace Payment.Application.Payments;

public sealed record CreatePaymentCommand(Guid TripId, Guid RiderId, Guid DriverId, decimal Amount,
    PaymentMethod PaymentMethod, Guid IdempotencyKey, Guid CorrelationId,
    string? GatewayToken = null, string Currency = "VND") : ICommand<PaymentView>;

public sealed record GetPaymentQuery(Guid PaymentId) : IQuery<PaymentView>;
public sealed record RefundPaymentCommand(Guid PaymentId, Guid IdempotencyKey, string Reason) : ICommand<RefundView>;

public sealed record RefundView(Guid Id, Guid PaymentId, decimal Amount, string Status,
    string? FailureReason, bool NeedsReconciliation, bool Created = false)
{
    public static RefundView From(Refund refund, bool created = false)
        => new(refund.Id, refund.PaymentId, refund.Amount, refund.Status.ToString(), null,
            refund.Status == Payment.Domain.Enums.RefundStatus.Pending, created);
}

public sealed record PaymentView(Guid Id, Guid TripId, Guid RiderId, Guid DriverId, decimal Amount,
    string Currency, string Status, string? FailureReason, bool NeedsReconciliation, bool Created = false)
{
    public static PaymentView From(PaymentTransaction p, bool created = false)
        => new(p.Id, p.TripId, p.RiderId, p.DriverId, p.Amount, p.Currency,
            p.Status.ToString(), p.FailureReason, p.Status == Payment.Domain.Enums.PaymentStatus.Pending, created);
}

public interface IPaymentRepository
{
    Task<IAsyncDisposable> LockAsync(string resource, CancellationToken ct);
    Task<IAsyncDisposable> BeginAsync(CancellationToken ct);
    Task<PaymentTransaction?> FindAsync(Guid id, CancellationToken ct);
    Task<PaymentTransaction?> FindByTripAsync(Guid tripId, CancellationToken ct);
    Task<PaymentTransaction?> FindByKeyAsync(Guid key, CancellationToken ct);
    Task<IReadOnlyList<Wallet>> LockWalletsAsync(IEnumerable<Guid> users, CancellationToken ct);
    void Add(PaymentTransaction payment);
    void AddRefund(Refund refund);
    void AddLedger(LedgerEntry entry);
    Task SaveAsync(CancellationToken ct);
    Task CommitAsync(CancellationToken ct);
}

public sealed record GatewayResult(bool Approved, string Reference);
public interface IPaymentGateway
{
    Task<GatewayResult?> LookupAsync(Guid key, CancellationToken ct);
    Task<GatewayResult> ChargeAsync(Guid key, string token, decimal amount, CancellationToken ct);
}

public interface IPaymentEvents
{
    Task PaymentFinishedAsync(PaymentTransaction payment, CancellationToken ct);
}

public interface IPaymentProcessor
{
    PaymentMethod Method { get; }
    Task<GatewayResult?> ChargeAsync(PaymentTransaction payment, CancellationToken ct);
}

public sealed class PaymentProcessorFactory(IEnumerable<IPaymentProcessor> processors)
{
    public IPaymentProcessor Get(PaymentMethod method) => processors.Single(p => p.Method == method);
}

public sealed class WalletPaymentProcessor : IPaymentProcessor
{
    public PaymentMethod Method => PaymentMethod.Wallet;
    public Task<GatewayResult?> ChargeAsync(PaymentTransaction payment, CancellationToken ct)
        => Task.FromResult<GatewayResult?>(new(true, "wallet"));
}

public sealed class PaymentGatewayOptions
{
    public int TimeoutSeconds { get; set; } = 5;
    public int RetryBaseMilliseconds { get; set; } = 1000;
}

public sealed class CardPaymentProcessor(IPaymentGateway gateway, IOptions<PaymentGatewayOptions> options) : IPaymentProcessor
{
    public PaymentMethod Method => PaymentMethod.Card;

    public Task<GatewayResult?> ChargeAsync(PaymentTransaction payment, CancellationToken ct)
        => ExecuteAsync(payment.IdempotencyKey,
            token => gateway.ChargeAsync(payment.IdempotencyKey, payment.GatewayToken!, payment.Amount, token), ct);

    private async Task<GatewayResult?> ExecuteAsync(Guid key,
        Func<CancellationToken, Task<GatewayResult>> action, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 4; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeout.CancelAfter(TimeSpan.FromSeconds(options.Value.TimeoutSeconds));
                var known = await gateway.LookupAsync(key, timeout.Token);
                return known ?? await action(timeout.Token);
            }
            catch (Exception ex) when (ex is TimeoutException || ex is HttpRequestException ||
                (ex is OperationCanceledException && !ct.IsCancellationRequested))
            {
                if (attempt == 3) return null;
                await Task.Delay(TimeSpan.FromMilliseconds(options.Value.RetryBaseMilliseconds * (1 << attempt) + Random.Shared.Next(501)), ct);
            }
        }
        return null;
    }
}
