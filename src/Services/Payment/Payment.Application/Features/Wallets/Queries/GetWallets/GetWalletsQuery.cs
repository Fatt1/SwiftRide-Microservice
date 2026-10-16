using Payment.Application.Dtos;
using Payment.Domain.Repositories;
using Shared;
using Shared.CQRS;

namespace Payment.Application.Features.Wallets.Queries.GetWallets;

public sealed record GetWalletsQuery : IQuery<IReadOnlyList<WalletDto>>;

public sealed class GetWalletsQueryHandler(IWalletRepository wallets)
    : IQueryHandler<GetWalletsQuery, IReadOnlyList<WalletDto>>
{
    public async Task<Result<IReadOnlyList<WalletDto>>> Handle(GetWalletsQuery request, CancellationToken ct) =>
        Result.Success<IReadOnlyList<WalletDto>>((await wallets.GetAllAsync(ct)).Select(WalletDto.From).ToList());
}
