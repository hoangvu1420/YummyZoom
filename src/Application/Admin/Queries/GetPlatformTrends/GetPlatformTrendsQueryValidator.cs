namespace YummyZoom.Application.Admin.Queries.GetPlatformTrends;

/// <summary>
/// Validation rules for the platform trends query.
/// </summary>
public sealed class GetPlatformTrendsQueryValidator : AbstractValidator<GetPlatformTrendsQuery>
{
    private const int MaxRangeDays = 180;

    public GetPlatformTrendsQueryValidator()
    {
        RuleFor(x => x)
            .Must(q => !q.StartDate.HasValue || !q.EndDate.HasValue || q.StartDate.Value <= q.EndDate.Value)
            .WithMessage("StartDate must be on or before EndDate.");

        RuleFor(x => x)
            .Must(q => !q.StartDate.HasValue || !q.EndDate.HasValue || (q.EndDate.Value.DayNumber - q.StartDate.Value.DayNumber) <= MaxRangeDays)
            .WithMessage($"The requested date window cannot exceed {MaxRangeDays} days.");
    }
}
