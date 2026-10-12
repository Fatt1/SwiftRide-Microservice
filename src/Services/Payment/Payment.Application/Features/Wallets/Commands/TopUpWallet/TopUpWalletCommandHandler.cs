using Payment.Application.Abstractions;
using Payment.Application.Dtos;
using Payment.Domain.Entities;
using Payment.Domain.Repositories;
using Shared;
using Shared.CQRS;

namespace Payment.Application.Features.Wallets.Commands.TopUpWallet;

public sealed class TopUpWalletCommandHandler(
    IWalletRepository wallets, ILedgerRepository ledger, IPaymentTransaction transactions)
    : ICommandHandler<TopUpWalletCommand, WalletDto>
{
    public async Task<Result<WalletDto>> Handle(TopUpWalletCommand request, CancellationToken ct)
    {
        await using var transaction = await transactions.BeginOwnedTransactionAsync();
        try
        {
            await transactions.LockPaymentsAsync([request.RequestId], ct);
            await transactions.LockWalletsAsync([request.UserId], ct);

            var wallet = await wallets.GetByUserIdAsync(request.UserId, ct);
            if (wallet is null)
                return Result.Failure<WalletDto>(new NotFoundError("Wallet for user", request.UserId));

            var existing = await ledger.GetByIdAsync(request.RequestId, ct);
            if (existing is not null)
            {
                if (existing.PaymentId is not null || existing.WalletId != wallet.Id ||
                    existing.Amount != request.Amount || existing.Description != "Wallet top-up")
                    return Result.Failure<WalletDto>(new ConflictError("RequestId belongs to another ledger entry."));
                return Result.Success(WalletDto.From(wallet));
            }

            wallet.Credit(request.Amount);
            ledger.Add(LedgerEntry.CreateTopUp(request.RequestId, wallet.Id, request.Amount));
            if (transaction is not null)
                await transactions.EndTransactionAsync();
            else
                await transactions.SaveChangesAsync();

            return Result.Success(WalletDto.From(wallet));
        }
        catch
        {
            if (transaction is not null)
                await transactions.RollBackTransactionAsync();
            throw;
        }
    }
}
