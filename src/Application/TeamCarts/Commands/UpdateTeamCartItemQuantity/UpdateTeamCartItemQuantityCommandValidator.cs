using FluentValidation;

namespace YummyZoom.Application.TeamCarts.Commands.UpdateTeamCartItemQuantity;

public sealed class UpdateTeamCartItemQuantityCommandValidator : AbstractValidator<UpdateTeamCartItemQuantityCommand>
{
    public UpdateTeamCartItemQuantityCommandValidator()
    {
        RuleFor(x => x.TeamCartId)
            .NotEmpty();

        RuleFor(x => x.TeamCartItemId)
            .NotEmpty();

        RuleFor(x => x.NewQuantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero");
    }
}

