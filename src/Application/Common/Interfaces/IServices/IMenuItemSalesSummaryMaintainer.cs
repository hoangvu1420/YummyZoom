using YummyZoom.Application.MenuItems.ReadModels;

namespace YummyZoom.Application.Common.Interfaces.IServices;

/// <summary>
/// Maintains denormalized sales counters for menu items and exposes query helpers
/// for read models that need popularity metrics.
/// </summary>
public interface IMenuItemSalesSummaryMaintainer
{
    /// <summary>
    /// Applies an idempotent delta to the tracked counters for a menu item.
    /// Implementations should use <paramref name="sourceVersion"/> to guarantee
    /// last-write-wins semantics when the same order is replayed.
    /// </summary>
    Task UpsertDeltaAsync(
        Guid restaurantId,
        Guid menuItemId,
        long lifetimeQuantityDelta,
        long rolling7DayQuantityDelta,
        long rolling30DayQuantityDelta,
        DateTimeOffset? lastSoldAt,
        long sourceVersion,
        CancellationToken ct = default);

    /// <summary>
    /// Recomputes the counters for the specified restaurant from authoritative sources.
    /// Useful for reconciliation jobs and backfills.
    /// </summary>
    Task RecomputeForRestaurantAsync(Guid restaurantId, CancellationToken ct = default);

    /// <summary>
    /// Fetches all menu item summaries for the restaurant for projection into read models.
    /// </summary>
    Task<IReadOnlyList<MenuItemSalesSummaryDto>> GetSummariesForRestaurantAsync(Guid restaurantId, CancellationToken ct = default);
}
