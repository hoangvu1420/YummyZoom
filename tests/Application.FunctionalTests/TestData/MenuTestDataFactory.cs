using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;
using YummyZoom.Domain.MenuEntity;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.TagEntity.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.TestData;

/// <summary>
/// Factory for creating menu-related test scenarios using an options-based API (mirrors CouponTestDataFactory pattern).
/// </summary>
public static class MenuTestDataFactory
{
    public static async Task<MenuScenarioResult> CreateRestaurantWithMenuAsync(MenuScenarioOptions options)
    {
        var restaurantId = RestaurantId.Create(options.RestaurantId ?? TestDataFactory.DefaultRestaurantId);

        // Create or reuse a menu
        var menuResult = Menu.Create(
            restaurantId,
            options.MenuName ?? $"Menu-{Guid.NewGuid():N}",
            options.MenuDescription ?? "Test Menu");
        if (menuResult.IsFailure)
            throw new InvalidOperationException($"Failed to create menu: {menuResult.Error}");
        var menu = menuResult.Value;
        if (!options.EnabledMenu)
        {
            menu.Disable();
        }
        menu.ClearDomainEvents();
        await AddAsync(menu);

        var categoryIds = new List<Guid>();
        var itemIds = new List<Guid>();
        var groupIds = new List<Guid>();
        var tagIds = new List<Guid>();

        // Categories
        var categoryCount = options.CategoryCount;
        for (var i = 0; i < categoryCount; i++)
        {
            var (name, order) = options.CategoryGenerator?.Invoke(i) ?? ($"Category-{i+1}", i + 1);
            var categoryResult = MenuCategory.Create(menu.Id, name, order);
            if (categoryResult.IsFailure)
                throw new InvalidOperationException($"Failed to create category '{name}': {categoryResult.Error}");
            var category = categoryResult.Value;
            category.ClearDomainEvents();
            await AddAsync(category);
            categoryIds.Add(category.Id.Value);

            // Items for this category
            var itemsForCategory = options.ItemGenerator?.Invoke(category.Id.Value, i);
            if (itemsForCategory != null)
            {
                foreach (var itemOpt in itemsForCategory)
                {
                    var itemResult = MenuItem.Create(
                        restaurantId,
                        category.Id,
                        itemOpt.Name ?? $"Item-{i+1}-{Guid.NewGuid():N}",
                        itemOpt.Description ?? "Desc",
                        new Money(itemOpt.PriceAmount ?? 10m, itemOpt.PriceCurrency ?? "USD"),
                        itemOpt.ImageUrl,
                        itemOpt.IsAvailable ?? true,
                        dietaryTagIds: itemOpt.TagIds?.Select(TagId.Create).ToList(),
                        appliedCustomizations: itemOpt.Customizations?.Select(c => AppliedCustomization.Create(CustomizationGroupId.Create(c.GroupId), c.DisplayTitle ?? "", c.DisplayOrder ?? 1)).ToList());
                    if (itemResult.IsFailure)
                        throw new InvalidOperationException($"Failed to create item: {itemResult.Error}");
                    var item = itemResult.Value;
                    item.ClearDomainEvents();
                    await AddAsync(item);
                    itemIds.Add(item.Id.Value);
                    if (itemOpt.TagIds != null) tagIds.AddRange(itemOpt.TagIds);
                    if (itemOpt.Customizations != null) groupIds.AddRange(itemOpt.Customizations.Select(c => c.GroupId));
                }
            }
        }

        // Optionally create tags explicitly (ids used above should exist already depending on tests)
        // For now, we only return ids; explicit tag creation helpers can be added if needed.

        // Soft-delete selectors
        foreach (var idx in options.SoftDeleteCategoryIndexes ?? Array.Empty<int>())
        {
            if (idx >= 0 && idx < categoryIds.Count)
            {
                var catId = MenuCategoryId.Create(categoryIds[idx]);
                var category = await FindAsync<MenuCategory>(catId);
                category!.MarkAsDeleted(DateTimeOffset.UtcNow);
                category.ClearDomainEvents();
                await UpdateAsync(category);
            }
        }
        foreach (var idx in options.SoftDeleteItemIndexes ?? Array.Empty<int>())
        {
            if (idx >= 0 && idx < itemIds.Count)
            {
                var itemId = MenuItemId.Create(itemIds[idx]);
                var item = await FindAsync<MenuItem>(itemId);
                item!.MarkAsDeleted(DateTimeOffset.UtcNow);
                item.ClearDomainEvents();
                await UpdateAsync(item);
            }
        }

        return new MenuScenarioResult(
            restaurantId.Value,
            menu.Id.Value,
            categoryIds,
            itemIds,
            groupIds.Distinct().ToList(),
            tagIds.Distinct().ToList());
    }

