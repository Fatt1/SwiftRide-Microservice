using MediatR;
using Payment.Application.Abstractions;
using Payment.Application.Dtos;
using Payment.Application.Features.Payments.Commands.CreatePayment;
using Payment.Domain.Enums;
using Payment.Domain.Repositories;
using Shared;
using Shared.CQRS;

namespace Payment.Application.Features.Payments.Commands.RetryPayment;

public sealed class RetryPaymentCommandHandler(IPaymentRepository payments, IPaymentTransaction transactions, ISender sender)
    : ICommandHandler<RetryPaymentCommand, PaymentDto>
{
    public async Task<Result<PaymentDto>> Handle(RetryPaymentCommand request, CancellationToken cancellationToken)
    {


        var history = await payments.GetAllByTripIdAsync(request.TripId, cancellationToken);
        if (history.Count == 0)
            return Result.Failure<PaymentDto>(new NotFoundError("Payment for trip", request.TripId));

        if (history.Any(payment => payment.Status is PaymentStatus.Completed or PaymentStatus.Refunded))
            return Result.Failure<PaymentDto>(new ConflictError("Trip has already been paid."));

        if (history.Any(payment => payment.Status == PaymentStatus.Pending))
            return Result.Failure<PaymentDto>(new ConflictError("Trip has a payment that is still pending."));

        // The repository returns newest first.
        var originalPayment = history[^1];
        if (originalPayment.DriverId is not Guid driverId || originalPayment.Currency != request.Currency)
            return Result.Failure<PaymentDto>(new ConflictError("The original payment has incompatible details."));

        var result = await sender.Send(new CreatePaymentCommand
        {
            TripId = request.TripId,
            RiderId = originalPayment.RiderId,
            DriverId = driverId,
            Amount = originalPayment.Amount,
            Currency = originalPayment.Currency,
            PaymentMethod = request.PaymentMethod,
            IdempotencyKey = request.IdempotencyKey,
            GatewayToken = request.GatewayToken,


        }, cancellationToken);



        return result;
    }
}
