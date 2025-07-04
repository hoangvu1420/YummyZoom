using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.MenuAggregate;
using YummyZoom.Domain.MenuAggregate.Events;
using YummyZoom.Domain.MenuAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.MenuAggregate;

/// <summary>
/// Tests for Menu aggregate item management functionality.
/// </summary>
[TestFixture]
public class MenuItemTests
{
    private static readonly RestaurantId DefaultRestaurantId = RestaurantId.CreateUnique();
    private const string DefaultMenuName = "Test Menu";
    private const string DefaultMenuDescription = "Test Description";
    private const string DefaultCategoryName = "Test Category";
    private const string DefaultItemName = "Test Item";
    private const string DefaultItemDescription = "Test Item Description";
    private static readonly Money DefaultBasePrice = new Money(12.99m, Currencies.Default);

    #region AddItemToCategory() Method Tests

    [Test]
    public void AddItemToCategory_WithValidInputs_ShouldSucceedAndAddItem()
    {
        // Arrange
        var menu = CreateMenuWithCategory();
        var categoryId = menu.Categories.First().Id;
        menu.ClearDomainEvents();

        // Act
        var result = menu.AddItemToCategory(
            categoryId,
            DefaultItemName,
            DefaultItemDescription,
            DefaultBasePrice);

        // Assert
        result.ShouldBeSuccessful();
        var item = result.Value;
        
        item.Name.Should().Be(DefaultItemName);
        item.Description.Should().Be(DefaultItemDescription);
        item.BasePrice.Should().Be(DefaultBasePrice);
        item.IsAvailable.Should().BeTrue();
        
        menu.TotalItemCount.Should().Be(1);
        menu.AvailableItemCount.Should().Be(1);
        menu.UnavailableItemCount.Should().Be(0);
        menu.IsMenuEmpty.Should().BeFalse();
        
        menu.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<MenuItemAdded>()
            .Which.Should().Match<MenuItemAdded>(e => 
                e.MenuId == menu.Id && e.CategoryId == categoryId && e.ItemId == item.Id);
    }

    [Test]
    public void AddItemToCategory_WithCustomAvailability_ShouldCreateItemWithCorrectAvailability()
    {
        // Arrange
        var menu = CreateMenuWithCategory();
        var categoryId = menu.Categories.First().Id;

        // Act
        var result = menu.AddItemToCategory(
            categoryId,
            DefaultItemName,
            DefaultItemDescription,
            DefaultBasePrice,
            isAvailable: false);

        // Assert
        result.ShouldBeSuccessful();
        var item = result.Value;
        
        item.IsAvailable.Should().BeFalse();
        menu.AvailableItemCount.Should().Be(0);
        menu.UnavailableItemCount.Should().Be(1);
    }

    [Test]
    public void AddItemToCategory_WithImageUrl_ShouldCreateItemWithImageUrl()
    {
        // Arrange
        var menu = CreateMenuWithCategory();
        var categoryId = menu.Categories.First().Id;
        var imageUrl = "https://example.com/image.jpg";

        // Act
        var result = menu.AddItemToCategory(
            categoryId,
            DefaultItemName,
            DefaultItemDescription,
            DefaultBasePrice,
            imageUrl);

        // Assert
        result.ShouldBeSuccessful();
        var item = result.Value;
        
        item.ImageUrl.Should().Be(imageUrl);
    }

    [Test]
    public void AddItemToCategory_WithNonExistentCategory_ShouldFail()
    {
        // Arrange
        var menu = CreateValidMenu();
        var nonExistentCategoryId = MenuCategoryId.CreateUnique();

        // Act
        var result = menu.AddItemToCategory(
            nonExistentCategoryId,
            DefaultItemName,
            DefaultItemDescription,
            DefaultBasePrice);

        // Assert
        result.ShouldBeFailure("Menu.CategoryNotFound");
    }

    [Test]
    public void AddItemToCategory_WithInvalidItemName_ShouldFail()
    {
        // Arrange
        var menu = CreateMenuWithCategory();
        var categoryId = menu.Categories.First().Id;

        // Act & Assert - Null name
        var nullResult = menu.AddItemToCategory(categoryId, null!, DefaultItemDescription, DefaultBasePrice);
        nullResult.ShouldBeFailure("Menu.InvalidItemName");

        // Act & Assert - Empty name
        var emptyResult = menu.AddItemToCategory(categoryId, "", DefaultItemDescription, DefaultBasePrice);
        emptyResult.ShouldBeFailure("Menu.InvalidItemName");

        // Act & Assert - Whitespace name
        var whitespaceResult = menu.AddItemToCategory(categoryId, "   ", DefaultItemDescription, DefaultBasePrice);
        whitespaceResult.ShouldBeFailure("Menu.InvalidItemName");
    }

