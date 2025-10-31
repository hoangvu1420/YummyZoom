using FluentValidation;

namespace YummyZoom.Application.Restaurants.Queries.Public.GetMenuItemAvailability;

public sealed class GetMenuItemAvailabilityQueryValidator : AbstractValidator<GetMenuItemAvailabilityQuery>
{
    public GetMenuItemAvailabilityQueryValidator()
    {
        RuleFor(x => x.RestaurantId).NotEmpty();
        RuleFor(x => x.ItemId).NotEmpty();
    }
}

