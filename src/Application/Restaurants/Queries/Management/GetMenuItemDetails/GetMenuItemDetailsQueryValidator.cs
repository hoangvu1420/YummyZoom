namespace YummyZoom.Application.Restaurants.Queries.Management.GetMenuItemDetails;

public sealed class GetMenuItemDetailsQueryValidator : AbstractValidator<GetMenuItemDetailsQuery>
{
    public GetMenuItemDetailsQueryValidator()
    {
        RuleFor(x => x.RestaurantId)
            .NotEmpty().WithMessage("RestaurantId is required.");

        RuleFor(x => x.MenuItemId)
            .NotEmpty().WithMessage("MenuItemId is required.");
    }
}

