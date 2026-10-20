using Payment.Application.Dtos;
using Payment.Domain.Repositories;
using Shared;
using Shared.CQRS;

namespace Payment.Application.Features.Wallets.Queries.GetLedgerByUserId;

public sealed record GetLedgerByUserIdQuery(Guid UserId) : IQuery<IReadOnlyList<LedgerEntryDto>>;

public sealed class GetLedgerByUserIdQueryHandler(IWalletRepository wallets, ILedgerRepository ledger)
    : IQueryHandler<GetLedgerByUserIdQuery, IReadOnlyList<LedgerEntryDto>>
{
    public async Task<Result<IReadOnlyList<LedgerEntryDto>>> Handle(GetLedgerByUserIdQuery request, CancellationToken ct)
    {
        var wallet = await wallets.GetByUserIdAsync(request.UserId, ct);
        if (wallet is null)
        {
            return Result.Success<IReadOnlyList<LedgerEntryDto>>(Array.Empty<LedgerEntryDto>());
        }

        var entries = await ledger.GetAllAsync(ct);
        var filtered = entries
            .Where(e => e.WalletId == wallet.Id)
            .OrderByDescending(e => e.CreatedAt)
            .Select(LedgerEntryDto.From)
            .ToList();

        return Result.Success<IReadOnlyList<LedgerEntryDto>>(filtered);
    }
}