    public static async Task DisableMenuAsync(Guid menuId)
    {
        var id = MenuId.Create(menuId);
        var menu = await FindAsync<Menu>(id);
        menu!.Disable();
        menu.ClearDomainEvents();
        await UpdateAsync(menu);
    }

    public static async Task EnableMenuAsync(Guid menuId)
    {
        var id = MenuId.Create(menuId);
        var menu = await FindAsync<Menu>(id);
        menu!.Enable();
        menu.ClearDomainEvents();
        await UpdateAsync(menu);
    }

    public static async Task SoftDeleteCategoryAsync(Guid categoryId)
    {
        var id = MenuCategoryId.Create(categoryId);
        var category = await FindAsync<MenuCategory>(id);
        category!.MarkAsDeleted(DateTimeOffset.UtcNow);
        category.ClearDomainEvents();
        await UpdateAsync(category);
    }

    public static async Task SoftDeleteItemAsync(Guid itemId)
    {
        var id = MenuItemId.Create(itemId);
        var item = await FindAsync<MenuItem>(id);
        item!.MarkAsDeleted(DateTimeOffset.UtcNow);
        item.ClearDomainEvents();
        await UpdateAsync(item);
    }

    public static async Task RenameCategoryAsync(Guid categoryId, string newName)
    {
        var id = MenuCategoryId.Create(categoryId);
        var category = await FindAsync<MenuCategory>(id);
        category!.UpdateName(newName);
        category.ClearDomainEvents();
        await UpdateAsync(category);
    }

    public static async Task RenameItemAsync(Guid itemId, string newName)
    {
        var id = MenuItemId.Create(itemId);
        var item = await FindAsync<MenuItem>(id);
        item!.UpdateDetails(newName, item.Description);
        item.ClearDomainEvents();
        await UpdateAsync(item);
    }

    public static async Task AttachTagsToItemAsync(Guid itemId, IReadOnlyList<Guid> tagIds)
    {
        var id = MenuItemId.Create(itemId);
        var item = await FindAsync<MenuItem>(id);
        item!.SetDietaryTags(tagIds.Select(TagId.Create).ToList());
        item.ClearDomainEvents();
        await UpdateAsync(item);
    }

    public static async Task AttachCustomizationsToItemAsync(Guid itemId, IReadOnlyList<(Guid GroupId, string Title, int Order)> customizations)
    {
        var id = MenuItemId.Create(itemId);
        var item = await FindAsync<MenuItem>(id);
        foreach (var c in customizations)
        {
            var assign = item!.AssignCustomizationGroup(AppliedCustomization.Create(CustomizationGroupId.Create(c.GroupId), c.Title, c.Order));
            if (assign.IsFailure)
                throw new InvalidOperationException(assign.Error.Code);
        }
        item!.ClearDomainEvents();
        await UpdateAsync(item);
    }
}

public sealed class MenuScenarioOptions
{
    public Guid? RestaurantId { get; set; }
    public string? MenuName { get; set; }
    public string? MenuDescription { get; set; }
    public bool EnabledMenu { get; set; } = true;
    public int CategoryCount { get; set; } = 3;
    public Func<int, (string Name, int DisplayOrder)>? CategoryGenerator { get; set; }
    public Func<Guid, int, IEnumerable<ItemOptions>>? ItemGenerator { get; set; }
    public int[]? SoftDeleteCategoryIndexes { get; set; }
    public int[]? SoftDeleteItemIndexes { get; set; }
}

public sealed class ItemOptions
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? PriceAmount { get; set; }
    public string? PriceCurrency { get; set; }
    public string? ImageUrl { get; set; }
    public bool? IsAvailable { get; set; }
    public List<Guid>? TagIds { get; set; }
    public List<CustomizationLink>? Customizations { get; set; }
}

public sealed class CustomizationLink
{
    public Guid GroupId { get; set; }
    public string? DisplayTitle { get; set; }
    public int? DisplayOrder { get; set; }
}

public sealed record MenuScenarioResult(
    Guid RestaurantId,
    Guid MenuId,
    List<Guid> CategoryIds,
    List<Guid> ItemIds,
    List<Guid> GroupIds,
    List<Guid> TagIds);
