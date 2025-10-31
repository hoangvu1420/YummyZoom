using FluentValidation;

namespace YummyZoom.Application.Restaurants.Queries.Public.GetMenuItemDetails;

public sealed class GetMenuItemPublicDetailsQueryValidator : AbstractValidator<GetMenuItemPublicDetailsQuery>
{
    public GetMenuItemPublicDetailsQueryValidator()
    {
        RuleFor(x => x.RestaurantId).NotEmpty();
        RuleFor(x => x.ItemId).NotEmpty();
    }
}

