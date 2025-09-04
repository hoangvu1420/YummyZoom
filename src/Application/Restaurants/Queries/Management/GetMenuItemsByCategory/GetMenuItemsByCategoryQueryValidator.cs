namespace YummyZoom.Application.Restaurants.Queries.Management.GetMenuItemsByCategory;

public sealed class GetMenuItemsByCategoryQueryValidator : AbstractValidator<GetMenuItemsByCategoryQuery>
{
    public GetMenuItemsByCategoryQueryValidator()
    {
        RuleFor(x => x.RestaurantId)
            .NotEmpty().WithMessage("RestaurantId is required.");

        RuleFor(x => x.MenuCategoryId)
            .NotEmpty().WithMessage("MenuCategoryId is required.");

        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("PageNumber must be >= 1.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("PageSize must be >= 1.");
    }
}

