using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Shared;
using Shared.CQRS;
using Shared.Enums.Payments;

namespace Payment.Application.Payments;

public sealed class CreatePaymentHandler(IPaymentRepository repository, PaymentProcessorFactory factory,
    IPaymentEvents events) : ICommandHandler<CreatePaymentCommand, PaymentView>
{
    public async Task<Result<PaymentView>> Handle(CreatePaymentCommand request, CancellationToken ct)
    {
        using var activity = PaymentTelemetry.Activities.StartActivity("payment.create");
        activity?.SetTag("correlation.id", request.CorrelationId);
        // Session locks serialize replays across API instances, including calls outside a DB transaction.
        await using var keyLock = await repository.LockAsync($"payment-key:{request.IdempotencyKey}", ct);
        await using var tripLock = await repository.LockAsync($"trip:{request.TripId}", ct);
        var payment = await repository.FindByKeyAsync(request.IdempotencyKey, ct);
        if (payment is not null && !Matches(payment, request))
            return Result.Failure<PaymentView>(new ConflictError("Idempotency key has a different payload."));
        if (payment is null && await repository.FindByTripAsync(request.TripId, ct) is not null)
            return Result.Failure<PaymentView>(new ConflictError("Trip already has a payment with another key."));
        var created = payment is null;
        payment ??= PaymentTransaction.Create(request.TripId, request.RiderId, request.DriverId,
            request.Amount, request.PaymentMethod, request.IdempotencyKey, request.GatewayToken,
            request.Currency);
        await using var paymentLock = await repository.LockAsync($"payment:{payment.Id}", ct);
        if (payment.Status != PaymentStatus.Pending) return Result.Success(PaymentView.From(payment));

        await using (var tx = await repository.BeginAsync(ct))
        {
            if (created) repository.Add(payment);
            var wallets = await repository.LockWalletsAsync(new[] { payment.RiderId, payment.DriverId }, ct);
            var driver = wallets.SingleOrDefault(w => w.UserId == payment.DriverId && w.UserRole == "driver" && w.Currency == "VND");
            var rider = wallets.SingleOrDefault(w => w.UserId == payment.RiderId && w.UserRole == "rider" && w.Currency == "VND");
            if (driver is null || (payment.PaymentMethod == PaymentMethod.Wallet && rider is null))
                await FailAsync(payment, "Required wallet is missing or invalid.", ct);
            else if (payment.PaymentMethod == PaymentMethod.Wallet)
            {
                if (rider!.Balance < payment.Amount) await FailAsync(payment, "Insufficient balance.", ct);
                else
                {
                    rider.Debit(payment.Amount);
                    driver.Credit(payment.Amount);
                    AddLedger(payment, rider.Id, driver.Id);
                    payment.MarkCompleted();
                    await events.PaymentFinishedAsync(payment, ct);
                }
            }
            await repository.CommitAsync(ct);
        }
        if (payment.PaymentMethod == PaymentMethod.Card && payment.Status == PaymentStatus.Pending)
        {
            var outcome = await factory.Get(payment.PaymentMethod).ChargeAsync(payment, ct);
            await using var tx = await repository.BeginAsync(ct);
            if (outcome is { Approved: false }) await FailAsync(payment, "Card declined.", ct);
            else if (outcome is { Approved: true })
            {
                var wallets = await repository.LockWalletsAsync(new[] { payment.DriverId }, ct);
                var driver = wallets.Single(w => w.UserId == payment.DriverId);
                driver.Credit(payment.Amount);
                AddLedger(payment, null, driver.Id);
                payment.MarkCompleted(System.Text.Json.JsonSerializer.Serialize(outcome.Reference));
                await events.PaymentFinishedAsync(payment, ct);
            }
            await repository.CommitAsync(ct);
        }
        PaymentTelemetry.Record("payment", payment.Status.ToString());
        return Result.Success(PaymentView.From(payment, created));
    }

    private async Task FailAsync(PaymentTransaction payment, string reason, CancellationToken ct)
    {
        payment.MarkFailed(reason);
        await events.PaymentFinishedAsync(payment, ct);
    }

    private void AddLedger(PaymentTransaction payment, Guid? debit, Guid credit)
    {
        repository.AddLedger(LedgerEntry.Create(payment.Id, debit, payment.PaymentMethod, EntryType.Debit, payment.Amount));
        repository.AddLedger(LedgerEntry.Create(payment.Id, credit, payment.PaymentMethod, EntryType.Credit, payment.Amount));
    }

    private static bool Matches(PaymentTransaction p, CreatePaymentCommand r)
        => p.TripId == r.TripId && p.RiderId == r.RiderId && p.DriverId == r.DriverId && p.Amount == r.Amount
            && p.Currency == r.Currency && p.PaymentMethod == r.PaymentMethod && p.GatewayToken == r.GatewayToken;
}

