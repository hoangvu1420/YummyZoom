using FluentValidation;

namespace YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;

public sealed class AddItemToTeamCartCommandValidator : AbstractValidator<AddItemToTeamCartCommand>
{
    public AddItemToTeamCartCommandValidator()
    {
        RuleFor(x => x.TeamCartId)
            .NotEmpty();

        RuleFor(x => x.MenuItemId)
            .NotEmpty();

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero");

        When(x => x.SelectedCustomizations != null, () =>
        {
            RuleForEach(x => x.SelectedCustomizations!)
                .ChildRules(sel =>
                {
                    sel.RuleFor(s => s.GroupId).NotEmpty();
                    sel.RuleFor(s => s.ChoiceId).NotEmpty();
                });
        });
    }
}

