using Payment.Application.Abstractions;
using Payment.Application.Payments.Contexts;
using Payment.Domain.Repositories;
using Shared;
using Shared.Enums.Payments;

namespace Payment.Infrastructure.Strategies;

public sealed class CardPaymentStrategy(IWalletRepository wallets, ICardPaymentProcessor processor) : IPaymentStrategy
{
    public PaymentMethod Method => PaymentMethod.Card;

    public async Task<Result> ChargeAsync(PaymentExecutionContext context, CancellationToken ct)
    {
        var payment = context.Payment;
        var driver = await wallets.GetByUserIdAsync(payment.DriverId, ct);
        if (driver is null)
            return Result.Failure(new ConflictError("A driver wallet is required."));
        if (driver.Currency != payment.Currency)
            return Result.Failure(new ConflictError("Driver wallet currency does not match."));

        var result = await processor.ChargeAsync(payment.Amount, payment.GatewayToken!, payment.IdempotencyKey, ct);
        context.ProcessorResponse = result.Response;
        if (!result.IsSuccess)
            return Result.Failure(new ConflictError(result.FailureReason!));
        driver.Credit(payment.Amount);
        context.CreditWalletId = driver.Id;
        return Result.Success();
    }

    public async Task<Result> RefundAsync(RefundExecutionContext context, CancellationToken ct)
    {
        var payment = context.Payment;
        var driver = await wallets.GetByUserIdAsync(payment.DriverId, ct);
        if (driver is null || driver.Currency != payment.Currency ||
            driver.Balance < payment.Amount)
            return Result.Failure(new ConflictError("Driver wallet is missing, incompatible or has insufficient balance."));

        var result = await processor.RefundAsync(payment.Amount, payment.GatewayToken!, payment.Id, ct);
        if (!result.IsSuccess)
            return Result.Failure(new ConflictError(result.FailureReason!));
        driver.Debit(payment.Amount);
        context.DebitWalletId = driver.Id;
        return Result.Success();
    }
}
