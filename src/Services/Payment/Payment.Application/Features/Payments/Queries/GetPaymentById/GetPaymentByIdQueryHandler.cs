using Payment.Application.Dtos;
using Payment.Domain.Repositories;
using Shared;
using Shared.CQRS;

namespace Payment.Application.Features.Payments.Queries.GetPaymentById;

public sealed class GetPaymentByIdQueryHandler(IPaymentRepository payments)
    : IQueryHandler<GetPaymentByIdQuery, PaymentDto>
{
    public async Task<Result<PaymentDto>> Handle(GetPaymentByIdQuery request, CancellationToken cancellationToken)
    {
        var payment = await payments.GetByIdAsync(request.PaymentId, cancellationToken);
        return payment is null
            ? Result.Failure<PaymentDto>(new NotFoundError("Payment", request.PaymentId))
            : Result.Success(PaymentDto.From(payment));
    }
}
