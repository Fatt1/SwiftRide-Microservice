using Payment.Application.Dtos;
using Shared.CQRS;

namespace Payment.Application.Features.Ledger.Queries.GetLedgerByPaymentId;

public sealed record GetLedgerByPaymentIdQuery(Guid PaymentId) : IQuery<IReadOnlyList<LedgerEntryDto>>;
