using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.MenuAggregate;
using YummyZoom.Domain.MenuAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.MenuAggregate;

/// <summary>
/// Tests for Menu aggregate query and search functionality.
/// </summary>
[TestFixture]
public class MenuQueryTests
{
    private static readonly RestaurantId DefaultRestaurantId = RestaurantId.CreateUnique();
    private const string DefaultMenuName = "Test Menu";
    private const string DefaultMenuDescription = "Test Description";
    private static readonly Money DefaultBasePrice = new Money(12.99m, Currencies.Default);

    #region GetCategoriesOrderedByDisplayOrder() Tests

    [Test]
    public void GetCategoriesOrderedByDisplayOrder_WithMultipleCategories_ShouldReturnOrderedByDisplayOrder()
    {
        // Arrange
        var menu = CreateValidMenu();
        var category3 = menu.AddCategory("Category C", "Description C", 3).Value;
        var category1 = menu.AddCategory("Category A", "Description A", 1).Value;
        var category2 = menu.AddCategory("Category B", "Description B", 2).Value;

        // Act
        var orderedCategories = menu.GetCategoriesOrderedByDisplayOrder();

        // Assert
        orderedCategories.Should().HaveCount(3);
        orderedCategories[0].Should().Be(category1);
        orderedCategories[1].Should().Be(category2);
        orderedCategories[2].Should().Be(category3);
    }

    [Test]
    public void GetCategoriesOrderedByDisplayOrder_WithNoCategories_ShouldReturnEmptyList()
    {
        // Arrange
        var menu = CreateValidMenu();

        // Act
        var orderedCategories = menu.GetCategoriesOrderedByDisplayOrder();

        // Assert
        orderedCategories.Should().BeEmpty();
    }

    #endregion

    #region GetAllItems() Tests

    [Test]
    public void GetAllItems_WithItemsInMultipleCategories_ShouldReturnAllItems()
    {
        // Arrange
        var menu = CreateValidMenu();
        var category1 = menu.AddCategory("Category 1", "Description 1", 1).Value;
        var category2 = menu.AddCategory("Category 2", "Description 2", 2).Value;
        
        var item1 = menu.AddItemToCategory(category1.Id, "Item 1", "Description 1", DefaultBasePrice).Value;
        var item2 = menu.AddItemToCategory(category1.Id, "Item 2", "Description 2", DefaultBasePrice).Value;
        var item3 = menu.AddItemToCategory(category2.Id, "Item 3", "Description 3", DefaultBasePrice).Value;

        // Act
        var allItems = menu.GetAllItems();

        // Assert
        allItems.Should().HaveCount(3);
        allItems.Should().Contain(item1);
        allItems.Should().Contain(item2);
        allItems.Should().Contain(item3);
    }

    [Test]
    public void GetAllItems_WithNoItems_ShouldReturnEmptyList()
    {
        // Arrange
        var menu = CreateValidMenu();
        menu.AddCategory("Category 1", "Description 1", 1);

        // Act
        var allItems = menu.GetAllItems();

        // Assert
        allItems.Should().BeEmpty();
    }

    #endregion

    #region GetAvailableItems() Tests

    [Test]
    public void GetAvailableItems_WithMixedAvailability_ShouldReturnOnlyAvailableItems()
    {
        // Arrange
        var menu = CreateValidMenu();
        var category = menu.AddCategory("Category 1", "Description 1", 1).Value;
        
        var availableItem1 = menu.AddItemToCategory(category.Id, "Available 1", "Description 1", DefaultBasePrice, isAvailable: true).Value;
        var unavailableItem = menu.AddItemToCategory(category.Id, "Unavailable", "Description 2", DefaultBasePrice, isAvailable: false).Value;
        var availableItem2 = menu.AddItemToCategory(category.Id, "Available 2", "Description 3", DefaultBasePrice, isAvailable: true).Value;

        // Act
        var availableItems = menu.GetAvailableItems();

        // Assert
        availableItems.Should().HaveCount(2);
        availableItems.Should().Contain(availableItem1);
        availableItems.Should().Contain(availableItem2);
        availableItems.Should().NotContain(unavailableItem);
    }

    [Test]
    public void GetAvailableItems_WithNoAvailableItems_ShouldReturnEmptyList()
    {
        // Arrange
        var menu = CreateValidMenu();
        var category = menu.AddCategory("Category 1", "Description 1", 1).Value;
        menu.AddItemToCategory(category.Id, "Unavailable", "Description", DefaultBasePrice, isAvailable: false);

        // Act
        var availableItems = menu.GetAvailableItems();

        // Assert
        availableItems.Should().BeEmpty();
    }

    #endregion

    #region GetUnavailableItems() Tests

