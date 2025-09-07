using FluentValidation;

namespace YummyZoom.Application.TeamCarts.Commands.ApplyTipToTeamCart;

public sealed class ApplyTipToTeamCartCommandValidator : AbstractValidator<ApplyTipToTeamCartCommand>
{
    public ApplyTipToTeamCartCommandValidator()
    {
        RuleFor(x => x.TeamCartId).NotEmpty();
        RuleFor(x => x.TipAmount)
            .GreaterThanOrEqualTo(0m);
    }
}

