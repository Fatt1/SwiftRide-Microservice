using Payment.Application.Dtos;
using Shared.CQRS;

namespace Payment.Application.Features.Payments.Queries.GetPaymentById;

public sealed record GetPaymentByIdQuery(Guid PaymentId) : IQuery<PaymentDto>;