    [Test]
    public void AddItemToCategory_WithInvalidItemDescription_ShouldFail()
    {
        // Arrange
        var menu = CreateMenuWithCategory();
        var categoryId = menu.Categories.First().Id;

        // Act & Assert - Null description
        var nullResult = menu.AddItemToCategory(categoryId, DefaultItemName, null!, DefaultBasePrice);
        nullResult.ShouldBeFailure("Menu.InvalidItemDescription");

        // Act & Assert - Empty description
        var emptyResult = menu.AddItemToCategory(categoryId, DefaultItemName, "", DefaultBasePrice);
        emptyResult.ShouldBeFailure("Menu.InvalidItemDescription");

        // Act & Assert - Whitespace description
        var whitespaceResult = menu.AddItemToCategory(categoryId, DefaultItemName, "   ", DefaultBasePrice);
        whitespaceResult.ShouldBeFailure("Menu.InvalidItemDescription");
    }

    [Test]
    public void AddItemToCategory_WithNegativePrice_ShouldFail()
    {
        // Arrange
        var menu = CreateMenuWithCategory();
        var categoryId = menu.Categories.First().Id;
        var negativePrice = new Money(-5.00m, Currencies.Default);

        // Act
        var result = menu.AddItemToCategory(
            categoryId,
            DefaultItemName,
            DefaultItemDescription,
            negativePrice);

        // Assert
        result.ShouldBeFailure("Menu.NegativeMenuItemPrice");
    }

    [Test]
    public void AddItemToCategory_WithDuplicateItemNameInCategory_ShouldFail()
    {
        // Arrange
        var menu = CreateMenuWithCategory();
        var categoryId = menu.Categories.First().Id;
        menu.AddItemToCategory(categoryId, DefaultItemName, DefaultItemDescription, DefaultBasePrice);

        // Act - Try to add item with same name (case-insensitive)
        var result = menu.AddItemToCategory(categoryId, DefaultItemName.ToUpper(), "Different Description", DefaultBasePrice);

        // Assert
        result.ShouldBeFailure("Menu.DuplicateItemName");
        menu.TotalItemCount.Should().Be(1); // Should still have only the first item
    }

    #endregion

    #region RemoveItemFromCategory() Method Tests

    [Test]
    public void RemoveItemFromCategory_WithValidItem_ShouldSucceedAndRemoveItem()
    {
        // Arrange
        var menu = CreateMenuWithCategoryAndItem();
        var categoryId = menu.Categories.First().Id;
        var itemId = menu.GetAllItems().First().Id;
        menu.ClearDomainEvents();

        // Act
        var result = menu.RemoveItemFromCategory(categoryId, itemId);

        // Assert
        result.ShouldBeSuccessful();
        menu.TotalItemCount.Should().Be(0);
        menu.AvailableItemCount.Should().Be(0);
        menu.IsMenuEmpty.Should().BeTrue(); // Has categories but no items
        
        menu.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<MenuItemRemoved>()
            .Which.Should().Match<MenuItemRemoved>(e => 
                e.MenuId == menu.Id && e.CategoryId == categoryId && e.ItemId == itemId);
    }

    [Test]
    public void RemoveItemFromCategory_WithNonExistentCategory_ShouldFail()
    {
        // Arrange
        var menu = CreateMenuWithCategoryAndItem();
        var nonExistentCategoryId = MenuCategoryId.CreateUnique();
        var itemId = menu.GetAllItems().First().Id;

        // Act
        var result = menu.RemoveItemFromCategory(nonExistentCategoryId, itemId);

        // Assert
        result.ShouldBeFailure("Menu.CategoryNotFound");
    }

    [Test]
    public void RemoveItemFromCategory_WithNonExistentItem_ShouldFail()
    {
        // Arrange
        var menu = CreateMenuWithCategory();
        var categoryId = menu.Categories.First().Id;
        var nonExistentItemId = MenuItemId.CreateUnique();

        // Act
        var result = menu.RemoveItemFromCategory(categoryId, nonExistentItemId);

        // Assert
        result.ShouldBeFailure("Menu.ItemNotFound");
    }

    #endregion

