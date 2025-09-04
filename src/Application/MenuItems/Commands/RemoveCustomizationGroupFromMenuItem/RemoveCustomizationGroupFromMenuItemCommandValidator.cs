namespace YummyZoom.Application.MenuItems.Commands.RemoveCustomizationGroupFromMenuItem;

public sealed class RemoveCustomizationGroupFromMenuItemCommandValidator : AbstractValidator<RemoveCustomizationGroupFromMenuItemCommand>
{
    public RemoveCustomizationGroupFromMenuItemCommandValidator()
    {
        RuleFor(v => v.RestaurantId)
            .NotEmpty().WithMessage("Restaurant ID is required.");

        RuleFor(v => v.MenuItemId)
            .NotEmpty().WithMessage("Menu Item ID is required.");

        RuleFor(v => v.CustomizationGroupId)
            .NotEmpty().WithMessage("Customization Group ID is required.");
    }
}