    [Test]
    public void GetUnavailableItems_WithMixedAvailability_ShouldReturnOnlyUnavailableItems()
    {
        // Arrange
        var menu = CreateValidMenu();
        var category = menu.AddCategory("Category 1", "Description 1", 1).Value;
        
        var availableItem = menu.AddItemToCategory(category.Id, "Available", "Description 1", DefaultBasePrice, isAvailable: true).Value;
        var unavailableItem1 = menu.AddItemToCategory(category.Id, "Unavailable 1", "Description 2", DefaultBasePrice, isAvailable: false).Value;
        var unavailableItem2 = menu.AddItemToCategory(category.Id, "Unavailable 2", "Description 3", DefaultBasePrice, isAvailable: false).Value;

        // Act
        var unavailableItems = menu.GetUnavailableItems();

        // Assert
        unavailableItems.Should().HaveCount(2);
        unavailableItems.Should().Contain(unavailableItem1);
        unavailableItems.Should().Contain(unavailableItem2);
        unavailableItems.Should().NotContain(availableItem);
    }

    [Test]
    public void GetUnavailableItems_WithNoUnavailableItems_ShouldReturnEmptyList()
    {
        // Arrange
        var menu = CreateValidMenu();
        var category = menu.AddCategory("Category 1", "Description 1", 1).Value;
        menu.AddItemToCategory(category.Id, "Available", "Description", DefaultBasePrice, isAvailable: true);

        // Act
        var unavailableItems = menu.GetUnavailableItems();

        // Assert
        unavailableItems.Should().BeEmpty();
    }

    #endregion

    #region GetItemsByCategory() Tests

    [Test]
    public void GetItemsByCategory_WithValidCategory_ShouldReturnItemsFromThatCategoryOnly()
    {
        // Arrange
        var menu = CreateValidMenu();
        var category1 = menu.AddCategory("Category 1", "Description 1", 1).Value;
        var category2 = menu.AddCategory("Category 2", "Description 2", 2).Value;
        
        var item1 = menu.AddItemToCategory(category1.Id, "Item 1", "Description 1", DefaultBasePrice).Value;
        var item2 = menu.AddItemToCategory(category1.Id, "Item 2", "Description 2", DefaultBasePrice).Value;
        var item3 = menu.AddItemToCategory(category2.Id, "Item 3", "Description 3", DefaultBasePrice).Value;

        // Act
        var category1Items = menu.GetItemsByCategory(category1.Id);

        // Assert
        category1Items.Should().HaveCount(2);
        category1Items.Should().Contain(item1);
        category1Items.Should().Contain(item2);
        category1Items.Should().NotContain(item3);
    }

    [Test]
    public void GetItemsByCategory_WithNonExistentCategory_ShouldReturnEmptyList()
    {
        // Arrange
        var menu = CreateValidMenu();
        var nonExistentCategoryId = MenuCategoryId.CreateUnique();

        // Act
        var items = menu.GetItemsByCategory(nonExistentCategoryId);

        // Assert
        items.Should().BeEmpty();
    }

    [Test]
    public void GetItemsByCategory_WithEmptyCategory_ShouldReturnEmptyList()
    {
        // Arrange
        var menu = CreateValidMenu();
        var category = menu.AddCategory("Empty Category", "Description", 1).Value;

        // Act
        var items = menu.GetItemsByCategory(category.Id);

        // Assert
        items.Should().BeEmpty();
    }

    #endregion

    #region SearchItemsByName() Tests

    [Test]
    public void SearchItemsByName_WithMatchingItems_ShouldReturnMatchingItemsCaseInsensitive()
    {
        // Arrange
        var menu = CreateValidMenu();
        var category = menu.AddCategory("Category 1", "Description 1", 1).Value;
        
        var pizzaItem = menu.AddItemToCategory(category.Id, "Margherita Pizza", "Classic pizza", DefaultBasePrice).Value;
        var burgerItem = menu.AddItemToCategory(category.Id, "Beef Burger", "Juicy burger", DefaultBasePrice).Value;
        var pizzaItem2 = menu.AddItemToCategory(category.Id, "Pepperoni Pizza", "Spicy pizza", DefaultBasePrice).Value;

        // Act
        var pizzaItems = menu.SearchItemsByName("pizza");

        // Assert
        pizzaItems.Should().HaveCount(2);
        pizzaItems.Should().Contain(pizzaItem);
        pizzaItems.Should().Contain(pizzaItem2);
        pizzaItems.Should().NotContain(burgerItem);
    }

