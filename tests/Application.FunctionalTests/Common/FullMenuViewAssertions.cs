using System.Text.Json;
using FluentAssertions;
using YummyZoom.Infrastructure.Persistence.ReadModels.FullMenu;

namespace YummyZoom.Application.FunctionalTests.Common;

/// <summary>
/// Helper methods for asserting FullMenuView JSON content in functional tests.
/// Provides utilities to parse and validate the structure and content of the denormalized menu view.
/// </summary>
public static class FullMenuViewAssertions
{
    /// <summary>
    /// Gets the root JSON element from a FullMenuView's MenuJson.
    /// </summary>
    public static JsonElement GetRoot(FullMenuView view)
    {
        view.Should().NotBeNull();
        view.MenuJson.Should().NotBeNullOrWhiteSpace();
        
        using var doc = JsonDocument.Parse(view.MenuJson);
        return doc.RootElement.Clone();
    }

    // ----- Customization Groups (view.customizationGroups) -----

    public static bool HasCustomizationGroup(FullMenuView view, Guid groupId)
    {
        var root = GetRoot(view);
        var groupsById = root.GetProperty("customizationGroups").GetProperty("byId");
        return groupsById.TryGetProperty(groupId.ToString(), out _);
    }

    public static JsonElement GetCustomizationGroup(FullMenuView view, Guid groupId)
    {
        var root = GetRoot(view);
        var groupsById = root.GetProperty("customizationGroups").GetProperty("byId");
        if (!groupsById.TryGetProperty(groupId.ToString(), out var group))
            throw new InvalidOperationException($"CustomizationGroup {groupId} not found in menu view");
        return group;
    }

    public static void ShouldHaveCustomizationGroup(this FullMenuView view, Guid groupId, string name, int min, int max, string because = "")
    {
        HasCustomizationGroup(view, groupId).Should().BeTrue(because.Length > 0 ? because : $"group {groupId} should exist in menu view");
        var group = GetCustomizationGroup(view, groupId);
        group.GetProperty("id").GetGuid().Should().Be(groupId);
        group.GetProperty("name").GetString().Should().Be(name);
        group.GetProperty("min").GetInt32().Should().Be(min);
        group.GetProperty("max").GetInt32().Should().Be(max);
    }

    public static void ShouldNotHaveCustomizationGroup(this FullMenuView view, Guid groupId, string because = "")
    {
        HasCustomizationGroup(view, groupId).Should().BeFalse(because.Length > 0 ? because : $"group {groupId} should not exist in menu view");
    }

    // ----- Customization Group Options -----

    public static IReadOnlyList<Guid> GetGroupOptionIds(FullMenuView view, Guid groupId)
    {
        var group = GetCustomizationGroup(view, groupId);
        return group.GetProperty("options").EnumerateArray()
            .OrderBy(e => e.GetProperty("displayOrder").GetInt32())
            .ThenBy(e => e.GetProperty("name").GetString())
            .Select(e => e.GetProperty("id").GetGuid())
            .ToList();
    }

    public static IReadOnlyList<(Guid Id, string Name, decimal Amount, string Currency, bool IsDefault, int DisplayOrder)> GetGroupOptions(FullMenuView view, Guid groupId)
    {
        var group = GetCustomizationGroup(view, groupId);
        return group.GetProperty("options").EnumerateArray()
            .Select(e => (
                Id: e.GetProperty("id").GetGuid(),
                Name: e.GetProperty("name").GetString()!,
                Amount: e.GetProperty("priceDelta").GetProperty("amount").GetDecimal(),
                Currency: e.GetProperty("priceDelta").GetProperty("currency").GetString()!,
                IsDefault: e.GetProperty("isDefault").GetBoolean(),
                DisplayOrder: e.GetProperty("displayOrder").GetInt32()
            ))
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToList();
    }

    public static void ShouldHaveGroupOption(this FullMenuView view, Guid groupId, Guid optionId, string name, decimal priceAmount, string currency, bool isDefault, int displayOrder, string because = "")
    {
        var options = GetGroupOptions(view, groupId);
        options.Should().Contain(o => o.Id == optionId, because.Length > 0 ? because : $"group {groupId} should contain option {optionId}");
        var opt = options.First(o => o.Id == optionId);
        opt.Name.Should().Be(name);
        opt.Amount.Should().Be(priceAmount);
        opt.Currency.Should().Be(currency);
        opt.IsDefault.Should().Be(isDefault);
        opt.DisplayOrder.Should().Be(displayOrder);
    }

