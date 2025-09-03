namespace YummyZoom.Application.Search.Queries.UniversalSearch;

public sealed class UniversalSearchQueryValidator : AbstractValidator<UniversalSearchQuery>
{
    public UniversalSearchQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Term).MaximumLength(256);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue);
        RuleFor(x => x.Cuisines).Must(c => c!.All(s => s.Length <= 100)).When(x => x.Cuisines is { Length: > 0 });
        RuleFor(x => x.Tags).Must(t => t!.All(s => s.Length <= 100)).When(x => x.Tags is { Length: > 0 });
        RuleFor(x => x.PriceBands).Must(pb => pb!.All(b => b >= 0)).When(x => x.PriceBands is { Length: > 0 });
    }
}
