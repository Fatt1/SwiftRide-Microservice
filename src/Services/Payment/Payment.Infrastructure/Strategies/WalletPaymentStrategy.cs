using Payment.Application.Abstractions;
using Payment.Application.Payments.Contexts;
using Payment.Domain.Repositories;
using Shared;
using Shared.Enums.Payments;

namespace Payment.Infrastructure.Strategies;

public sealed class WalletPaymentStrategy(IWalletRepository wallets) : IPaymentStrategy
{
    public PaymentMethod Method => PaymentMethod.Wallet;

    public async Task<Result> ChargeAsync(PaymentExecutionContext context, CancellationToken ct)
    {
        var payment = context.Payment;
        var rider = await wallets.GetByUserIdAsync(payment.RiderId, ct);
        var driver = await wallets.GetByUserIdAsync(payment.DriverId, ct);
        if (rider is null || rider.Currency != payment.Currency)
            return Result.Failure(new ConflictError("A rider wallet with matching currency is required."));
        if (rider.Balance < payment.Amount)
            return Result.Failure(new ConflictError("Insufficient rider wallet balance."));
        if (driver is null)
            return Result.Failure(new ConflictError("A driver wallet is required."));
        if (driver.Currency != payment.Currency)
            return Result.Failure(new ConflictError("Driver wallet currency does not match."));
        rider.Debit(payment.Amount);
        driver.Credit(payment.Amount);
        context.DebitWalletId = rider.Id;
        context.CreditWalletId = driver.Id;
        return Result.Success();
    }

    public async Task<Result> RefundAsync(RefundExecutionContext context, CancellationToken ct)
    {
        var payment = context.Payment;
        var driver = await wallets.GetByUserIdAsync(payment.DriverId, ct);
        var rider = await wallets.GetByUserIdAsync(payment.RiderId, ct);
        if (driver is null || driver.Currency != payment.Currency ||
            driver.Balance < payment.Amount)
            return Result.Failure(new ConflictError("Driver wallet is missing, incompatible or has insufficient balance."));
        if (rider is null || rider.Currency != payment.Currency)
            return Result.Failure(new ConflictError("A rider wallet with matching currency is required."));
        driver.Debit(payment.Amount);
        rider.Credit(payment.Amount);
        context.DebitWalletId = driver.Id;
        context.CreditWalletId = rider.Id;
        return Result.Success();
    }
}
