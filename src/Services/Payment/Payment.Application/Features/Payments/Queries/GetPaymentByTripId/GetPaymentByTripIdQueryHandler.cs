using Payment.Application.Dtos;
using Payment.Domain.Repositories;
using Shared;
using Shared.CQRS;
namespace Payment.Application.Features.Payments.Queries.GetPaymentByTripId;
public sealed class GetPaymentByTripIdQueryHandler(IPaymentRepository payments) : IQueryHandler<GetPaymentByTripIdQuery, PaymentHistoryDto>
{
    public async Task<Result<PaymentHistoryDto>> Handle(GetPaymentByTripIdQuery request, CancellationToken ct)
    {
        var payment = await payments.GetByTripIdAsync(request.TripId, ct);
        if (payment is null)
            return Result.Failure<PaymentHistoryDto>(new NotFoundError("Payment for trip", request.TripId));
        return Result.Success(PaymentHistoryDto.From(payment));
    }
}