public sealed class RefundPaymentHandler(IPaymentRepository repository,
    IPaymentEvents events) : ICommandHandler<RefundPaymentCommand, RefundView>
{
    public async Task<Result<RefundView>> Handle(RefundPaymentCommand request, CancellationToken ct)
    {
        using var activity = PaymentTelemetry.Activities.StartActivity("payment.refund");
        await using var keyLock = await repository.LockAsync($"refund-key:{request.IdempotencyKey}", ct);
        await using var paymentLock = await repository.LockAsync($"payment:{request.PaymentId}", ct);
        var payment = await repository.FindAsync(request.PaymentId, ct);
        if (payment is null) return Result.Failure<RefundView>(new NotFoundError("Payment", request.PaymentId));
        if (payment.PaymentMethod == PaymentMethod.Card)
            return Result.Failure<RefundView>(new ConflictError("Card refunds are not supported with the current schema."));
        var refund = payment.Refunds.SingleOrDefault(r => r.Status is RefundStatus.Pending or RefundStatus.Completed);
        if (refund is not null && refund.Reason != request.Reason)
            return Result.Failure<RefundView>(new ConflictError("Payment already has a refund with another reason."));
        if (refund?.Status == RefundStatus.Completed) return Result.Success(RefundView.From(refund));
        if (payment.Status != PaymentStatus.Completed)
            return Result.Failure<RefundView>(new ConflictError("Payment is not eligible for a refund."));
        var created = refund is null;
        await using var tx = await repository.BeginAsync(ct);
        var wallets = await repository.LockWalletsAsync(new[] { payment.RiderId, payment.DriverId }, ct);
        var driver = wallets.SingleOrDefault(w => w.UserId == payment.DriverId && w.UserRole == "driver" && w.Currency == "VND");
        var rider = wallets.SingleOrDefault(w => w.UserId == payment.RiderId && w.UserRole == "rider" && w.Currency == "VND");
        if (driver is null || rider is null)
            return Result.Failure<RefundView>(new ConflictError("Required wallet is missing or invalid."));
        if (driver.Balance < payment.Amount)
            return Result.Failure<RefundView>(new ConflictError("Driver has insufficient balance; manual compensation required."));
        refund ??= payment.RequestRefund(request.Reason);
        if (created) repository.AddRefund(refund);
        driver.Debit(refund.Amount);
        rider.Credit(refund.Amount);
        await CompleteAsync(payment, refund, driver.Id, rider.Id, ct);
        await repository.CommitAsync(ct);
        PaymentTelemetry.Record("refund", refund.Status.ToString());
        return Result.Success(RefundView.From(refund, created));
    }

    private async Task CompleteAsync(PaymentTransaction payment, Refund refund, Guid debit, Guid? credit, CancellationToken ct)
    {
        repository.AddLedger(LedgerEntry.Create(payment.Id, debit, payment.PaymentMethod, EntryType.Debit,
            refund.Amount, "Refund"));
        repository.AddLedger(LedgerEntry.Create(payment.Id, credit, payment.PaymentMethod, EntryType.Credit,
            refund.Amount, "Refund", refund.Id));
        refund.MarkCompleted();
        payment.MarkRefunded();
        await events.RefundFinishedAsync(payment, refund, ct);
    }
}

public sealed class GetPaymentHandler(IPaymentRepository repository) : IQueryHandler<GetPaymentQuery, PaymentView>
{
    public async Task<Result<PaymentView>> Handle(GetPaymentQuery request, CancellationToken ct)
    {
        var payment = await repository.FindAsync(request.PaymentId, ct);
        if (payment is null) return Result.Failure<PaymentView>(new NotFoundError("Payment", request.PaymentId));
        return Result.Success(PaymentView.From(payment));
    }
}
