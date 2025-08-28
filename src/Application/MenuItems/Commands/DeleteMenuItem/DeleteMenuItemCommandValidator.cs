namespace YummyZoom.Application.MenuItems.Commands.DeleteMenuItem;

public sealed class DeleteMenuItemCommandValidator : AbstractValidator<DeleteMenuItemCommand>
{
    public DeleteMenuItemCommandValidator()
    {
        RuleFor(v => v.RestaurantId)
            .NotEmpty().WithMessage("Restaurant ID is required.");

        RuleFor(v => v.MenuItemId)
            .NotEmpty().WithMessage("Menu Item ID is required.");
    }
}

