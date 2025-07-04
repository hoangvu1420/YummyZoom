using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.MenuAggregate;
using YummyZoom.Domain.MenuAggregate.Events;
using YummyZoom.Domain.MenuAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.MenuAggregate;

/// <summary>
/// Tests for Menu aggregate category management functionality.
/// </summary>
[TestFixture]
public class MenuCategoryTests
{
    private static readonly RestaurantId DefaultRestaurantId = RestaurantId.CreateUnique();
    private const string DefaultMenuName = "Test Menu";
    private const string DefaultMenuDescription = "Test Description";
    private const string DefaultCategoryName = "Test Category";
    private const string DefaultCategoryDescription = "Test Category Description";
    private const int DefaultDisplayOrder = 1;

    #region AddCategory() Method Tests

    [Test]
    public void AddCategory_WithValidInputs_ShouldSucceedAndAddCategory()
    {
        // Arrange
        var menu = CreateValidMenu();
        menu.ClearDomainEvents();

        // Act
        var result = menu.AddCategory(DefaultCategoryName, DefaultCategoryDescription, DefaultDisplayOrder);

        // Assert
        result.ShouldBeSuccessful();
        var category = result.Value;
        
        category.Name.Should().Be(DefaultCategoryName);
        category.DisplayOrder.Should().Be(DefaultDisplayOrder);
        
        menu.Categories.Should().ContainSingle()
            .Which.Should().Be(category);
        menu.HasCategories.Should().BeTrue();
        menu.CategoryCount.Should().Be(1);
        
        menu.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<MenuCategoryAdded>()
            .Which.Should().Match<MenuCategoryAdded>(e => 
                e.MenuId == menu.Id && e.CategoryId == category.Id);
    }

    [Test]
    public void AddCategory_WithDuplicateName_ShouldFail()
    {
        // Arrange
        var menu = CreateValidMenu();
        menu.AddCategory(DefaultCategoryName, DefaultCategoryDescription, DefaultDisplayOrder);

        // Act - Try to add category with same name (case-insensitive)
        var result = menu.AddCategory(DefaultCategoryName.ToUpper(), DefaultCategoryDescription, 2);

        // Assert
        result.ShouldBeFailure("Menu.DuplicateCategoryName");
        menu.CategoryCount.Should().Be(1); // Should still have only the first category
    }

    [Test]
    public void AddCategory_WithInvalidName_ShouldFail()
    {
        // Arrange
        var menu = CreateValidMenu();

        // Act & Assert - Null name
        var nullResult = menu.AddCategory(null!, DefaultCategoryDescription, DefaultDisplayOrder);
        nullResult.ShouldBeFailure("Menu.InvalidCategoryName");

        // Act & Assert - Empty name
        var emptyResult = menu.AddCategory("", DefaultCategoryDescription, DefaultDisplayOrder);
        emptyResult.ShouldBeFailure("Menu.InvalidCategoryName");

        // Act & Assert - Whitespace name
        var whitespaceResult = menu.AddCategory("   ", DefaultCategoryDescription, DefaultDisplayOrder);
        whitespaceResult.ShouldBeFailure("Menu.InvalidCategoryName");
    }

    [Test]
    public void AddCategory_WithInvalidDisplayOrder_ShouldFail()
    {
        // Arrange
        var menu = CreateValidMenu();

        // Act & Assert - Zero display order
        var zeroResult = menu.AddCategory(DefaultCategoryName, DefaultCategoryDescription, 0);
        zeroResult.ShouldBeFailure("Menu.InvalidDisplayOrder");

        // Act & Assert - Negative display order
        var negativeResult = menu.AddCategory(DefaultCategoryName, DefaultCategoryDescription, -1);
        negativeResult.ShouldBeFailure("Menu.InvalidDisplayOrder");
    }

    #endregion

    #region RemoveCategory() Method Tests

    [Test]
    public void RemoveCategory_WithValidCategory_ShouldSucceedAndRemoveCategory()
    {
        // Arrange
        var menu = CreateValidMenu();
        var categoryResult = menu.AddCategory(DefaultCategoryName, DefaultCategoryDescription, DefaultDisplayOrder);
        var categoryId = categoryResult.Value.Id;
        menu.ClearDomainEvents();

        // Act
        var result = menu.RemoveCategory(categoryId);

        // Assert
        result.ShouldBeSuccessful();
        menu.Categories.Should().BeEmpty();
        menu.HasCategories.Should().BeFalse();
        menu.CategoryCount.Should().Be(0);
        
        menu.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<MenuCategoryRemoved>()
            .Which.Should().Match<MenuCategoryRemoved>(e => 
                e.MenuId == menu.Id && e.CategoryId == categoryId);
    }

    [Test]
    public void RemoveCategory_WithNonExistentCategory_ShouldFail()
    {
        // Arrange
        var menu = CreateValidMenu();
        var nonExistentCategoryId = MenuCategoryId.CreateUnique();

        // Act
        var result = menu.RemoveCategory(nonExistentCategoryId);

        // Assert
        result.ShouldBeFailure("Menu.CategoryNotFound");
    }

    [Test]
    public void RemoveCategory_WithCategoryThatHasItems_ShouldFail()
    {
        // Arrange
        var menu = CreateMenuWithCategoryAndItem();
        var categoryId = menu.Categories.First().Id;

        // Act
        var result = menu.RemoveCategory(categoryId);

        // Assert
        result.ShouldBeFailure("Menu.CannotRemoveCategoryWithItems");
        menu.CategoryCount.Should().Be(1); // Category should still exist
    }

    #endregion

    #region GetCategory() Method Tests

