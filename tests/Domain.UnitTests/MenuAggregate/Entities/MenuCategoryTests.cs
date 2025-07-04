using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.MenuAggregate.Entities;

namespace YummyZoom.Domain.UnitTests.MenuAggregate.Entities;

/// <summary>
/// Tests for MenuCategory entity functionality.
/// </summary>
[TestFixture]
public class MenuCategoryTests
{
    private const string DefaultName = "Test Category";
    private const int DefaultDisplayOrder = 1;
    private static readonly Money DefaultBasePrice = new Money(12.99m, Currencies.Default);

    #region Create() Method Tests

    [Test]
    public void Create_WithValidInputs_ShouldSucceedAndInitializeCorrectly()
    {
        // Arrange & Act
        var result = MenuCategory.Create(DefaultName, DefaultDisplayOrder);

        // Assert
        result.ShouldBeSuccessful();
        var category = result.Value;
        
        category.Id.Value.Should().NotBe(Guid.Empty);
        category.Name.Should().Be(DefaultName);
        category.DisplayOrder.Should().Be(DefaultDisplayOrder);
        category.Items.Should().BeEmpty();
        category.HasItems.Should().BeFalse();
        category.ItemCount.Should().Be(0);
    }

    [Test]
    public void Create_WithValidInputsAndItems_ShouldSucceedAndInitializeWithItems()
    {
        // Arrange
        var items = new List<MenuItem>
        {
            CreateValidMenuItem("Item 1"),
            CreateValidMenuItem("Item 2")
        };

        // Act
        var result = MenuCategory.Create(DefaultName, DefaultDisplayOrder, items);

        // Assert
        result.ShouldBeSuccessful();
        var category = result.Value;
        
        category.Items.Should().HaveCount(2);
        category.HasItems.Should().BeTrue();
        category.ItemCount.Should().Be(2);
    }

    [Test]
    public void Create_WithNullOrEmptyName_ShouldFail()
    {
        // Act & Assert - Null name
        var nullResult = MenuCategory.Create(null!, DefaultDisplayOrder);
        nullResult.ShouldBeFailure("Menu.InvalidCategoryName");

        // Act & Assert - Empty name
        var emptyResult = MenuCategory.Create("", DefaultDisplayOrder);
        emptyResult.ShouldBeFailure("Menu.InvalidCategoryName");

        // Act & Assert - Whitespace name
        var whitespaceResult = MenuCategory.Create("   ", DefaultDisplayOrder);
        whitespaceResult.ShouldBeFailure("Menu.InvalidCategoryName");
    }

    [Test]
    public void Create_WithInvalidDisplayOrder_ShouldFail()
    {
        // Act & Assert - Zero display order
        var zeroResult = MenuCategory.Create(DefaultName, 0);
        zeroResult.ShouldBeFailure("Menu.InvalidDisplayOrder");

        // Act & Assert - Negative display order
        var negativeResult = MenuCategory.Create(DefaultName, -1);
        negativeResult.ShouldBeFailure("Menu.InvalidDisplayOrder");
    }

    [Test]
    public void Create_WithDuplicateItemNames_ShouldFail()
    {
        // Arrange
        var items = new List<MenuItem>
        {
            CreateValidMenuItem("Duplicate Item"),
            CreateValidMenuItem("DUPLICATE ITEM") // Case-insensitive duplicate
        };

        // Act
        var result = MenuCategory.Create(DefaultName, DefaultDisplayOrder, items);

        // Assert
        result.ShouldBeFailure("Menu.DuplicateItemName");
    }

    #endregion

    #region AddMenuItem() Method Tests

    [Test]
    public void AddMenuItem_WithValidItem_ShouldSucceedAndAddItem()
    {
        // Arrange
        var category = CreateValidCategory();
        var menuItem = CreateValidMenuItem("Test Item");

        // Act
        var result = category.AddMenuItem(menuItem);

        // Assert
        result.ShouldBeSuccessful();
        category.Items.Should().ContainSingle()
            .Which.Should().Be(menuItem);
        category.HasItems.Should().BeTrue();
        category.ItemCount.Should().Be(1);
    }

    [Test]
    public void AddMenuItem_WithDuplicateName_ShouldFail()
    {
        // Arrange
        var category = CreateValidCategory();
        var existingItem = CreateValidMenuItem("Existing Item");
        category.AddMenuItem(existingItem);

        var duplicateItem = CreateValidMenuItem("EXISTING ITEM"); // Case-insensitive duplicate

        // Act
        var result = category.AddMenuItem(duplicateItem);

        // Assert
        result.ShouldBeFailure("Menu.DuplicateItemName");
        category.Items.Should().ContainSingle(); // Only original item
    }

    [Test]
    public void AddMenuItem_MultipleItems_ShouldSucceedAndMaintainOrder()
    {
        // Arrange
        var category = CreateValidCategory();
        var item1 = CreateValidMenuItem("Item 1");
        var item2 = CreateValidMenuItem("Item 2");
        var item3 = CreateValidMenuItem("Item 3");

        // Act
        category.AddMenuItem(item1);
        category.AddMenuItem(item2);
        category.AddMenuItem(item3);

        // Assert
        category.Items.Should().HaveCount(3);
        category.Items[0].Should().Be(item1);
        category.Items[1].Should().Be(item2);
        category.Items[2].Should().Be(item3);
    }

