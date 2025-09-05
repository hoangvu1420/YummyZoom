using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Interfaces.IServices;

namespace YummyZoom.Infrastructure.ReadModels.Search;

public sealed class SearchIndexMaintenanceHostedService : BackgroundService
{
    private readonly SearchIndexMaintenanceOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SearchIndexMaintenanceHostedService> _logger;

    public SearchIndexMaintenanceHostedService(
        IOptions<SearchIndexMaintenanceOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<SearchIndexMaintenanceHostedService> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("SearchIndexMaintenance: disabled via configuration");
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

            int builtRestaurants = 0, builtItems = 0, softDeleted = 0;

            const string missingRestaurantsSql = """
                SELECT r."Id"
                FROM "Restaurants" r
                WHERE r."IsDeleted" = FALSE
                AND NOT EXISTS (
                    SELECT 1 FROM "SearchIndexItems" s WHERE s."Id" = r."Id" AND s."Type" = 'restaurant'
                )
                LIMIT @Batch;
                """;

            var missingRestaurants = await conn.QueryAsync<Guid>(new CommandDefinition(missingRestaurantsSql, new { Batch = _options.BackfillBatchSize }, cancellationToken: ct));
            builtRestaurants += await UpsertRestaurantsAsync(missingRestaurants, ct);

            const string missingItemsSql = """
                SELECT i."Id"
                FROM "MenuItems" i
                JOIN "Restaurants" r ON r."Id" = i."RestaurantId"
                WHERE i."IsDeleted" = FALSE AND r."IsDeleted" = FALSE
                AND NOT EXISTS (
                    SELECT 1 FROM "SearchIndexItems" s WHERE s."Id" = i."Id" AND s."Type" = 'menu_item'
                )
                LIMIT @Batch;
                """;

            var missingItems = await conn.QueryAsync<Guid>(new CommandDefinition(missingItemsSql, new { Batch = _options.BackfillBatchSize }, cancellationToken: ct));
            builtItems += await UpsertMenuItemsAsync(missingItems, ct);

            if (_options.DeleteOrphans)
            {
                const string orphanRestaurantsSql = """
                    SELECT s."Id"
                    FROM "SearchIndexItems" s
                    LEFT JOIN "Restaurants" r ON r."Id" = s."Id"
                    WHERE s."Type" = 'restaurant'
                    AND (r."Id" IS NULL OR r."IsDeleted" = TRUE)
                    LIMIT @Batch;
                    """;
                var orphanRestaurants = await conn.QueryAsync<Guid>(new CommandDefinition(orphanRestaurantsSql, new { Batch = _options.BackfillBatchSize }, cancellationToken: ct));
                softDeleted += await SoftDeleteIdsAsync(orphanRestaurants, ct);

                const string orphanItemsSql = """
                    SELECT s."Id"
                    FROM "SearchIndexItems" s
                    LEFT JOIN "MenuItems" i ON i."Id" = s."Id"
                    WHERE s."Type" = 'menu_item'
                    AND (i."Id" IS NULL OR i."IsDeleted" = TRUE)
                    LIMIT @Batch;
                    """;
                var orphanItems = await conn.QueryAsync<Guid>(new CommandDefinition(orphanItemsSql, new { Batch = _options.BackfillBatchSize }, cancellationToken: ct));
                softDeleted += await SoftDeleteIdsAsync(orphanItems, ct);
            }

            _logger.LogInformation(
                "SearchIndexMaintenance backfill: restaurants_built={RestaurantsBuilt}, items_built={ItemsBuilt}, soft_deleted={SoftDeleted}",
                builtRestaurants, builtItems, softDeleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SearchIndexMaintenance: backfill pass failed");
        }
    }

    private async Task ReconcileOnceAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
            using var conn = db.CreateConnection();

            int rebuiltRestaurants = 0, rebuiltItems = 0;

