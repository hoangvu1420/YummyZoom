using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Admin.Queries.GetPlatformMetricsSummary;

/// <summary>
/// Retrieves the most recent platform-wide KPI snapshot populated by the admin metrics maintainer.
/// </summary>
public sealed record GetPlatformMetricsSummaryQuery : IRequest<Result<AdminPlatformMetricsSummaryDto>>;

/// <summary>
/// Errors related to retrieving the platform metrics summary snapshot.
/// </summary>
public static class GetPlatformMetricsSummaryErrors
{
    public static Error SnapshotUnavailable => Error.NotFound(
        "AdminDashboard.PlatformSnapshotUnavailable",
        "Admin platform metrics have not been computed yet.");
}
