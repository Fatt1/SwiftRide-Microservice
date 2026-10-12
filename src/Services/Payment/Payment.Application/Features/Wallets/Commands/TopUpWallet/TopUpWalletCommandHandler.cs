using Payment.Application.Abstractions;
using Payment.Application.Dtos;
using Payment.Domain.Entities;
using Payment.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared;
using Shared.CQRS;

namespace Payment.Application.Features.Wallets.Commands.TopUpWallet;

public sealed class TopUpWalletCommandHandler(
    IWalletRepository wallets, ILedgerRepository ledger, IPaymentTransaction transactions,
    ILogger<TopUpWalletCommandHandler> logger)
    : ICommandHandler<TopUpWalletCommand, WalletDto>
{
    public async Task<Result<WalletDto>> Handle(TopUpWalletCommand request, CancellationToken ct)
    {
        await using var transaction = await transactions.BeginOwnedTransactionAsync();
        try
        {
            logger.LogInformation("Bắt đầu nạp ví: UserId={UserId}, RequestId={RequestId}, Amount={Amount} VND",
                request.UserId, request.RequestId, request.Amount);
            await transactions.LockPaymentsAsync([request.RequestId], ct);
            await transactions.LockWalletsAsync([request.UserId], ct);

            var wallet = await wallets.GetByUserIdAsync(request.UserId, ct);
            if (wallet is null)
            {
                logger.LogWarning("Nạp ví bị từ chối: UserId={UserId}, RequestId={RequestId}; không tìm thấy ví, số dư không thay đổi",
                    request.UserId, request.RequestId);
                return Result.Failure<WalletDto>(new NotFoundError("Wallet for user", request.UserId));
            }

            var existing = await ledger.GetByIdAsync(request.RequestId, ct);
            if (existing is not null)
            {
                if (existing.PaymentId is not null || existing.WalletId != wallet.Id ||
                    existing.Amount != request.Amount || existing.Description != "Wallet top-up")
                {
                    logger.LogWarning("Nạp ví bị từ chối: UserId={UserId}, RequestId={RequestId}; RequestId đã thuộc giao dịch khác, số dư không thay đổi",
                        request.UserId, request.RequestId);
                    return Result.Failure<WalletDto>(new ConflictError("RequestId belongs to another ledger entry."));
                }
                logger.LogInformation("Yêu cầu nạp ví đã xử lý trước đó: UserId={UserId}, WalletId={WalletId}, RequestId={RequestId}, Balance={Balance} VND; không cộng tiền lần nữa",
                    request.UserId, wallet.Id, request.RequestId, wallet.Balance);
                return Result.Success(WalletDto.From(wallet));
            }

            wallet.Credit(request.Amount);
            ledger.Add(LedgerEntry.CreateTopUp(request.RequestId, wallet.Id, request.Amount));
            if (transaction is not null)
                await transactions.EndTransactionAsync();
            else
                await transactions.SaveChangesAsync();

            logger.LogInformation("Nạp ví đã lưu: UserId={UserId}, WalletId={WalletId}, RequestId={RequestId}, Amount={Amount} VND, NewBalance={Balance} VND; ledger Credit đã tạo",
                request.UserId, wallet.Id, request.RequestId, request.Amount, wallet.Balance);
            return Result.Success(WalletDto.From(wallet));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Nạp ví lỗi: UserId={UserId}, RequestId={RequestId}, Amount={Amount} VND; transaction do handler sở hữu sẽ rollback nếu có",
                request.UserId, request.RequestId, request.Amount);
            if (transaction is not null)
                await transactions.RollBackTransactionAsync();
            throw;
        }
    }
}