    #endregion

    #region RemoveMenuItem() Method Tests

    [Test]
    public void RemoveMenuItem_WithExistingItem_ShouldSucceedAndRemoveItem()
    {
        // Arrange
        var category = CreateValidCategory();
        var menuItem = CreateValidMenuItem("Test Item");
        category.AddMenuItem(menuItem);

        // Act
        var result = category.RemoveMenuItem(menuItem.Id);

        // Assert
        result.ShouldBeSuccessful();
        category.Items.Should().BeEmpty();
        category.HasItems.Should().BeFalse();
        category.ItemCount.Should().Be(0);
    }

    [Test]
    public void RemoveMenuItem_WithNonExistentItem_ShouldFail()
    {
        // Arrange
        var category = CreateValidCategory();
        var nonExistentItemId = CreateValidMenuItem("Non-existent").Id;

        // Act
        var result = category.RemoveMenuItem(nonExistentItemId);

        // Assert
        result.ShouldBeFailure("Menu.ItemNotFound");
    }

    [Test]
    public void RemoveMenuItem_FromMultipleItems_ShouldRemoveCorrectItem()
    {
        // Arrange
        var category = CreateValidCategory();
        var item1 = CreateValidMenuItem("Item 1");
        var item2 = CreateValidMenuItem("Item 2");
        var item3 = CreateValidMenuItem("Item 3");
        
        category.AddMenuItem(item1);
        category.AddMenuItem(item2);
        category.AddMenuItem(item3);

        // Act
        var result = category.RemoveMenuItem(item2.Id);

        // Assert
        result.ShouldBeSuccessful();
        category.Items.Should().HaveCount(2);
        category.Items.Should().Contain(item1);
        category.Items.Should().NotContain(item2);
        category.Items.Should().Contain(item3);
    }

    #endregion

    #region GetMenuItem() Method Tests

    [Test]
    public void GetMenuItem_WithExistingItem_ShouldSucceedAndReturnItem()
    {
        // Arrange
        var category = CreateValidCategory();
        var menuItem = CreateValidMenuItem("Test Item");
        category.AddMenuItem(menuItem);

        // Act
        var result = category.GetMenuItem(menuItem.Id);

        // Assert
        result.ShouldBeSuccessful();
        result.Value.Should().Be(menuItem);
    }

    [Test]
    public void GetMenuItem_WithNonExistentItem_ShouldFail()
    {
        // Arrange
        var category = CreateValidCategory();
        var nonExistentItemId = CreateValidMenuItem("Non-existent").Id;

        // Act
        var result = category.GetMenuItem(nonExistentItemId);

        // Assert
        result.ShouldBeFailure("Menu.ItemNotFound");
    }

    #endregion

    #region UpdateCategoryDetails() Method Tests

    [Test]
    public void UpdateCategoryDetails_WithValidInputs_ShouldSucceedAndUpdateProperties()
    {
        // Arrange
        var category = CreateValidCategory();
        var newName = "Updated Category Name";
        var newDisplayOrder = 5;

        // Act
        var result = category.UpdateCategoryDetails(newName, newDisplayOrder);

        // Assert
        result.ShouldBeSuccessful();
        category.Name.Should().Be(newName);
        category.DisplayOrder.Should().Be(newDisplayOrder);
    }

    [Test]
    public void UpdateCategoryDetails_WithNullOrEmptyName_ShouldFail()
    {
        // Arrange
        var category = CreateValidCategory();

        // Act & Assert - Null name
        var nullResult = category.UpdateCategoryDetails(null!, DefaultDisplayOrder);
        nullResult.ShouldBeFailure("Menu.InvalidCategoryName");

        // Act & Assert - Empty name
        var emptyResult = category.UpdateCategoryDetails("", DefaultDisplayOrder);
        emptyResult.ShouldBeFailure("Menu.InvalidCategoryName");

        // Act & Assert - Whitespace name
        var whitespaceResult = category.UpdateCategoryDetails("   ", DefaultDisplayOrder);
        whitespaceResult.ShouldBeFailure("Menu.InvalidCategoryName");
    }

    [Test]
    public void UpdateCategoryDetails_WithInvalidDisplayOrder_ShouldFail()
    {
        // Arrange
        var category = CreateValidCategory();

        // Act & Assert - Zero display order
        var zeroResult = category.UpdateCategoryDetails(DefaultName, 0);
        zeroResult.ShouldBeFailure("Menu.InvalidDisplayOrder");

        // Act & Assert - Negative display order
        var negativeResult = category.UpdateCategoryDetails(DefaultName, -1);
        negativeResult.ShouldBeFailure("Menu.InvalidDisplayOrder");
    }

    #endregion

    #region Helper Methods

    private static MenuCategory CreateValidCategory()
    {
        return MenuCategory.Create(DefaultName, DefaultDisplayOrder).Value;
    }

    private static MenuItem CreateValidMenuItem(string name)
    {
        return MenuItem.Create(name, "Test Description", DefaultBasePrice).Value;
    }

    #endregion
}
