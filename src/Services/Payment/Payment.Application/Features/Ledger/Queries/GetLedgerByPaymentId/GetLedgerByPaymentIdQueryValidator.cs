using FluentValidation;
namespace Payment.Application.Features.Ledger.Queries.GetLedgerByPaymentId;
public sealed class GetLedgerByPaymentIdQueryValidator : AbstractValidator<GetLedgerByPaymentIdQuery>
{
    public GetLedgerByPaymentIdQueryValidator() => RuleFor(x => x.PaymentId).NotEmpty();
}
