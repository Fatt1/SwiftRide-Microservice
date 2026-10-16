using Payment.Application.Dtos;
using Payment.Domain.Repositories;
using Shared;
using Shared.CQRS;

namespace Payment.Application.Features.Ledger.Queries.GetLedger;

public sealed record GetLedgerQuery : IQuery<IReadOnlyList<LedgerEntryDto>>;

public sealed class GetLedgerQueryHandler(ILedgerRepository ledger)
    : IQueryHandler<GetLedgerQuery, IReadOnlyList<LedgerEntryDto>>
{
    public async Task<Result<IReadOnlyList<LedgerEntryDto>>> Handle(GetLedgerQuery request, CancellationToken ct) =>
        Result.Success<IReadOnlyList<LedgerEntryDto>>((await ledger.GetAllAsync(ct)).Select(LedgerEntryDto.From).ToList());
}
