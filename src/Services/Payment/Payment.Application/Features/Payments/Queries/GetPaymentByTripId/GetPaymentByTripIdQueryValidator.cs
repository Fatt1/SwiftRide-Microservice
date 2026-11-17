using FluentValidation;
namespace Payment.Application.Features.Payments.Queries.GetPaymentByTripId;
public sealed class GetPaymentByTripIdQueryValidator : AbstractValidator<GetPaymentByTripIdQuery>
{
    public GetPaymentByTripIdQueryValidator() => RuleFor(x => x.TripId).NotEmpty();
}
