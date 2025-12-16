namespace YummyZoom.Application.Restaurants.Queries.Management.GetMenuCategoriesForMenu;

public sealed class GetMenuCategoriesForMenuQueryValidator : AbstractValidator<GetMenuCategoriesForMenuQuery>
{
    public GetMenuCategoriesForMenuQueryValidator()
    {
        RuleFor(x => x.RestaurantId)
            .NotEmpty().WithMessage("RestaurantId is required.");

        RuleFor(x => x.MenuId)
            .NotEmpty().WithMessage("MenuId is required.");
    }
}
