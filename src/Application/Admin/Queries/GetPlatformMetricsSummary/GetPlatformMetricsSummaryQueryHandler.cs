using Dapper;
using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Admin.Queries.GetPlatformMetricsSummary;

/// <summary>
/// Handler responsible for reading the latest admin platform metrics snapshot from the projections store.
/// </summary>
public sealed class GetPlatformMetricsSummaryQueryHandler : IRequestHandler<GetPlatformMetricsSummaryQuery, Result<AdminPlatformMetricsSummaryDto>>
{
    private const string SnapshotId = "platform";

    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<GetPlatformMetricsSummaryQueryHandler> _logger;

    public GetPlatformMetricsSummaryQueryHandler(
        IDbConnectionFactory dbConnectionFactory,
        ILogger<GetPlatformMetricsSummaryQueryHandler> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
    }

    public async Task<Result<AdminPlatformMetricsSummaryDto>> Handle(GetPlatformMetricsSummaryQuery request, CancellationToken cancellationToken)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        const string sql = """
SELECT
    "TotalOrders"              AS TotalOrders,
    "ActiveOrders"             AS ActiveOrders,
    "DeliveredOrders"          AS DeliveredOrders,
    "GrossMerchandiseVolume"   AS GrossMerchandiseVolume,
    "TotalRefunds"             AS TotalRefunds,
    "ActiveRestaurants"        AS ActiveRestaurants,
    "ActiveCustomers"          AS ActiveCustomers,
    "OpenSupportTickets"       AS OpenSupportTickets,
    "TotalReviews"             AS TotalReviews,
    "LastOrderAtUtc"           AS LastOrderAtUtc,
    "UpdatedAtUtc"             AS UpdatedAtUtc
FROM "AdminPlatformMetricsSnapshots"
WHERE "SnapshotId" = @SnapshotId;
""";

        var summary = await connection.QuerySingleOrDefaultAsync<AdminPlatformMetricsSummaryDto>(
            new CommandDefinition(sql, new { SnapshotId }, cancellationToken: cancellationToken));

        if (summary is null)
        {
            _logger.LogWarning("Admin metrics snapshot with id {SnapshotId} not found", SnapshotId);
            return Result.Failure<AdminPlatformMetricsSummaryDto>(GetPlatformMetricsSummaryErrors.SnapshotUnavailable);
        }

        _logger.LogInformation("Retrieved admin platform metrics snapshot (updated at {UpdatedAtUtc})", summary.UpdatedAtUtc);
        return Result.Success(summary);
    }
}
