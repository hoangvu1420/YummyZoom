using FluentValidation;

namespace YummyZoom.Application.TeamCarts.Commands.JoinTeamCart;

public sealed class JoinTeamCartCommandValidator : AbstractValidator<JoinTeamCartCommand>
{
    public JoinTeamCartCommandValidator()
    {
        RuleFor(x => x.TeamCartId)
            .NotEmpty();

        RuleFor(x => x.ShareToken)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.GuestName)
            .NotEmpty()
            .MaximumLength(200);
    }
}

