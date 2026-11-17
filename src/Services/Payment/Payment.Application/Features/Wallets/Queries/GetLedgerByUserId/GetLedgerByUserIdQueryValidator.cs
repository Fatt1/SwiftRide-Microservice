using FluentValidation;
namespace Payment.Application.Features.Wallets.Queries.GetLedgerByUserId;
public sealed class GetLedgerByUserIdQueryValidator : AbstractValidator<GetLedgerByUserIdQuery>
{
    public GetLedgerByUserIdQueryValidator() => RuleFor(x => x.UserId).NotEmpty();
}
