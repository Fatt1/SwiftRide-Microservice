using Payment.Application.Dtos;
using Shared.CQRS;

namespace Payment.Application.Features.Wallets.Commands.TopUpWallet;

public sealed record TopUpWalletCommand(Guid UserId, Guid RequestId, decimal Amount) : ICommand<WalletDto>;