    #region GetMenuItem() Method Tests

    [Test]
    public void GetMenuItem_WithValidCategoryAndItem_ShouldSucceedAndReturnItem()
    {
        // Arrange
        var menu = CreateMenuWithCategoryAndItem();
        var categoryId = menu.Categories.First().Id;
        var itemId = menu.GetAllItems().First().Id;

        // Act
        var result = menu.GetMenuItem(categoryId, itemId);

        // Assert
        result.ShouldBeSuccessful();
        var item = result.Value;
        
        item.Id.Should().Be(itemId);
        item.Name.Should().Be(DefaultItemName);
    }

    [Test]
    public void GetMenuItem_WithNonExistentCategory_ShouldFail()
    {
        // Arrange
        var menu = CreateMenuWithCategoryAndItem();
        var nonExistentCategoryId = MenuCategoryId.CreateUnique();
        var itemId = menu.GetAllItems().First().Id;

        // Act
        var result = menu.GetMenuItem(nonExistentCategoryId, itemId);

        // Assert
        result.ShouldBeFailure("Menu.CategoryNotFound");
    }

    [Test]
    public void GetMenuItem_WithNonExistentItem_ShouldFail()
    {
        // Arrange
        var menu = CreateMenuWithCategory();
        var categoryId = menu.Categories.First().Id;
        var nonExistentItemId = MenuItemId.CreateUnique();

        // Act
        var result = menu.GetMenuItem(categoryId, nonExistentItemId);

        // Assert
        result.ShouldBeFailure("Menu.ItemNotFound");
    }

    #endregion

    #region GetMenuItemFromAnyCategory() Method Tests

    [Test]
    public void GetMenuItemFromAnyCategory_WithValidItem_ShouldSucceedAndReturnItem()
    {
        // Arrange
        var menu = CreateMenuWithCategoryAndItem();
        var itemId = menu.GetAllItems().First().Id;

        // Act
        var result = menu.GetMenuItemFromAnyCategory(itemId);

        // Assert
        result.ShouldBeSuccessful();
        var item = result.Value;
        
        item.Id.Should().Be(itemId);
        item.Name.Should().Be(DefaultItemName);
    }

    [Test]
    public void GetMenuItemFromAnyCategory_WithNonExistentItem_ShouldFail()
    {
        // Arrange
        var menu = CreateMenuWithCategory();
        var nonExistentItemId = MenuItemId.CreateUnique();

        // Act
        var result = menu.GetMenuItemFromAnyCategory(nonExistentItemId);

        // Assert
        result.ShouldBeFailure("Menu.ItemNotFound");
    }

    #endregion

    #region UpdateMenuItem() Method Tests

    [Test]
    public void UpdateMenuItem_WithValidInputs_ShouldSucceedAndUpdateItem()
    {
        // Arrange
        var menu = CreateMenuWithCategoryAndItem();
        var categoryId = menu.Categories.First().Id;
        var itemId = menu.GetAllItems().First().Id;
        var newName = "Updated Item Name";
        var newDescription = "Updated Item Description";
        var newPrice = new Money(15.99m, Currencies.Default);
        var newImageUrl = "https://example.com/new-image.jpg";
        menu.ClearDomainEvents();

        // Act
        var result = menu.UpdateMenuItem(categoryId, itemId, newName, newDescription, newPrice, newImageUrl);

        // Assert
        result.ShouldBeSuccessful();
        
        var item = menu.GetMenuItem(categoryId, itemId).Value;
        item.Name.Should().Be(newName);
        item.Description.Should().Be(newDescription);
        item.BasePrice.Should().Be(newPrice);
        item.ImageUrl.Should().Be(newImageUrl);
        
        menu.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<MenuItemUpdated>()
            .Which.Should().Match<MenuItemUpdated>(e => 
                e.MenuId == menu.Id && e.CategoryId == categoryId && e.ItemId == itemId);
    }

    [Test]
    public void UpdateMenuItem_WithDuplicateNameInCategory_ShouldFail()
    {
        // Arrange
        var menu = CreateMenuWithCategory();
        var categoryId = menu.Categories.First().Id;
        var item1 = menu.AddItemToCategory(categoryId, "Item 1", DefaultItemDescription, DefaultBasePrice).Value;
        var item2 = menu.AddItemToCategory(categoryId, "Item 2", DefaultItemDescription, DefaultBasePrice).Value;

        // Act - Try to update item2 with item1's name
        var result = menu.UpdateMenuItem(categoryId, item2.Id, "Item 1", "New Description", DefaultBasePrice);

        // Assert
        result.ShouldBeFailure("Menu.DuplicateItemName");
    }

