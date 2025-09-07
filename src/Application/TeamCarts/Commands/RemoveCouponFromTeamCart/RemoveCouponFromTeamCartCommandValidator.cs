using FluentValidation;

namespace YummyZoom.Application.TeamCarts.Commands.RemoveCouponFromTeamCart;

public sealed class RemoveCouponFromTeamCartCommandValidator : AbstractValidator<RemoveCouponFromTeamCartCommand>
{
    public RemoveCouponFromTeamCartCommandValidator()
    {
        RuleFor(x => x.TeamCartId).NotEmpty();
    }
}


