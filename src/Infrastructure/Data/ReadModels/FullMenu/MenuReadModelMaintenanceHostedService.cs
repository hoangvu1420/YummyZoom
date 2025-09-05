using System.Diagnostics;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Restaurants.Queries.Common;

namespace YummyZoom.Infrastructure.Data.ReadModels.FullMenu;

public sealed class MenuReadModelMaintenanceHostedService : BackgroundService
{
    private readonly MenuReadModelMaintenanceOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MenuReadModelMaintenanceHostedService> _logger;

    public MenuReadModelMaintenanceHostedService(
        IOptions<MenuReadModelMaintenanceOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<MenuReadModelMaintenanceHostedService> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("MenuReadModelMaintenance: disabled via configuration");
            return;
        }

        if (_options.InitialDelay > TimeSpan.Zero)
        {
            try { await Task.Delay(_options.InitialDelay, stoppingToken); }
            catch (OperationCanceledException) { return; }
        }

        await BackfillOnceAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await ReconcileOnceAsync(stoppingToken);
            try { await Task.Delay(_options.ReconInterval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task BackfillOnceAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
            using var conn = db.CreateConnection();
            var sw = Stopwatch.StartNew();
            int built = 0, deleted = 0;

            const string missingSql = """
SELECT r."Id"
FROM "Restaurants" r
JOIN "Menus" m ON m."RestaurantId" = r."Id"
WHERE m."IsEnabled" = TRUE
  AND m."IsDeleted" = FALSE
  AND r."IsDeleted" = FALSE
  AND NOT EXISTS (
    SELECT 1 FROM "FullMenuViews" v WHERE v."RestaurantId" = r."Id"
  )
LIMIT @Batch;
""";

            var missing = await conn.QueryAsync<Guid>(new CommandDefinition(missingSql, new { Batch = _options.BackfillBatchSize }, cancellationToken: ct));
            built += await ProcessIdsAsync(missing, ct);

            if (_options.DeleteOrphans)
            {
                const string orphanSql = """
SELECT v."RestaurantId"
FROM "FullMenuViews" v
LEFT JOIN "Menus" m
  ON m."RestaurantId" = v."RestaurantId"
  AND m."IsEnabled" = TRUE
  AND m."IsDeleted" = FALSE
WHERE m."Id" IS NULL
LIMIT @Batch;
""";

                var orphans = await conn.QueryAsync<Guid>(new CommandDefinition(orphanSql, new { Batch = _options.BackfillBatchSize }, cancellationToken: ct));
                deleted += await DeleteIdsAsync(orphans, ct);
            }

            sw.Stop();
            _logger.LogInformation(
                "MenuReadModelMaintenance backfill: built={Built}, deleted={Deleted}, took={ElapsedMs}ms",
                built, deleted, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MenuReadModelMaintenance: backfill pass failed");
        }
    }

    private async Task ReconcileOnceAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
            using var conn = db.CreateConnection();
            var sw = Stopwatch.StartNew();
            int rebuilt = 0;

            const string staleSql = """
SELECT v."RestaurantId"
FROM "FullMenuViews" v
ORDER BY v."LastRebuiltAt" ASC
LIMIT @Batch;
""";

            var stale = await conn.QueryAsync<Guid>(new CommandDefinition(staleSql, new { Batch = _options.ReconBatchSize }, cancellationToken: ct));
            rebuilt += await ProcessIdsAsync(stale, ct);

            sw.Stop();
            _logger.LogInformation(
                "MenuReadModelMaintenance reconcile: rebuilt={Rebuilt}, took={ElapsedMs}ms",
                rebuilt, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MenuReadModelMaintenance: reconciliation pass failed");
        }
    }

    private async Task<int> ProcessIdsAsync(IEnumerable<Guid> restaurantIds, CancellationToken ct)
    {
        var counter = 0;
        var success = 0;
        using var throttler = new SemaphoreSlim(_options.MaxParallelism);
        var tasks = new List<Task>();

        foreach (var id in restaurantIds)
        {
            await throttler.WaitAsync(ct);
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    using var innerScope = _scopeFactory.CreateScope();
                    var rebuilder = innerScope.ServiceProvider.GetRequiredService<IMenuReadModelRebuilder>();
                    var (menuJson, rebuiltAt) = await rebuilder.RebuildAsync(id, ct);
                    await rebuilder.UpsertAsync(id, menuJson, rebuiltAt, ct);

                    var n = Interlocked.Increment(ref counter);
                    Interlocked.Increment(ref success);
                    if (_options.LogEveryN > 0 && n % _options.LogEveryN == 0)
                    {
                        _logger.LogInformation("MenuReadModelMaintenance: processed {Count} restaurants", n);
                    }
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("Enabled menu not found", StringComparison.OrdinalIgnoreCase))
                {
                    // No enabled menu for restaurant → ensure view is removed
                    using var innerScope = _scopeFactory.CreateScope();
                    var rebuilder = innerScope.ServiceProvider.GetRequiredService<IMenuReadModelRebuilder>();
                    await rebuilder.DeleteAsync(id, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "MenuReadModelMaintenance: failed processing RestaurantId={RestaurantId}", id);
                }
                finally
                {
                    throttler.Release();
                }
            }, ct));
        }

        await Task.WhenAll(tasks);
        return success;
    }

    private async Task<int> DeleteIdsAsync(IEnumerable<Guid> restaurantIds, CancellationToken ct)
    {
        var deleted = 0;
        foreach (var id in restaurantIds)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var rebuilder = scope.ServiceProvider.GetRequiredService<IMenuReadModelRebuilder>();
                await rebuilder.DeleteAsync(id, ct);
                Interlocked.Increment(ref deleted);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MenuReadModelMaintenance: failed deleting RestaurantId={RestaurantId}", id);
            }
        }
        return deleted;
    }
}
