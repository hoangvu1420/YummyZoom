using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Restaurants.Queries.Common;
using YummyZoom.Domain.Common.Models;
using YummyZoom.Domain.TagEntity.ValueObjects;

namespace YummyZoom.Application.Tags.EventHandlers.Shared;

/// <summary>
/// Shared base for Tag event projectors that rebuild the FullMenuView.
/// - Ensures idempotency via IdempotentNotificationHandler
/// - Provides helpers to rebuild the restaurant menu JSON
/// - Provides a method to find impacted restaurants by TagId via direct SQL
/// </summary>
public abstract class TagProjectorBase<TEvent> : IdempotentNotificationHandler<TEvent>
    where TEvent : IDomainEvent, IHasEventId
{
    protected readonly IMenuReadModelRebuilder _rebuilder;
    protected readonly IDbConnectionFactory _dbConnectionFactory;
    protected readonly ILogger _logger;

    protected TagProjectorBase(
        IUnitOfWork uow,
        IInboxStore inbox,
        IMenuReadModelRebuilder rebuilder,
        IDbConnectionFactory dbConnectionFactory,
        ILogger logger) : base(uow, inbox)
    {
        _rebuilder = rebuilder;
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
    }

    protected async Task RebuildForRestaurantAsync(Guid restaurantId, CancellationToken ct)
    {
        try
        {
            var (menuJson, rebuiltAt) = await _rebuilder.RebuildAsync(restaurantId, ct);
            await _rebuilder.UpsertAsync(restaurantId, menuJson, rebuiltAt, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rebuild FullMenuView for RestaurantId={RestaurantId}", restaurantId);
            // Do not throw to avoid breaking the originating command; outbox retry policy may re-run
        }
    }

    protected async Task<IReadOnlyList<Guid>> FindRestaurantsByTagAsync(TagId tagId, CancellationToken ct)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        const string sql = """
            SELECT DISTINCT mi."RestaurantId"
            FROM "MenuItems" mi
            WHERE mi."IsDeleted" = FALSE
              AND mi."DietaryTagIds" @> @TagIdJson::jsonb;
            """;

        var tagJson = $"[\"{tagId.Value}\"]";

        var rows = await connection.QueryAsync<Guid>(new CommandDefinition(sql, new { TagIdJson = tagJson }, cancellationToken: ct));
        return rows.ToList();
    }
}
