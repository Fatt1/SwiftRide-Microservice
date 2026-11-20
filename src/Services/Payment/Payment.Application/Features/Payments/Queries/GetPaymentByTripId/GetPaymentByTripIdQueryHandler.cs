using Payment.Application.Dtos;
using Payment.Domain.Repositories;
using Shared;
using Shared.CQRS;
namespace Payment.Application.Features.Payments.Queries.GetPaymentByTripId;
public sealed class GetPaymentByTripIdQueryHandler(IPaymentRepository payments)
    : IQueryHandler<GetPaymentByTripIdQuery, IReadOnlyList<PaymentHistoryDto>>
{
    public async Task<Result<IReadOnlyList<PaymentHistoryDto>>> Handle(
        GetPaymentByTripIdQuery request, CancellationToken cancellationToken)
    {
        var history = await payments.GetAllByTripIdAsync(request.TripId, cancellationToken);
        if (history.Count == 0)
            return Result.Failure<IReadOnlyList<PaymentHistoryDto>>(new NotFoundError("Payment for trip", request.TripId));

        return Result.Success<IReadOnlyList<PaymentHistoryDto>>(history.Select(PaymentHistoryDto.From).ToList());
    }
}