    [Test]
    public void SearchItemsByName_WithPartialMatch_ShouldReturnMatchingItems()
    {
        // Arrange
        var menu = CreateValidMenu();
        var category = menu.AddCategory("Category 1", "Description 1", 1).Value;
        
        var cheeseburger = menu.AddItemToCategory(category.Id, "Cheeseburger", "Burger with cheese", DefaultBasePrice).Value;
        var burger = menu.AddItemToCategory(category.Id, "Classic Burger", "Simple burger", DefaultBasePrice).Value;
        var pizza = menu.AddItemToCategory(category.Id, "Pizza", "Italian pizza", DefaultBasePrice).Value;

        // Act
        var burgerItems = menu.SearchItemsByName("burger");

        // Assert
        burgerItems.Should().HaveCount(2);
        burgerItems.Should().Contain(cheeseburger);
        burgerItems.Should().Contain(burger);
        burgerItems.Should().NotContain(pizza);
    }

    [Test]
    public void SearchItemsByName_WithNoMatches_ShouldReturnEmptyList()
    {
        // Arrange
        var menu = CreateValidMenu();
        var category = menu.AddCategory("Category 1", "Description 1", 1).Value;
        menu.AddItemToCategory(category.Id, "Pizza", "Italian pizza", DefaultBasePrice);

        // Act
        var items = menu.SearchItemsByName("burger");

        // Assert
        items.Should().BeEmpty();
    }

    [Test]
    public void SearchItemsByName_WithNullOrEmptySearchTerm_ShouldReturnEmptyList()
    {
        // Arrange
        var menu = CreateValidMenu();
        var category = menu.AddCategory("Category 1", "Description 1", 1).Value;
        menu.AddItemToCategory(category.Id, "Pizza", "Italian pizza", DefaultBasePrice);

        // Act & Assert - Null search term
        var nullResult = menu.SearchItemsByName(null!);
        nullResult.Should().BeEmpty();

        // Act & Assert - Empty search term
        var emptyResult = menu.SearchItemsByName("");
        emptyResult.Should().BeEmpty();

        // Act & Assert - Whitespace search term
        var whitespaceResult = menu.SearchItemsByName("   ");
        whitespaceResult.Should().BeEmpty();
    }

    #endregion

    #region Count Properties Tests

    [Test]
    public void CountProperties_WithNoCategories_ShouldReturnZero()
    {
        // Arrange
        var menu = CreateValidMenu();

        // Act & Assert
        menu.HasCategories.Should().BeFalse();
        menu.CategoryCount.Should().Be(0);
        menu.TotalItemCount.Should().Be(0);
        menu.AvailableItemCount.Should().Be(0);
        menu.UnavailableItemCount.Should().Be(0);
        menu.IsMenuEmpty.Should().BeTrue();
    }

    [Test]
    public void CountProperties_WithCategoriesButNoItems_ShouldReturnCorrectCounts()
    {
        // Arrange
        var menu = CreateValidMenu();
        menu.AddCategory("Category 1", "Description 1", 1);
        menu.AddCategory("Category 2", "Description 2", 2);

        // Act & Assert
        menu.HasCategories.Should().BeTrue();
        menu.CategoryCount.Should().Be(2);
        menu.TotalItemCount.Should().Be(0);
        menu.AvailableItemCount.Should().Be(0);
        menu.UnavailableItemCount.Should().Be(0);
        menu.IsMenuEmpty.Should().BeTrue(); // No items
    }

    [Test]
    public void CountProperties_WithMixedItemAvailability_ShouldReturnCorrectCounts()
    {
        // Arrange
        var menu = CreateValidMenu();
        var category1 = menu.AddCategory("Category 1", "Description 1", 1).Value;
        var category2 = menu.AddCategory("Category 2", "Description 2", 2).Value;
        
        menu.AddItemToCategory(category1.Id, "Available 1", "Description 1", DefaultBasePrice, isAvailable: true);
        menu.AddItemToCategory(category1.Id, "Available 2", "Description 2", DefaultBasePrice, isAvailable: true);
        menu.AddItemToCategory(category1.Id, "Unavailable 1", "Description 3", DefaultBasePrice, isAvailable: false);
        menu.AddItemToCategory(category2.Id, "Available 3", "Description 4", DefaultBasePrice, isAvailable: true);
        menu.AddItemToCategory(category2.Id, "Unavailable 2", "Description 5", DefaultBasePrice, isAvailable: false);

        // Act & Assert
        menu.HasCategories.Should().BeTrue();
        menu.CategoryCount.Should().Be(2);
        menu.TotalItemCount.Should().Be(5);
        menu.AvailableItemCount.Should().Be(3);
        menu.UnavailableItemCount.Should().Be(2);
        menu.IsMenuEmpty.Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private static Menu CreateValidMenu()
    {
        return Menu.Create(DefaultRestaurantId, DefaultMenuName, DefaultMenuDescription).Value;
    }

    #endregion
}
