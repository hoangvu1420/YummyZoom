namespace YummyZoom.Application.MenuItems.Commands.AssignMenuItemToCategory;

public sealed class AssignMenuItemToCategoryCommandValidator : AbstractValidator<AssignMenuItemToCategoryCommand>
{
    public AssignMenuItemToCategoryCommandValidator()
    {
        RuleFor(v => v.RestaurantId)
            .NotEmpty().WithMessage("Restaurant ID is required.");

        RuleFor(v => v.MenuItemId)
            .NotEmpty().WithMessage("Menu Item ID is required.");

        RuleFor(v => v.NewCategoryId)
            .NotEmpty().WithMessage("New Category ID is required.");
    }
}