            const string staleRestaurantsSql = """
                SELECT r."Id"
                FROM "SearchIndexItems" s
                JOIN "Restaurants" r ON r."Id" = s."Id"
                WHERE s."Type" = 'restaurant' AND s."SoftDeleted" = FALSE
                ORDER BY s."UpdatedAt" ASC
                LIMIT @Batch;
                """;
            var staleRestaurants = await conn.QueryAsync<Guid>(new CommandDefinition(staleRestaurantsSql, new { Batch = _options.ReconBatchSize }, cancellationToken: ct));
            rebuiltRestaurants += await UpsertRestaurantsAsync(staleRestaurants, ct);

            const string staleItemsSql = """
                SELECT i."Id"
                FROM "SearchIndexItems" s
                JOIN "MenuItems" i ON i."Id" = s."Id"
                WHERE s."Type" = 'menu_item' AND s."SoftDeleted" = FALSE
                ORDER BY s."UpdatedAt" ASC
                LIMIT @Batch;
                """;
            var staleItems = await conn.QueryAsync<Guid>(new CommandDefinition(staleItemsSql, new { Batch = _options.ReconBatchSize }, cancellationToken: ct));
            rebuiltItems += await UpsertMenuItemsAsync(staleItems, ct);

            _logger.LogInformation(
                "SearchIndexMaintenance reconcile: restaurants_rebuilt={RestaurantsRebuilt}, items_rebuilt={ItemsRebuilt}",
                rebuiltRestaurants, rebuiltItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SearchIndexMaintenance: reconciliation pass failed");
        }
    }

    private async Task<int> UpsertRestaurantsAsync(IEnumerable<Guid> restaurantIds, CancellationToken ct)
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
                    using var inner = _scopeFactory.CreateScope();
                    var maintainer = inner.ServiceProvider.GetRequiredService<ISearchReadModelMaintainer>();
                    var time = inner.ServiceProvider.GetRequiredService<TimeProvider>();
                    var sv = time.GetUtcNow().Ticks;
                    await maintainer.UpsertRestaurantByIdAsync(id, sv, ct);

                    var n = Interlocked.Increment(ref counter);
                    Interlocked.Increment(ref success);
                    if (_options.LogEveryN > 0 && n % _options.LogEveryN == 0)
                    {
                        _logger.LogInformation("SearchIndexMaintenance: processed {Count} restaurants", n);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "SearchIndexMaintenance: failed processing RestaurantId={RestaurantId}", id);
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

    private async Task<int> UpsertMenuItemsAsync(IEnumerable<Guid> itemIds, CancellationToken ct)
    {
        var counter = 0;
        var success = 0;
        using var throttler = new SemaphoreSlim(_options.MaxParallelism);
        var tasks = new List<Task>();

        foreach (var id in itemIds)
        {
            await throttler.WaitAsync(ct);
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    using var inner = _scopeFactory.CreateScope();
                    var maintainer = inner.ServiceProvider.GetRequiredService<ISearchReadModelMaintainer>();
                    var time = inner.ServiceProvider.GetRequiredService<TimeProvider>();
                    var sv = time.GetUtcNow().Ticks;
                    await maintainer.UpsertMenuItemByIdAsync(id, sv, ct);

                    var n = Interlocked.Increment(ref counter);
                    Interlocked.Increment(ref success);
                    if (_options.LogEveryN > 0 && n % _options.LogEveryN == 0)
                    {
                        _logger.LogInformation("SearchIndexMaintenance: processed {Count} menu items", n);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "SearchIndexMaintenance: failed processing MenuItemId={MenuItemId}", id);
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

    private async Task<int> SoftDeleteIdsAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        var deleted = 0;
        foreach (var id in ids)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var maintainer = scope.ServiceProvider.GetRequiredService<ISearchReadModelMaintainer>();
                var time = scope.ServiceProvider.GetRequiredService<TimeProvider>();
                var sv = time.GetUtcNow().Ticks;
                await maintainer.SoftDeleteByIdAsync(id, sv, ct);
                Interlocked.Increment(ref deleted);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SearchIndexMaintenance: failed soft-deleting Id={Id}", id);
            }
        }
        return deleted;
    }
}

