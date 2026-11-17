using FluentValidation;
namespace Payment.Application.Features.Payments.Queries.GetPaymentById;
public sealed class GetPaymentByIdQueryValidator : AbstractValidator<GetPaymentByIdQuery>
{
    public GetPaymentByIdQueryValidator() => RuleFor(x => x.PaymentId).NotEmpty();
}
