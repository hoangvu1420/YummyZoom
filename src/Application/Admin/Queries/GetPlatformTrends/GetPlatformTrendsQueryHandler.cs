using Dapper;
using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Admin.Queries.GetPlatformTrends;

/// <summary>
/// Handler for retrieving the platform performance series used to power admin dashboard charts.
/// </summary>
public sealed class GetPlatformTrendsQueryHandler : IRequestHandler<GetPlatformTrendsQuery, Result<IReadOnlyList<AdminDailyPerformancePointDto>>>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<GetPlatformTrendsQueryHandler> _logger;

    public GetPlatformTrendsQueryHandler(
        IDbConnectionFactory dbConnectionFactory,
        ILogger<GetPlatformTrendsQueryHandler> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<AdminDailyPerformancePointDto>>> Handle(GetPlatformTrendsQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var defaultStart = today.AddDays(-29);
        var startDate = request.StartDate ?? defaultStart;
        var endDate = request.EndDate ?? today;

        if (startDate > endDate)
        {
            return Result.Failure<IReadOnlyList<AdminDailyPerformancePointDto>>(
                Error.Validation("AdminDashboard.InvalidDateRange", "StartDate must be on or before EndDate."));
        }

        using var connection = _dbConnectionFactory.CreateConnection();

        const string sql = """
SELECT
    "BucketDate"                 AS BucketDate,
    "TotalOrders"                AS TotalOrders,
    "DeliveredOrders"            AS DeliveredOrders,
    "GrossMerchandiseVolume"     AS GrossMerchandiseVolume,
    "TotalRefunds"               AS TotalRefunds,
    "NewCustomers"               AS NewCustomers,
    "NewRestaurants"             AS NewRestaurants,
    "UpdatedAtUtc"               AS UpdatedAtUtc
FROM "AdminDailyPerformanceSeries"
WHERE "BucketDate" BETWEEN @StartDate AND @EndDate
ORDER BY "BucketDate" ASC;
""";

        var dbParameters = new
        {
            StartDate = ToUtcDateTime(startDate),
            EndDate = ToUtcDateTime(endDate)
        };

        var rows = await connection.QueryAsync<TrendRow>(
            new CommandDefinition(sql, dbParameters, cancellationToken: cancellationToken));

        var list = rows
            .Select(r => new AdminDailyPerformancePointDto(
                DateOnly.FromDateTime(r.BucketDate.Date),
                r.TotalOrders,
                r.DeliveredOrders,
                r.GrossMerchandiseVolume,
                r.TotalRefunds,
                r.NewCustomers,
                r.NewRestaurants,
                r.UpdatedAtUtc))
            .ToList();

        _logger.LogInformation(
            "Retrieved {Count} admin performance points between {StartDate} and {EndDate}",
            list.Count,
            startDate,
            endDate);

        return Result.Success<IReadOnlyList<AdminDailyPerformancePointDto>>(list);
    }

    private static DateTime ToUtcDateTime(DateOnly date) =>
        DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

    private sealed record TrendRow(
        DateTime BucketDate,
        int TotalOrders,
        int DeliveredOrders,
        decimal GrossMerchandiseVolume,
        decimal TotalRefunds,
        int NewCustomers,
        int NewRestaurants,
        DateTime UpdatedAtUtc);
}

