namespace YummyZoom.Application.MenuItems.Commands.AssignCustomizationGroupToMenuItem;

public sealed class AssignCustomizationGroupToMenuItemCommandValidator : AbstractValidator<AssignCustomizationGroupToMenuItemCommand>
{
    public AssignCustomizationGroupToMenuItemCommandValidator()
    {
        RuleFor(v => v.RestaurantId)
            .NotEmpty().WithMessage("Restaurant ID is required.");

        RuleFor(v => v.MenuItemId)
            .NotEmpty().WithMessage("Menu Item ID is required.");

        RuleFor(v => v.CustomizationGroupId)
            .NotEmpty().WithMessage("Customization Group ID is required.");

        RuleFor(v => v.DisplayTitle)
            .NotEmpty().WithMessage("Display title is required.")
            .MaximumLength(200).WithMessage("Display title must not exceed 200 characters.");

        When(v => v.DisplayOrder.HasValue, () =>
        {
            RuleFor(v => v.DisplayOrder!.Value)
                .GreaterThanOrEqualTo(0).WithMessage("Display order must be non-negative.");
        });
    }
}

