using Payment.Application.Dtos;
using Payment.Domain.Repositories;
using Shared;
using Shared.CQRS;

namespace Payment.Application.Features.Ledger.Queries.GetLedgerByPaymentId;

public sealed class GetLedgerByPaymentIdQueryHandler(ILedgerRepository ledger, IPaymentRepository payments)
    : IQueryHandler<GetLedgerByPaymentIdQuery, IReadOnlyList<LedgerEntryDto>>
{
    public async Task<Result<IReadOnlyList<LedgerEntryDto>>> Handle(GetLedgerByPaymentIdQuery request, CancellationToken ct)
    {
        if (await payments.GetByIdAsync(request.PaymentId, ct) is null)
            return Result.Failure<IReadOnlyList<LedgerEntryDto>>(new NotFoundError("Payment", request.PaymentId));

        var entries = await ledger.GetByPaymentIdAsync(request.PaymentId, ct);
        return Result.Success<IReadOnlyList<LedgerEntryDto>>(entries.Select(LedgerEntryDto.From).ToList());
    }
}
