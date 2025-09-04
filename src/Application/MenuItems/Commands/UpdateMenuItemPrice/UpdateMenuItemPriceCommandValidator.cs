namespace YummyZoom.Application.MenuItems.Commands.UpdateMenuItemPrice;

public sealed class UpdateMenuItemPriceCommandValidator : AbstractValidator<UpdateMenuItemPriceCommand>
{
    public UpdateMenuItemPriceCommandValidator()
    {
        RuleFor(v => v.RestaurantId)
            .NotEmpty().WithMessage("Restaurant ID is required.");

        RuleFor(v => v.MenuItemId)
            .NotEmpty().WithMessage("Menu Item ID is required.");

        RuleFor(v => v.Price)
            .GreaterThan(0).WithMessage("Price must be greater than zero.")
            .LessThanOrEqualTo(9999.99m).WithMessage("Price must not exceed $9,999.99.");

        RuleFor(v => v.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency must be a 3-character ISO code (e.g., USD, EUR).")
            .Matches("^[A-Z]{3}$").WithMessage("Currency must be uppercase letters only (e.g., USD, EUR).");
    }
}

