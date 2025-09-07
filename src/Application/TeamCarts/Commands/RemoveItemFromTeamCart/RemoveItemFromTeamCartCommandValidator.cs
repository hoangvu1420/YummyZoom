using FluentValidation;

namespace YummyZoom.Application.TeamCarts.Commands.RemoveItemFromTeamCart;

public sealed class RemoveItemFromTeamCartCommandValidator : AbstractValidator<RemoveItemFromTeamCartCommand>
{
    public RemoveItemFromTeamCartCommandValidator()
    {
        RuleFor(x => x.TeamCartId)
            .NotEmpty();

        RuleFor(x => x.TeamCartItemId)
            .NotEmpty();
    }
}

