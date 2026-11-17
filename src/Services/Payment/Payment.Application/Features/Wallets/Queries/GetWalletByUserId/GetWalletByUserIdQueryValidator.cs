using FluentValidation;
namespace Payment.Application.Features.Wallets.Queries.GetWalletByUserId;
public sealed class GetWalletByUserIdQueryValidator : AbstractValidator<GetWalletByUserIdQuery>
{
    public GetWalletByUserIdQueryValidator() => RuleFor(x => x.UserId).NotEmpty();
}
