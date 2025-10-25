using FluentValidation;

namespace YummyZoom.Application.Pricing.Queries.GetPricingPreview;

public class GetPricingPreviewQueryValidator : AbstractValidator<GetPricingPreviewQuery>
{
    public GetPricingPreviewQueryValidator()
    {
        RuleFor(x => x.RestaurantId)
            .NotEmpty()
            .WithMessage("Restaurant ID is required");

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("At least one item is required");

        RuleForEach(x => x.Items)
            .SetValidator(new PricingPreviewItemDtoValidator());

        RuleFor(x => x.TipAmount)
            .GreaterThanOrEqualTo(0)
            .When(x => x.TipAmount.HasValue)
            .WithMessage("Tip amount must be non-negative");

        RuleFor(x => x.CouponCode)
            .MaximumLength(50)
            .When(x => !string.IsNullOrEmpty(x.CouponCode))
            .WithMessage("Coupon code cannot exceed 50 characters");
    }
}

public class PricingPreviewItemDtoValidator : AbstractValidator<PricingPreviewItemDto>
{
    public PricingPreviewItemDtoValidator()
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
            .SetValidator(new PricingPreviewCustomizationDtoValidator())
            .When(x => x.Customizations != null);
    }
}

public class PricingPreviewCustomizationDtoValidator : AbstractValidator<PricingPreviewCustomizationDto>
{
    public PricingPreviewCustomizationDtoValidator()
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
