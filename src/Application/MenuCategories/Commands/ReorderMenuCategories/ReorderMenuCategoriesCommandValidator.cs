using FluentValidation;

namespace YummyZoom.Application.MenuCategories.Commands.ReorderMenuCategories;

public sealed class ReorderMenuCategoriesCommandValidator : AbstractValidator<ReorderMenuCategoriesCommand>
{
    public ReorderMenuCategoriesCommandValidator()
    {
        RuleFor(v => v.RestaurantId)
            .NotEmpty().WithMessage("Restaurant ID is required.");

        RuleFor(v => v.CategoryOrders)
            .NotNull().WithMessage("CategoryOrders is required.")
            .NotEmpty().WithMessage("CategoryOrders must contain at least one entry.");

        RuleForEach(v => v.CategoryOrders).ChildRules(orders =>
        {
            orders.RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage("CategoryId is required.");

            orders.RuleFor(x => x.DisplayOrder)
                .GreaterThan(0).WithMessage("DisplayOrder must be greater than zero.");
        });

        RuleFor(v => v.CategoryOrders)
            .Must(HaveDistinctCategoryIds)
            .WithMessage("CategoryOrders must not contain duplicate CategoryIds.");

        RuleFor(v => v.CategoryOrders)
            .Must(HaveDistinctDisplayOrders)
            .WithMessage("CategoryOrders must not contain duplicate DisplayOrder values.");
    }

    private static bool HaveDistinctCategoryIds(IReadOnlyCollection<CategoryOrderDto>? orders)
    {
        if (orders is null) return true;
        return orders.Count == orders.Select(o => o.CategoryId).Distinct().Count();
    }

    private static bool HaveDistinctDisplayOrders(IReadOnlyCollection<CategoryOrderDto>? orders)
    {
        if (orders is null) return true;
        return orders.Count == orders.Select(o => o.DisplayOrder).Distinct().Count();
    }
}
