using System.Linq;
using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.MenuItems.ReadModels;
using YummyZoom.Infrastructure.Persistence.EfCore;
using YummyZoom.Infrastructure.Persistence.ReadModels.MenuItemSales;

namespace YummyZoom.Infrastructure.Persistence.ReadModels.MenuItemSales;

/// <summary>
/// Maintains aggregated sales counters for menu items.
/// Persists incremental deltas as orders transition to delivered and
/// exposes recompute helpers for reconciliation jobs.
/// </summary>
public sealed class MenuItemSalesSummaryMaintainer : IMenuItemSalesSummaryMaintainer
{
    private readonly ApplicationDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public MenuItemSalesSummaryMaintainer(ApplicationDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task UpsertDeltaAsync(
        Guid restaurantId,
        Guid menuItemId,
        long lifetimeQuantityDelta,
        long rolling7DayQuantityDelta,
        long rolling30DayQuantityDelta,
        DateTimeOffset? lastSoldAt,
        long sourceVersion,
        CancellationToken ct = default)
    {
        var now = _timeProvider.GetUtcNow();

        var summary = await _dbContext.MenuItemSalesSummaries
            .SingleOrDefaultAsync(
                x => x.RestaurantId == restaurantId && x.MenuItemId == menuItemId,
                cancellationToken: ct);

        if (summary is null)
        {
            summary = new MenuItemSalesSummary
            {
                RestaurantId = restaurantId,
                MenuItemId = menuItemId,
                LifetimeQuantity = lifetimeQuantityDelta,
                Rolling7DayQuantity = rolling7DayQuantityDelta,
                Rolling30DayQuantity = rolling30DayQuantityDelta,
                LastSoldAt = lastSoldAt,
                LastUpdatedAt = now,
                SourceVersion = sourceVersion
            };

            await _dbContext.MenuItemSalesSummaries.AddAsync(summary, ct);
            return;
        }

        summary.LifetimeQuantity = checked(summary.LifetimeQuantity + lifetimeQuantityDelta);
        summary.Rolling7DayQuantity = checked(summary.Rolling7DayQuantity + rolling7DayQuantityDelta);
        summary.Rolling30DayQuantity = checked(summary.Rolling30DayQuantity + rolling30DayQuantityDelta);

        if (lastSoldAt.HasValue)
        {
            summary.LastSoldAt = summary.LastSoldAt is null || lastSoldAt > summary.LastSoldAt
                ? lastSoldAt
                : summary.LastSoldAt;
        }

        summary.LastUpdatedAt = now;
        summary.SourceVersion = sourceVersion;
    }

    public async Task RecomputeForRestaurantAsync(Guid restaurantId, CancellationToken ct = default)
    {
        var now = _timeProvider.GetUtcNow();
        var sevenDayThreshold = now - TimeSpan.FromDays(7);
        var thirtyDayThreshold = now - TimeSpan.FromDays(30);

        var deliveredItems = await _dbContext.Orders
            .AsNoTracking()
            .Where(o => o.RestaurantId.Value == restaurantId && o.Status == Domain.OrderAggregate.Enums.OrderStatus.Delivered)
            .SelectMany(o => o.OrderItems.Select(item => new
            {
                o.RestaurantId,
                MenuItemId = item.Snapshot_MenuItemId.Value,
                item.Quantity,
                DeliveredAt = o.ActualDeliveryTime ?? o.LastUpdateTimestamp
            }))
            .ToListAsync(ct);

        var aggregates = deliveredItems
            .GroupBy(x => x.MenuItemId)
            .Select(g =>
            {
                long lifetime = g.Sum(x => (long)x.Quantity);
                long rolling7 = g.Where(x => ToUtcOffset(x.DeliveredAt) >= sevenDayThreshold).Sum(x => (long)x.Quantity);
                long rolling30 = g.Where(x => ToUtcOffset(x.DeliveredAt) >= thirtyDayThreshold).Sum(x => (long)x.Quantity);
                DateTimeOffset? lastSoldAt = g.Select(x => ToUtcOffset(x.DeliveredAt)).Where(d => d != DateTimeOffset.MinValue).DefaultIfEmpty().Max();

                if (lastSoldAt == DateTimeOffset.MinValue)
                {
                    lastSoldAt = null;
                }

                return new
                {
                    MenuItemId = g.Key,
                    Lifetime = lifetime,
                    Rolling7 = rolling7,
                    Rolling30 = rolling30,
                    LastSoldAt = lastSoldAt
                };
            })
            .ToList();

        var existing = await _dbContext.MenuItemSalesSummaries
            .Where(x => x.RestaurantId == restaurantId)
            .ToListAsync(ct);

        var existingMap = existing.ToDictionary(x => x.MenuItemId);
        var processed = new HashSet<Guid>();

        foreach (var aggregate in aggregates)
        {
            if (!existingMap.TryGetValue(aggregate.MenuItemId, out var summary))
            {
                summary = new MenuItemSalesSummary
                {
                    RestaurantId = restaurantId,
                    MenuItemId = aggregate.MenuItemId
                };

                await _dbContext.MenuItemSalesSummaries.AddAsync(summary, ct);
                existingMap.Add(aggregate.MenuItemId, summary);
            }

            summary.LifetimeQuantity = aggregate.Lifetime;
            summary.Rolling7DayQuantity = aggregate.Rolling7;
            summary.Rolling30DayQuantity = aggregate.Rolling30;
            summary.LastSoldAt = aggregate.LastSoldAt;
            summary.LastUpdatedAt = now;
            summary.SourceVersion = now.Ticks;

            processed.Add(aggregate.MenuItemId);
        }

        foreach (var summary in existing)
        {
            if (processed.Contains(summary.MenuItemId))
            {
                continue;
            }

            _dbContext.MenuItemSalesSummaries.Remove(summary);
        }
    }

    public async Task<IReadOnlyList<MenuItemSalesSummaryDto>> GetSummariesForRestaurantAsync(Guid restaurantId, CancellationToken ct = default)
    {
        return await _dbContext.MenuItemSalesSummaries
            .AsNoTracking()
            .Where(x => x.RestaurantId == restaurantId)
            .Select(x => new MenuItemSalesSummaryDto(
                x.RestaurantId,
                x.MenuItemId,
                x.LifetimeQuantity,
                x.Rolling7DayQuantity,
                x.Rolling30DayQuantity,
                x.LastSoldAt,
                x.LastUpdatedAt))
            .ToListAsync(ct);
    }

    private static DateTimeOffset ToUtcOffset(DateTime timestamp)
    {
        var normalized = DateTime.SpecifyKind(timestamp, DateTimeKind.Utc);
        return new DateTimeOffset(normalized, TimeSpan.Zero);
    }
}
