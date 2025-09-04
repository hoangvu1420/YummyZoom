namespace YummyZoom.Application.Restaurants.Queries.Management.GetMenuCategoryDetails;

public sealed class GetMenuCategoryDetailsQueryValidator : AbstractValidator<GetMenuCategoryDetailsQuery>
{
    public GetMenuCategoryDetailsQueryValidator()
    {
        RuleFor(x => x.RestaurantId)
            .NotEmpty().WithMessage("RestaurantId is required.");

        RuleFor(x => x.MenuCategoryId)
            .NotEmpty().WithMessage("MenuCategoryId is required.");
    }
}

