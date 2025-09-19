namespace YummyZoom.Application.Admin.Queries.ListRestaurantsForAdmin;

/// <summary>
/// Validation rules for the admin restaurant listing query.
/// </summary>
public sealed class ListRestaurantsForAdminQueryValidator : AbstractValidator<ListRestaurantsForAdminQuery>
{
    private const int MaxPageSize = 100;
    private const int MaxSearchLength = 200;

    public ListRestaurantsForAdminQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0);

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(MaxPageSize);

        RuleFor(x => x.MinAverageRating)
            .InclusiveBetween(0, 5)
            .When(x => x.MinAverageRating.HasValue);

        RuleFor(x => x.MinOrdersLast30Days)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MinOrdersLast30Days.HasValue);

        RuleFor(x => x.MaxOutstandingBalance)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MaxOutstandingBalance.HasValue);

        RuleFor(x => x.Search)
            .MaximumLength(MaxSearchLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Search));
    }
}
