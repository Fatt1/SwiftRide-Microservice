using Payment.Application.Abstractions;
using Payment.Application.Payments;
using Payment.Application.Payments.Contexts;
using Payment.Domain.Enums;
using Payment.Domain.Repositories;
using Shared;
using Shared.Enums.Payments;

namespace Payment.Infrastructure.Strategies;

public sealed class CardPaymentStrategy(IWalletRepository wallets, ICardPaymentProcessor processor,
    LedgerPostingService ledgerPosting) : IPaymentStrategy
{
    public PaymentMethod Method => PaymentMethod.Card;

    public async Task<Result> ChargeAsync(PaymentExecutionContext context, CancellationToken ct)
    {
        var payment = context.Payment;
        var driverWallet = await wallets.GetByUserIdAsync(payment.DriverId!.Value, ct);
        if (driverWallet is null)
            return Result.Failure(new ConflictError("A driver wallet is required."));
        if (driverWallet.Currency != payment.Currency)
            return Result.Failure(new ConflictError("Driver wallet currency does not match."));

        var result = await processor.ChargeAsync(payment.Amount, payment.GatewayToken!, payment.IdempotencyKey, context.RetryAttempt, ct);
        context.ProcessorResponse = result.Response;
        if (!result.IsSuccess)
            return Result.Failure(new ConflictError(result.FailureReason!));
        ledgerPosting.Record(payment.Id, null, payment.PaymentMethod, EntryType.Debit, payment.Amount, "Payment charge");
        ledgerPosting.Record(payment.Id, driverWallet, payment.PaymentMethod, EntryType.Credit, payment.Amount, "Driver earnings");
        return Result.Success();
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