    public static void ShouldNotHaveGroupOption(this FullMenuView view, Guid groupId, Guid optionId, string because = "")
    {
        var ids = GetGroupOptionIds(view, groupId);
        ids.Should().NotContain(optionId, because.Length > 0 ? because : $"group {groupId} should not contain option {optionId}");
    }

    public static void ShouldHaveGroupOptionsInOrder(this FullMenuView view, Guid groupId, params Guid[] optionIds)
    {
        var ids = GetGroupOptionIds(view, groupId);
        ids.Should().Equal(optionIds);
    }

    /// <summary>
    /// Checks if an item exists in the items.byId section of the menu view.
    /// </summary>
    public static bool HasItem(FullMenuView view, Guid itemId)
    {
        var root = GetRoot(view);
        var itemsById = root.GetProperty("items").GetProperty("byId");
        return itemsById.TryGetProperty(itemId.ToString(), out _);
    }

    /// <summary>
    /// Checks if a category's itemOrder contains the specified item.
    /// </summary>
    public static bool CategoryHasItem(FullMenuView view, Guid categoryId, Guid itemId)
    {
        var root = GetRoot(view);
        var categoriesById = root.GetProperty("categories").GetProperty("byId");
        
        if (!categoriesById.TryGetProperty(categoryId.ToString(), out var category))
            return false;
            
        var itemOrder = category.GetProperty("itemOrder").EnumerateArray()
            .Select(e => e.GetGuid())
            .ToList();
            
        return itemOrder.Contains(itemId);
    }

    /// <summary>
    /// Gets the JSON element for a specific item from items.byId.
    /// Throws if the item doesn't exist.
    /// </summary>
    public static JsonElement GetItem(FullMenuView view, Guid itemId)
    {
        var root = GetRoot(view);
        var itemsById = root.GetProperty("items").GetProperty("byId");
        
        if (!itemsById.TryGetProperty(itemId.ToString(), out var item))
            throw new InvalidOperationException($"Item {itemId} not found in menu view");
            
        return item;
    }

    /// <summary>
    /// Gets the price amount for a specific item.
    /// </summary>
    public static decimal GetItemPriceAmount(FullMenuView view, Guid itemId)
    {
        var item = GetItem(view, itemId);
        return item.GetProperty("price").GetProperty("amount").GetDecimal();
    }

    /// <summary>
    /// Gets the price currency for a specific item.
    /// </summary>
    public static string GetItemPriceCurrency(FullMenuView view, Guid itemId)
    {
        var item = GetItem(view, itemId);
        return item.GetProperty("price").GetProperty("currency").GetString()!;
    }

    /// <summary>
    /// Gets the availability status for a specific item.
    /// </summary>
    public static bool GetItemAvailability(FullMenuView view, Guid itemId)
    {
        var item = GetItem(view, itemId);
        return item.GetProperty("isAvailable").GetBoolean();
    }

    /// <summary>
    /// Gets the name for a specific item.
    /// </summary>
    public static string GetItemName(FullMenuView view, Guid itemId)
    {
        var item = GetItem(view, itemId);
        return item.GetProperty("name").GetString()!;
    }

    /// <summary>
    /// Gets the description for a specific item.
    /// </summary>
    public static string GetItemDescription(FullMenuView view, Guid itemId)
    {
        var item = GetItem(view, itemId);
        return item.GetProperty("description").GetString()!;
    }

    /// <summary>
    /// Gets the image URL for a specific item (may be null).
    /// </summary>
    public static string? GetItemImageUrl(FullMenuView view, Guid itemId)
    {
        var item = GetItem(view, itemId);
        var imageUrlElement = item.GetProperty("imageUrl");
        return imageUrlElement.ValueKind == JsonValueKind.Null ? null : imageUrlElement.GetString();
    }

    /// <summary>
    /// Gets the dietary tag IDs for a specific item.
    /// </summary>
    public static IReadOnlyList<Guid> GetItemDietaryTagIds(FullMenuView view, Guid itemId)
    {
        var item = GetItem(view, itemId);
        return item.GetProperty("dietaryTagIds").EnumerateArray()
            .Select(e => e.GetGuid())
            .ToList();
    }

    /// <summary>
    /// Gets the customization group IDs for a specific item.
    /// </summary>
    public static IReadOnlyList<Guid> GetItemCustomizationGroupIds(FullMenuView view, Guid itemId)
    {
        var item = GetItem(view, itemId);
        return item.GetProperty("customizationGroups").EnumerateArray()
            .Select(e => e.GetProperty("groupId").GetGuid())
            .ToList();
    }

