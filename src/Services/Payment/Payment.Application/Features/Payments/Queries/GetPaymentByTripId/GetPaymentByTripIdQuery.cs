using Payment.Application.Dtos;
using Shared.CQRS;

namespace Payment.Application.Features.Payments.Queries.GetPaymentByTripId;

public sealed record GetPaymentByTripIdQuery(Guid TripId) : IQuery<PaymentHistoryDto>;