    [Test]
    public void GetCategory_WithValidCategoryId_ShouldSucceedAndReturnCategory()
    {
        // Arrange
        var menu = CreateValidMenu();
        var categoryResult = menu.AddCategory(DefaultCategoryName, DefaultCategoryDescription, DefaultDisplayOrder);
        var categoryId = categoryResult.Value.Id;

        // Act
        var result = menu.GetCategory(categoryId);

        // Assert
        result.ShouldBeSuccessful();
        var category = result.Value;
        
        category.Id.Should().Be(categoryId);
        category.Name.Should().Be(DefaultCategoryName);
        category.DisplayOrder.Should().Be(DefaultDisplayOrder);
    }

    [Test]
    public void GetCategory_WithNonExistentCategoryId_ShouldFail()
    {
        // Arrange
        var menu = CreateValidMenu();
        var nonExistentCategoryId = MenuCategoryId.CreateUnique();

        // Act
        var result = menu.GetCategory(nonExistentCategoryId);

        // Assert
        result.ShouldBeFailure("Menu.CategoryNotFound");
    }

    #endregion

    #region UpdateCategoryDetails() Method Tests

    [Test]
    public void UpdateCategoryDetails_WithValidInputs_ShouldSucceedAndUpdateCategory()
    {
        // Arrange
        var menu = CreateValidMenu();
        var categoryResult = menu.AddCategory(DefaultCategoryName, DefaultCategoryDescription, DefaultDisplayOrder);
        var categoryId = categoryResult.Value.Id;
        var newName = "Updated Category Name";
        var newDisplayOrder = 5;
        menu.ClearDomainEvents();

        // Act
        var result = menu.UpdateCategoryDetails(categoryId, newName, newDisplayOrder);

        // Assert
        result.ShouldBeSuccessful();
        
        var category = menu.GetCategory(categoryId).Value;
        category.Name.Should().Be(newName);
        category.DisplayOrder.Should().Be(newDisplayOrder);
        
        menu.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<MenuCategoryUpdated>()
            .Which.Should().Match<MenuCategoryUpdated>(e => 
                e.MenuId == menu.Id && e.CategoryId == categoryId);
    }

    [Test]
    public void UpdateCategoryDetails_WithNonExistentCategory_ShouldFail()
    {
        // Arrange
        var menu = CreateValidMenu();
        var nonExistentCategoryId = MenuCategoryId.CreateUnique();

        // Act
        var result = menu.UpdateCategoryDetails(nonExistentCategoryId, "New Name", 1);

        // Assert
        result.ShouldBeFailure("Menu.CategoryNotFound");
    }

    [Test]
    public void UpdateCategoryDetails_WithDuplicateName_ShouldFail()
    {
        // Arrange
        var menu = CreateValidMenu();
        var category1 = menu.AddCategory("Category 1", DefaultCategoryDescription, 1).Value;
        var category2 = menu.AddCategory("Category 2", DefaultCategoryDescription, 2).Value;

        // Act - Try to update category2 with category1's name
        var result = menu.UpdateCategoryDetails(category2.Id, "Category 1", 3);

        // Assert
        result.ShouldBeFailure("Menu.DuplicateCategoryName");
    }

    [Test]
    public void UpdateCategoryDetails_WithInvalidName_ShouldFail()
    {
        // Arrange
        var menu = CreateValidMenu();
        var categoryResult = menu.AddCategory(DefaultCategoryName, DefaultCategoryDescription, DefaultDisplayOrder);
        var categoryId = categoryResult.Value.Id;

        // Act & Assert - Null name
        var nullResult = menu.UpdateCategoryDetails(categoryId, null!, 1);
        nullResult.ShouldBeFailure("Menu.InvalidCategoryName");

        // Act & Assert - Empty name
        var emptyResult = menu.UpdateCategoryDetails(categoryId, "", 1);
        emptyResult.ShouldBeFailure("Menu.InvalidCategoryName");

        // Act & Assert - Whitespace name
        var whitespaceResult = menu.UpdateCategoryDetails(categoryId, "   ", 1);
        whitespaceResult.ShouldBeFailure("Menu.InvalidCategoryName");
    }

    [Test]
    public void UpdateCategoryDetails_WithInvalidDisplayOrder_ShouldFail()
    {
        // Arrange
        var menu = CreateValidMenu();
        var categoryResult = menu.AddCategory(DefaultCategoryName, DefaultCategoryDescription, DefaultDisplayOrder);
        var categoryId = categoryResult.Value.Id;

        // Act & Assert - Zero display order
        var zeroResult = menu.UpdateCategoryDetails(categoryId, "New Name", 0);
        zeroResult.ShouldBeFailure("Menu.InvalidDisplayOrder");

        // Act & Assert - Negative display order
        var negativeResult = menu.UpdateCategoryDetails(categoryId, "New Name", -1);
        negativeResult.ShouldBeFailure("Menu.InvalidDisplayOrder");
    }

    #endregion

    #region Helper Methods

    private static Menu CreateValidMenu()
    {
        return Menu.Create(DefaultRestaurantId, DefaultMenuName, DefaultMenuDescription).Value;
    }

    private static Menu CreateMenuWithCategoryAndItem()
    {
        var menu = CreateValidMenu();
        var categoryResult = menu.AddCategory(DefaultCategoryName, DefaultCategoryDescription, DefaultDisplayOrder);
        var categoryId = categoryResult.Value.Id;
        
        // Add an item to the category
        menu.AddItemToCategory(categoryId, "Test Item", "Test Item Description", 
            new YummyZoom.Domain.Common.ValueObjects.Money(10.99m, YummyZoom.Domain.Common.Constants.Currencies.Default));
        
        return menu;
    }

    #endregion
}