    /// <summary>
    /// Gets the customization groups with their display information for a specific item.
    /// </summary>
    public static IReadOnlyList<(Guid GroupId, string DisplayTitle, int DisplayOrder)> GetItemCustomizationGroups(FullMenuView view, Guid itemId)
    {
        var item = GetItem(view, itemId);
        return item.GetProperty("customizationGroups").EnumerateArray()
            .Select(e => (
                GroupId: e.GetProperty("groupId").GetGuid(),
                DisplayTitle: e.GetProperty("displayTitle").GetString()!,
                DisplayOrder: e.GetProperty("displayOrder").GetInt32()
            ))
            .ToList();
    }

    public static void ShouldHaveItemCustomizationGroup(this FullMenuView view, Guid itemId, Guid groupId, string displayTitle, int displayOrder, string because = "")
    {
        var groups = GetItemCustomizationGroups(view, itemId);
        groups.Should().Contain(g => g.GroupId == groupId, because.Length > 0 ? because : $"item {itemId} should contain customization group {groupId}");
        var g1 = groups.First(g => g.GroupId == groupId);
        g1.DisplayTitle.Should().Be(displayTitle);
        g1.DisplayOrder.Should().Be(displayOrder);
    }

    public static void ShouldNotHaveItemCustomizationGroup(this FullMenuView view, Guid itemId, Guid groupId, string because = "")
    {
        var groups = GetItemCustomizationGroups(view, itemId).Select(g => g.GroupId).ToList();
        groups.Should().NotContain(groupId, because.Length > 0 ? because : $"item {itemId} should not contain customization group {groupId}");
    }

    /// <summary>
    /// Gets the item order for a specific category.
    /// </summary>
    public static IReadOnlyList<Guid> GetCategoryItemOrder(FullMenuView view, Guid categoryId)
    {
        var root = GetRoot(view);
        var categoriesById = root.GetProperty("categories").GetProperty("byId");
        
        if (!categoriesById.TryGetProperty(categoryId.ToString(), out var category))
            throw new InvalidOperationException($"Category {categoryId} not found in menu view");
            
        return category.GetProperty("itemOrder").EnumerateArray()
            .Select(e => e.GetGuid())
            .ToList();
    }

    /// <summary>
    /// Asserts that an item exists in the menu view and is referenced by its category.
    /// </summary>
    public static void ShouldHaveItem(this FullMenuView view, Guid itemId, Guid categoryId, string because = "")
    {
        HasItem(view, itemId).Should().BeTrue(because.Length > 0 ? because : $"item {itemId} should exist in menu view");
        CategoryHasItem(view, categoryId, itemId).Should().BeTrue(because.Length > 0 ? because : $"category {categoryId} should reference item {itemId}");
    }

    /// <summary>
    /// Asserts that an item does not exist in the menu view and is not referenced by its category.
    /// </summary>
    public static void ShouldNotHaveItem(this FullMenuView view, Guid itemId, Guid categoryId, string because = "")
    {
        HasItem(view, itemId).Should().BeFalse(because.Length > 0 ? because : $"item {itemId} should not exist in menu view");
        CategoryHasItem(view, categoryId, itemId).Should().BeFalse(because.Length > 0 ? because : $"category {categoryId} should not reference item {itemId}");
    }

    /// <summary>
    /// Asserts that an item has the expected field values.
    /// </summary>
    public static void ShouldHaveItemWithValues(this FullMenuView view, Guid itemId, 
        string? expectedName = null, 
        string? expectedDescription = null, 
        decimal? expectedPriceAmount = null,
        string? expectedCurrency = null,
        bool? expectedAvailability = null,
        string? expectedImageUrl = null,
        string because = "")
    {
        var item = GetItem(view, itemId);
        
        if (expectedName != null)
            GetItemName(view, itemId).Should().Be(expectedName, because.Length > 0 ? because : $"item {itemId} should have expected name");
            
        if (expectedDescription != null)
            GetItemDescription(view, itemId).Should().Be(expectedDescription, because.Length > 0 ? because : $"item {itemId} should have expected description");
            
        if (expectedPriceAmount.HasValue)
            GetItemPriceAmount(view, itemId).Should().Be(expectedPriceAmount.Value, because.Length > 0 ? because : $"item {itemId} should have expected price amount");
            
        if (expectedCurrency != null)
            GetItemPriceCurrency(view, itemId).Should().Be(expectedCurrency, because.Length > 0 ? because : $"item {itemId} should have expected currency");
            
        if (expectedAvailability.HasValue)
            GetItemAvailability(view, itemId).Should().Be(expectedAvailability.Value, because.Length > 0 ? because : $"item {itemId} should have expected availability");
            
        if (expectedImageUrl != null)
            GetItemImageUrl(view, itemId).Should().Be(expectedImageUrl, because.Length > 0 ? because : $"item {itemId} should have expected image URL");
    }
}
