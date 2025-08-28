namespace YummyZoom.Application.MenuItems.Commands.ChangeMenuItemAvailability;

public sealed class ChangeMenuItemAvailabilityCommandValidator : AbstractValidator<ChangeMenuItemAvailabilityCommand>
{
    public ChangeMenuItemAvailabilityCommandValidator()
    {
        RuleFor(v => v.RestaurantId)
            .NotEmpty().WithMessage("Restaurant ID is required.");

        RuleFor(v => v.MenuItemId)
            .NotEmpty().WithMessage("Menu Item ID is required.");
        // IsAvailable is a bool; no additional rules needed
    }
}

