namespace YummyZoom.Application.Restaurants.Queries.GetRestaurantPublicInfo;

public sealed class GetRestaurantPublicInfoQueryValidator : AbstractValidator<GetRestaurantPublicInfoQuery>
{
    public GetRestaurantPublicInfoQueryValidator()
    {
        RuleFor(x => x.RestaurantId)
            .NotEmpty().WithMessage("RestaurantId is required.");
    }
}
