using Microsoft.Extensions.Logging;
using Payment.Application.Abstractions;
using Payment.Application.Payments;
using Payment.Application.Payments.Contexts;
using Payment.Application.Payments.Results;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Domain.Repositories;
using Shared;
using Shared.Enums.Payments;

namespace Payment.Infrastructure.Strategies;

public sealed class CardPaymentStrategy(IWalletRepository wallets, ICardPaymentProcessor processor,
    LedgerPostingService ledgerPosting, ILogger<CardPaymentStrategy> logger) : IPaymentStrategy
{
    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromMilliseconds(200),
        TimeSpan.FromMilliseconds(500),
        TimeSpan.FromSeconds(1)
    ];

    public PaymentMethod Method => PaymentMethod.Card;

    public async Task<Result> ChargeAsync(PaymentExecutionContext context, CancellationToken ct)
    {
        var payment = context.Payment;
        var driverWallet = await wallets.GetByUserIdAsync(payment.DriverId!.Value, ct);
        if (driverWallet is null)
            return Result.Failure(new ConflictError("A driver wallet is required."));
        if (driverWallet.Currency != payment.Currency)
            return Result.Failure(new ConflictError("Driver wallet currency does not match."));

        var result = await ChargeGatewayAsync(payment, ct);
        context.ProcessorResponse = result.Response;
        if (!result.IsSuccess)
            return Result.Failure(new ConflictError(result.FailureReason!));
        ledgerPosting.Record(payment.Id, null, payment.PaymentMethod, EntryType.Debit, payment.Amount, "Payment charge");
        ledgerPosting.Record(payment.Id, driverWallet, payment.PaymentMethod, EntryType.Credit, payment.Amount, "Driver earnings");
        return Result.Success();
    }

    private async Task<CardPaymentResult> ChargeGatewayAsync(PaymentTransaction payment, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            return await processor.ChargeAsync(payment.Amount, payment.GatewayToken!, payment.IdempotencyKey, 0, ct);
        }
        catch (TransientCardPaymentException gatewayError)
        {
            for (var retryAttempt = 1; retryAttempt <= RetryDelays.Length; retryAttempt++)
            {
                var delay = RetryDelays[retryAttempt - 1];
                logger.LogWarning(gatewayError, "Sẽ gọi lại cổng thanh toán: TripId={TripId}, IdempotencyKey={IdempotencyKey}, GatewayRetryAttempt={GatewayRetryAttempt}, DelayMs={DelayMs}",
                    payment.TripId, payment.IdempotencyKey, retryAttempt, delay.TotalMilliseconds);
                await Task.Delay(delay, ct);
                ct.ThrowIfCancellationRequested();

                try
                {
                    return await processor.ChargeAsync(payment.Amount, payment.GatewayToken!, payment.IdempotencyKey, retryAttempt, ct);
                }
                catch (TransientCardPaymentException retryError) when (retryAttempt < RetryDelays.Length)
                {
                    gatewayError = retryError;
                }
            }

            throw;
        }
    }

    public async Task<Result> RefundAsync(RefundExecutionContext context, CancellationToken ct)
    {
        var payment = context.Payment;
        var driverWallet = await wallets.GetByUserIdAsync(payment.DriverId!.Value, ct);
        if (driverWallet is null || driverWallet.Currency != payment.Currency ||
            driverWallet.Balance < payment.Amount)
            return Result.Failure(new ConflictError("Driver wallet is missing, incompatible or has insufficient balance."));

        var result = await processor.RefundAsync(payment.Amount, payment.GatewayToken!, payment.Id, ct);
        if (!result.IsSuccess)
            return Result.Failure(new ConflictError(result.FailureReason!));
        ledgerPosting.Record(payment.Id, driverWallet, payment.PaymentMethod, EntryType.Debit, payment.Amount, "Refund: reverse driver earnings");
        ledgerPosting.Record(payment.Id, null, payment.PaymentMethod, EntryType.Credit, payment.Amount, "Refund: return rider payment");
        return Result.Success();
    }
}
