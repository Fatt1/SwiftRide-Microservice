using Payment.Application.Abstractions;
using Payment.Application.Dtos;
using Payment.Application.Payments;
using Payment.Domain.Entities;
using Payment.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared;
using Shared.CQRS;

namespace Payment.Application.Features.Wallets.Commands.TopUpWallet;

public sealed class TopUpWalletCommandHandler(
    IWalletRepository wallets, ILedgerRepository ledger, IPaymentRepository payments, IPaymentTransaction transactions,
    LedgerPostingService ledgerPosting,
    ILogger<TopUpWalletCommandHandler> logger)
    : ICommandHandler<TopUpWalletCommand, WalletDto>
{
    public async Task<Result<WalletDto>> Handle(TopUpWalletCommand request, CancellationToken ct)
    {
        await using var transaction = await transactions.BeginOwnedTransactionAsync();
        try
        {
            logger.LogInformation("Bắt đầu nạp ví: UserId={UserId}, IdempotencyKey={IdempotencyKey}, Amount={Amount} VND",
                request.UserId, request.IdempotencyKey, request.Amount);
            await transactions.LockPaymentsAsync([request.IdempotencyKey], ct);
            await transactions.LockWalletsAsync([request.UserId], ct);

            var wallet = await wallets.GetByUserIdAsync(request.UserId, ct);
            if (wallet is null)
            {
                logger.LogWarning("Nạp ví bị từ chối: UserId={UserId}, IdempotencyKey={IdempotencyKey}; không tìm thấy ví, số dư không thay đổi",
                    request.UserId, request.IdempotencyKey);
                return Result.Failure<WalletDto>(new NotFoundError("Wallet for user", request.UserId));
            }

            var existingPayment = await payments.GetByIdempotencyKeyAsync(request.IdempotencyKey, ct);
            var existing = await ledger.GetByIdAsync(request.IdempotencyKey, ct);
            if (existingPayment is not null || existing is not null)
            {
                if (existingPayment is null || existingPayment.Type != Payment.Domain.Enums.PaymentType.WalletTopUp ||
                    existingPayment.RiderId != request.UserId || existingPayment.Amount != request.Amount ||
                    existing is null || existing.PaymentId != existingPayment.Id || existing.WalletId != wallet.Id ||
                    existing.Amount != request.Amount || existing.Description != "Wallet top-up")
                {
                    logger.LogWarning("Nạp ví bị từ chối: UserId={UserId}, IdempotencyKey={IdempotencyKey}; IdempotencyKey đã thuộc giao dịch khác, số dư không thay đổi",
                        request.UserId, request.IdempotencyKey);
                    return Result.Failure<WalletDto>(new ConflictError("IdempotencyKey belongs to another ledger entry."));
                }
                logger.LogInformation("Yêu cầu nạp ví đã xử lý trước đó: UserId={UserId}, WalletId={WalletId}, IdempotencyKey={IdempotencyKey}, Balance={Balance} VND; không cộng tiền lần nữa",
                    request.UserId, wallet.Id, request.IdempotencyKey, wallet.Balance);
                return Result.Success(WalletDto.From(wallet));
            }

            var payment = PaymentTransaction.CreateTopUp(request.UserId, request.IdempotencyKey, request.Amount);
            payments.Add(payment);
            ledgerPosting.RecordTopUp(request.IdempotencyKey, payment.Id, wallet, request.Amount);
            if (transaction is not null)
                await transactions.EndTransactionAsync();
            else
                await transactions.SaveChangesAsync();

            logger.LogInformation("Nạp ví đã lưu: UserId={UserId}, WalletId={WalletId}, IdempotencyKey={IdempotencyKey}, Amount={Amount} VND, NewBalance={Balance} VND; ledger Credit đã tạo",
                request.UserId, wallet.Id, request.IdempotencyKey, request.Amount, wallet.Balance);
            return Result.Success(WalletDto.From(wallet));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Nạp ví lỗi: UserId={UserId}, IdempotencyKey={IdempotencyKey}, Amount={Amount} VND; transaction do handler sở hữu sẽ rollback nếu có",
                request.UserId, request.IdempotencyKey, request.Amount);
            if (transaction is not null)
                await transactions.RollBackTransactionAsync();
            throw;
        }
    }
}
