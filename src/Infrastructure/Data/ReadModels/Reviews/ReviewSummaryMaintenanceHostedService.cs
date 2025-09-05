using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YummyZoom.Application.Common.Interfaces;

namespace YummyZoom.Infrastructure.Data.ReadModels.Reviews;

public sealed class ReviewSummaryMaintenanceHostedService : BackgroundService
{
    private readonly ReviewSummaryMaintenanceOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReviewSummaryMaintenanceHostedService> _logger;

    public ReviewSummaryMaintenanceHostedService(
        IOptions<ReviewSummaryMaintenanceOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<ReviewSummaryMaintenanceHostedService> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("ReviewSummaryMaintenance: disabled via configuration");
            return;
        }

        if (_options.InitialDelay > TimeSpan.Zero)
        {
            try { await Task.Delay(_options.InitialDelay, stoppingToken); }
            catch (OperationCanceledException) { return; }
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await RecomputeAllOnceAsync(stoppingToken);
            try { await Task.Delay(_options.ReconInterval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task RecomputeAllOnceAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
            using var conn = db.CreateConnection();

            const string sql = """
INSERT INTO "RestaurantReviewSummaries" ("RestaurantId", "AverageRating", "TotalReviews")
SELECT r."Id",
       COALESCE(AVG(rv."Rating")::double precision, 0.0) AS avg_rating,
       COALESCE(COUNT(rv.*)::int, 0) AS total_reviews
FROM "Restaurants" r
LEFT JOIN "Reviews" rv
  ON rv."RestaurantId" = r."Id"
 AND rv."IsDeleted" = FALSE
 AND rv."IsHidden" = FALSE
WHERE r."IsDeleted" = FALSE
GROUP BY r."Id"
ON CONFLICT ("RestaurantId") DO UPDATE
  SET "AverageRating" = EXCLUDED."AverageRating",
      "TotalReviews"  = EXCLUDED."TotalReviews";
""";

            await conn.ExecuteAsync(new CommandDefinition(sql, cancellationToken: ct));
            _logger.LogInformation("ReviewSummaryMaintenance: recompute pass completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ReviewSummaryMaintenance: recompute pass failed");
        }
    }
}

