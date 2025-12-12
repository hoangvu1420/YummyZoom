using FluentValidation;

namespace YummyZoom.Application.Coupons.Queries.FastCheck;

public sealed class FastCouponCheckQueryValidator : AbstractValidator<FastCouponCheckQuery>
{
    public FastCouponCheckQueryValidator()
    {
        RuleFor(x => x.RestaurantId)
            .NotEmpty()
            .WithMessage("Restaurant ID is required");

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("At least one item is required")
            .Must(items => items.Count <= 50)
            .WithMessage("Maximum 50 items allowed per request");

        RuleForEach(x => x.Items)
            .SetValidator(new FastCouponCheckItemDtoValidator());

        RuleFor(x => x.TipAmount)
            .GreaterThanOrEqualTo(0)
            .When(x => x.TipAmount.HasValue)
            .WithMessage("Tip amount must be non-negative");
    }
}

public sealed class FastCouponCheckItemDtoValidator : AbstractValidator<FastCouponCheckItemDto>
{
    public FastCouponCheckItemDtoValidator()
    {
        RuleFor(x => x.MenuItemId)
            .NotEmpty()
            .WithMessage("Menu item ID is required");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0")
            .LessThanOrEqualTo(99)
            .WithMessage("Quantity cannot exceed 99");

        RuleForEach(x => x.Customizations)
            .SetValidator(new FastCouponCheckCustomizationDtoValidator())
            .When(x => x.Customizations != null);
    }
}

public sealed class FastCouponCheckCustomizationDtoValidator : AbstractValidator<FastCouponCheckCustomizationDto>
{
    public FastCouponCheckCustomizationDtoValidator()
    {
        RuleFor(x => x.CustomizationGroupId)
            .NotEmpty()
            .WithMessage("Customization group ID is required");

        RuleFor(x => x.ChoiceIds)
            .NotEmpty()
            .WithMessage("At least one choice must be selected")
            .Must(choiceIds => choiceIds.All(id => id != Guid.Empty))
            .WithMessage("All choice IDs must be valid GUIDs");
    }
}
