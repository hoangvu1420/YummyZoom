using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Admin.Queries.GetPlatformTrends;

/// <summary>
/// Retrieves the day-by-day performance series for platform metrics between an optional date window.
/// </summary>
/// <param name="StartDate">Inclusive start date (UTC). Defaults to UTC today minus 29 days when omitted.</param>
/// <param name="EndDate">Inclusive end date (UTC). Defaults to UTC today when omitted.</param>
public sealed record GetPlatformTrendsQuery(DateOnly? StartDate, DateOnly? EndDate)
    : IRequest<Result<IReadOnlyList<AdminDailyPerformancePointDto>>>;
