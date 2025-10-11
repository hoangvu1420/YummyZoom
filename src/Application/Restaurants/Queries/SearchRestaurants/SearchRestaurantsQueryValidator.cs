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

        // Geo: allow lat/lng without radius for distance sorting and distanceKm projection.
        When(x => x.Lat.HasValue || x.Lng.HasValue, () =>
        {
            RuleFor(x => x.Lat).NotNull().InclusiveBetween(-90, 90);
            RuleFor(x => x.Lng).NotNull().InclusiveBetween(-180, 180);
        });

        // Radius currently unsupported for this endpoint (map viewport/radius coming later)
        RuleFor(x => x.RadiusKm)
            .Null().WithMessage("RadiusKm is not supported.");

        // Sort: allow rating|distance|popularity (case-insensitive)
        When(x => !string.IsNullOrWhiteSpace(x.Sort), () =>
        {
            RuleFor(x => x.Sort!)
                .Must(s => new[] { "rating", "distance", "popularity" }.Contains(s.Trim().ToLowerInvariant()))
                .WithMessage("Sort must be one of: rating, distance, popularity.");
        });

        // BBox validation: minLon,minLat,maxLon,maxLat
        When(x => !string.IsNullOrWhiteSpace(x.Bbox), () =>
        {
            RuleFor(x => x.Bbox!)
                .Must(b =>
                {
                    var parts = b.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (parts.Length != 4) return false;
                    return double.TryParse(parts[0], out var minLon)
                        && double.TryParse(parts[1], out var minLat)
                        && double.TryParse(parts[2], out var maxLon)
                        && double.TryParse(parts[3], out var maxLat)
                        && minLon >= -180 && maxLon <= 180 && minLat >= -90 && maxLat <= 90
                        && minLon < maxLon && minLat < maxLat;
                })
                .WithMessage("bbox must be 'minLon,minLat,maxLon,maxLat' within valid ranges.");
        });
    }
}
