namespace YummyZoom.Application.Restaurants.Queries.SearchRestaurants;

public sealed class SearchRestaurantsQueryValidator : AbstractValidator<SearchRestaurantsQuery>
{
    private const int MaxPageSize = 50;

    public SearchRestaurantsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("PageNumber must be at least 1.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("PageSize must be at least 1.")
            .LessThanOrEqualTo(MaxPageSize).WithMessage($"PageSize must be <= {MaxPageSize}.");

        RuleFor(x => x.Q)
            .MaximumLength(100);

        RuleFor(x => x.Cuisine)
            .MaximumLength(50);

        When(x => x.Lat.HasValue || x.Lng.HasValue || x.RadiusKm.HasValue, () =>
        {
            RuleFor(x => x.Lat)
                .NotNull().InclusiveBetween(-90, 90);
            RuleFor(x => x.Lng)
                .NotNull().InclusiveBetween(-180, 180);
            RuleFor(x => x.RadiusKm)
                .NotNull().GreaterThan(0).LessThanOrEqualTo(25);
        });
    }
}
