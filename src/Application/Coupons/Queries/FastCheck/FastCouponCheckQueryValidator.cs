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
    }
}

public sealed class FastCouponCheckItemDtoValidator : AbstractValidator<FastCouponCheckItemDto>
{
    public FastCouponCheckItemDtoValidator()
    {
        RuleFor(x => x.MenuItemId)
            .NotEmpty()
            .WithMessage("Menu item ID is required");

        RuleFor(x => x.MenuCategoryId)
            .NotEmpty()
            .WithMessage("Menu category ID is required");

        RuleFor(x => x.Qty)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0")
            .LessThanOrEqualTo(20)
            .WithMessage("Maximum quantity per item is 20");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Unit price must be non-negative")
            .LessThanOrEqualTo(1000)
            .WithMessage("Unit price cannot exceed 1000");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required")
            .Length(3)
            .WithMessage("Currency must be a 3-letter code");
    }
}