    #endregion

    #region UpdateMenuItemPrice() Method Tests

    [Test]
    public void UpdateMenuItemPrice_WithValidInputs_ShouldSucceedAndUpdatePrice()
    {
        // Arrange
        var menu = CreateMenuWithCategoryAndItem();
        var categoryId = menu.Categories.First().Id;
        var itemId = menu.GetAllItems().First().Id;
        var newPrice = new Money(19.99m, Currencies.Default);
        menu.ClearDomainEvents();

        // Act
        var result = menu.UpdateMenuItemPrice(categoryId, itemId, newPrice);

        // Assert
        result.ShouldBeSuccessful();
        
        var item = menu.GetMenuItem(categoryId, itemId).Value;
        item.BasePrice.Should().Be(newPrice);
        
        menu.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<MenuItemUpdated>();
    }

    [Test]
    public void UpdateMenuItemPrice_WithNegativePrice_ShouldFail()
    {
        // Arrange
        var menu = CreateMenuWithCategoryAndItem();
        var categoryId = menu.Categories.First().Id;
        var itemId = menu.GetAllItems().First().Id;
        var negativePrice = new Money(-5.00m, Currencies.Default);

        // Act
        var result = menu.UpdateMenuItemPrice(categoryId, itemId, negativePrice);

        // Assert
        result.ShouldBeFailure("Menu.NegativeMenuItemPrice");
    }

    #endregion

    #region ToggleMenuItemAvailability() Method Tests

    [Test]
    public void ToggleMenuItemAvailability_WhenAvailable_ShouldMakeUnavailable()
    {
        // Arrange
        var menu = CreateMenuWithCategoryAndItem();
        var categoryId = menu.Categories.First().Id;
        var itemId = menu.GetAllItems().First().Id;
        menu.ClearDomainEvents();

        // Verify initial state
        var initialItem = menu.GetMenuItem(categoryId, itemId).Value;
        initialItem.IsAvailable.Should().BeTrue();

        // Act
        var result = menu.ToggleMenuItemAvailability(categoryId, itemId);

        // Assert
        result.ShouldBeSuccessful();
        
        var updatedItem = menu.GetMenuItem(categoryId, itemId).Value;
        updatedItem.IsAvailable.Should().BeFalse();
        
        menu.AvailableItemCount.Should().Be(0);
        menu.UnavailableItemCount.Should().Be(1);
        
        menu.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<MenuItemUpdated>();
    }

    [Test]
    public void ToggleMenuItemAvailability_WhenUnavailable_ShouldMakeAvailable()
    {
        // Arrange
        var menu = CreateMenuWithCategory();
        var categoryId = menu.Categories.First().Id;
        var itemResult = menu.AddItemToCategory(categoryId, DefaultItemName, DefaultItemDescription, DefaultBasePrice, isAvailable: false);
        var itemId = itemResult.Value.Id;
        menu.ClearDomainEvents();

        // Verify initial state
        var initialItem = menu.GetMenuItem(categoryId, itemId).Value;
        initialItem.IsAvailable.Should().BeFalse();

        // Act
        var result = menu.ToggleMenuItemAvailability(categoryId, itemId);

        // Assert
        result.ShouldBeSuccessful();
        
        var updatedItem = menu.GetMenuItem(categoryId, itemId).Value;
        updatedItem.IsAvailable.Should().BeTrue();
        
        menu.AvailableItemCount.Should().Be(1);
        menu.UnavailableItemCount.Should().Be(0);
        
        menu.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<MenuItemUpdated>();
    }

    #endregion

    #region Helper Methods

    private static Menu CreateValidMenu()
    {
        return Menu.Create(DefaultRestaurantId, DefaultMenuName, DefaultMenuDescription).Value;
    }

    private static Menu CreateMenuWithCategory()
    {
        var menu = CreateValidMenu();
        menu.AddCategory(DefaultCategoryName, "Test Category Description", 1);
        return menu;
    }

    private static Menu CreateMenuWithCategoryAndItem()
    {
        var menu = CreateMenuWithCategory();
        var categoryId = menu.Categories.First().Id;
        menu.AddItemToCategory(categoryId, DefaultItemName, DefaultItemDescription, DefaultBasePrice);
        return menu;
    }

    #endregion
}
