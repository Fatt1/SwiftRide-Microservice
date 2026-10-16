using Payment.Application.Dtos;
using Payment.Domain.Repositories;
using Shared;
using Shared.CQRS;

namespace Payment.Application.Features.Payments.Queries.GetPayments;

public sealed record GetPaymentsQuery : IQuery<IReadOnlyList<PaymentHistoryDto>>;

public sealed class GetPaymentsQueryHandler(IPaymentRepository payments)
    : IQueryHandler<GetPaymentsQuery, IReadOnlyList<PaymentHistoryDto>>
{
    public async Task<Result<IReadOnlyList<PaymentHistoryDto>>> Handle(GetPaymentsQuery request, CancellationToken ct) =>
        Result.Success<IReadOnlyList<PaymentHistoryDto>>((await payments.GetAllAsync(ct)).Select(PaymentHistoryDto.From).ToList());
}
