using Payment.Application.Abstractions;
using Payment.Application.Dtos;
using Payment.Domain.Entities;
using Payment.Domain.Repositories;
using Shared;
using Shared.CQRS;

namespace Payment.Application.Features.Wallets.Queries.GetWalletByUserId;

public sealed record GetWalletByUserIdQuery(Guid UserId) : IQuery<WalletDto>;

public sealed class GetWalletByUserIdQueryHandler(
    IWalletRepository wallets,
    IPaymentTransaction transactions)
    : IQueryHandler<GetWalletByUserIdQuery, WalletDto>
{
    public async Task<Result<WalletDto>> Handle(GetWalletByUserIdQuery request, CancellationToken ct)
    {
        var wallet = await wallets.GetByUserIdAsync(request.UserId, ct);
        if (wallet is null)
        {
            wallet = Wallet.Create(request.UserId);
            wallets.Add(wallet);
            await transactions.SaveChangesAsync();
        }
        return Result.Success(WalletDto.From(wallet));
    }
}
