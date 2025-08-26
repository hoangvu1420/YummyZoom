using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.Restaurants.Queries.Common;
using YummyZoom.Infrastructure.Data;
using YummyZoom.Infrastructure.Data.Models;
using YummyZoom.Infrastructure.Serialization;

namespace YummyZoom.Infrastructure.ReadModels.FullMenu;

public sealed class FullMenuViewRebuilder : IMenuReadModelRebuilder
{
    private readonly ApplicationDbContext _db;

    public FullMenuViewRebuilder(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<(string menuJson, DateTimeOffset lastRebuiltAt)> RebuildAsync(Guid restaurantId, CancellationToken ct = default)
    {
        // Load enabled menu for restaurant
        var menu = await _db.Menus
            .AsNoTracking()
            .Where(m => m.RestaurantId.Value == restaurantId && m.IsEnabled && !m.IsDeleted)
            .Select(m => new { m.Id, m.Name, m.Description, m.IsEnabled })
            .FirstOrDefaultAsync(ct);
        if (menu is null)
        {
            throw new InvalidOperationException("Enabled menu not found for restaurant");
        }

        // Categories (ordered)
        var categories = await _db.MenuCategories
            .AsNoTracking()
            .Where(c => c.MenuId == menu.Id && !c.IsDeleted)
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .Select(c => new { c.Id, c.Name, c.DisplayOrder })
            .ToListAsync(ct);

        var categoryOrder = categories.Select(c => c.Id).ToList();

        // Items by category
        var categoryIds = categories.Select(c => c.Id).ToList();
        var items = await _db.MenuItems
            .AsNoTracking()
            .Where(i => i.RestaurantId.Value == restaurantId && categoryIds.Contains(i.MenuCategoryId) && !i.IsDeleted)
            .Select(i => new
            {
                i.Id,
                i.MenuCategoryId,
                i.Name,
                i.Description,
                i.BasePrice.Amount,
                i.BasePrice.Currency,
                i.ImageUrl,
                i.IsAvailable,
                i.DietaryTagIds,
                i.AppliedCustomizations
            })
            .ToListAsync(ct);

        // Customization groups referenced
        var groupIds = items.SelectMany(i => i.AppliedCustomizations.Select(c => c.CustomizationGroupId.Value)).Distinct().ToList();
        var groups = await _db.CustomizationGroups
            .AsNoTracking()
            .Where(g => g.RestaurantId.Value == restaurantId && groupIds.Contains(g.Id.Value) && !g.IsDeleted)
            .Include(g => g.Choices)
            .Select(g => new
            {
                g.Id,
                g.GroupName,
                g.MinSelections,
                g.MaxSelections,
                Choices = g.Choices.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.PriceAdjustment.Amount,
                    c.PriceAdjustment.Currency,
                    c.IsDefault,
                    c.DisplayOrder
                }).ToList()
            })
            .ToListAsync(ct);

        // Tag legend
        var tagIds = items.SelectMany(i => i.DietaryTagIds.Select(t => t.Value)).Distinct().ToList();
        var tags = await _db.Tags
            .AsNoTracking()
            .Where(t => tagIds.Contains(t.Id.Value) && !t.IsDeleted)
            .Select(t => new { t.Id, t.TagName, Category = t.TagCategory.ToString() })
            .ToListAsync(ct);

        // Build normalized JSON object
        var now = DateTimeOffset.UtcNow;
        var doc = new
        {
            version = 1,
            restaurantId,
            menuId = menu.Id,
            menuName = menu.Name,
            menuDescription = menu.Description,
            menuEnabled = menu.IsEnabled,
            lastRebuiltAt = now,
            currency = items.Select(i => i.Currency).FirstOrDefault() ?? "USD",
            categories = new
            {
                order = categoryOrder,
                byId = categories.ToDictionary(
                    c => c.Id,
                    c => new
                    {
                        id = c.Id,
                        name = c.Name,
                        displayOrder = c.DisplayOrder,
                        itemOrder = items.Where(i => i.MenuCategoryId == c.Id)
                                         .OrderBy(i => i.Name)
                                         .Select(i => i.Id)
                                         .ToList()
                    })
            },
            items = new
            {
                byId = items.ToDictionary(
                    i => i.Id,
                    i => new
                    {
                        id = i.Id,
                        categoryId = i.MenuCategoryId,
                        name = i.Name,
                        description = i.Description,
                        price = new { amount = i.Amount, currency = i.Currency },
                        imageUrl = i.ImageUrl,
                        isAvailable = i.IsAvailable,
                        dietaryTagIds = i.DietaryTagIds.Select(t => t.Value).ToList(),
                        customizationGroups = i.AppliedCustomizations
                            .OrderBy(c => c.DisplayOrder)
                            .Select(c => new { groupId = c.CustomizationGroupId.Value, displayTitle = c.DisplayTitle, displayOrder = c.DisplayOrder })
                            .ToList()
                    })
            },
            customizationGroups = new
            {
                byId = groups.ToDictionary(
                    g => g.Id,
                    g => new
                    {
                        id = g.Id,
                        name = g.GroupName,
                        min = g.MinSelections,
                        max = g.MaxSelections,
                        options = g.Choices.Select(c => new
                        {
                            id = c.Id,
                            name = c.Name,
                            priceDelta = new { amount = c.Amount, currency = c.Currency },
                            isDefault = c.IsDefault,
                            displayOrder = c.DisplayOrder
                        }).ToList()
                    })
            },
            tagLegend = new
            {
                byId = tags.ToDictionary(t => t.Id, t => new { name = t.TagName, category = t.Category })
            }
        };

        var json = JsonSerializer.Serialize(doc, DomainJson.Options);
        return (json, now);
    }

    public async Task UpsertAsync(Guid restaurantId, string menuJson, DateTimeOffset lastRebuiltAt, CancellationToken ct = default)
    {
        var existing = await _db.FullMenuViews.SingleOrDefaultAsync(v => v.RestaurantId == restaurantId, ct);
        if (existing is null)
        {
            _db.FullMenuViews.Add(new FullMenuView
            {
                RestaurantId = restaurantId,
                MenuJson = menuJson,
                LastRebuiltAt = lastRebuiltAt
            });
        }
        else
        {
            existing.MenuJson = menuJson;
            existing.LastRebuiltAt = lastRebuiltAt;
        }
        await _db.SaveChangesAsync(ct);
    }
}
