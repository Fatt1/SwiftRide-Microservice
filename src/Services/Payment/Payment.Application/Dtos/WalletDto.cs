using Payment.Domain.Entities;

namespace Payment.Application.Dtos;

public sealed record WalletDto(Guid Id, Guid UserId, decimal Balance, string Currency)
{
    public static WalletDto From(Wallet wallet) =>
        new(wallet.Id, wallet.UserId, wallet.Balance, wallet.Currency);
}
