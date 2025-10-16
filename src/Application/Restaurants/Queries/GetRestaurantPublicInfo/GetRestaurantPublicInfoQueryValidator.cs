namespace YummyZoom.Application.Restaurants.Queries.GetRestaurantPublicInfo;

public sealed class GetRestaurantPublicInfoQueryValidator : AbstractValidator<GetRestaurantPublicInfoQuery>
{
    public GetRestaurantPublicInfoQueryValidator()
    {
        RuleFor(x => x.RestaurantId)
            .NotEmpty().WithMessage("RestaurantId is required.");

        // Latitude must be in valid range when provided
        RuleFor(x => x.Lat)
            .InclusiveBetween(-90, 90)
            .When(x => x.Lat.HasValue)
            .WithMessage("Latitude must be between -90 and 90.");

        // Longitude must be in valid range when provided
        RuleFor(x => x.Lng)
            .InclusiveBetween(-180, 180)
            .When(x => x.Lng.HasValue)
            .WithMessage("Longitude must be between -180 and 180.");

        // Both lat and lng must be provided together
        RuleFor(x => x)
            .Must(x => (x.Lat.HasValue && x.Lng.HasValue) || (!x.Lat.HasValue && !x.Lng.HasValue))
            .WithMessage("Both Lat and Lng must be provided together, or both omitted.");
    }
}
