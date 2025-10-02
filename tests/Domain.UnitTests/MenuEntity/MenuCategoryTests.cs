using YummyZoom.Domain.MenuEntity.Events;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using MenuCategoryEntity = YummyZoom.Domain.MenuEntity.MenuCategory;

namespace YummyZoom.Domain.UnitTests.MenuEntity;

/// <summary>
/// Tests for MenuCategory entity functionality including creation and updates.
/// </summary>
[TestFixture]
public class MenuCategoryTests
{
    private static readonly MenuId DefaultMenuId = MenuId.CreateUnique();
    private const string DefaultCategoryName = "Test Category";
    private const int DefaultDisplayOrder = 1;

    #region Create() Method Tests

    [Test]
    public void Create_WithValidInputs_ShouldSucceedAndInitializeCategoryCorrectly()
    {
        // Arrange & Act
        var result = MenuCategoryEntity.Create(
            DefaultMenuId,
            DefaultCategoryName,
            DefaultDisplayOrder);

        // Assert
        result.ShouldBeSuccessful();
        var category = result.Value;

        category.Id.Value.Should().NotBe(Guid.Empty);
        category.MenuId.Should().Be(DefaultMenuId);
        category.Name.Should().Be(DefaultCategoryName);
        category.DisplayOrder.Should().Be(DefaultDisplayOrder);

        // Assert domain event
        category.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(MenuCategoryAdded));
        var categoryAddedEvent = category.DomainEvents.OfType<MenuCategoryAdded>().Single();
        categoryAddedEvent.MenuId.Should().Be(DefaultMenuId);
        categoryAddedEvent.CategoryId.Should().Be(category.Id);
    }

    [Test]
    public void Create_WithDifferentDisplayOrders_ShouldCreateCategoriesWithCorrectOrders()
    {
        // Arrange & Act
        var result1 = MenuCategoryEntity.Create(DefaultMenuId, "Category 1", 1);
        var result2 = MenuCategoryEntity.Create(DefaultMenuId, "Category 2", 5);
        var result3 = MenuCategoryEntity.Create(DefaultMenuId, "Category 3", 10);

        // Assert
        result1.ShouldBeSuccessful();
        result2.ShouldBeSuccessful();
        result3.ShouldBeSuccessful();

        result1.Value.DisplayOrder.Should().Be(1);
        result2.Value.DisplayOrder.Should().Be(5);
        result3.Value.DisplayOrder.Should().Be(10);
    }

    [Test]
    public void Create_WithInvalidName_ShouldFail()
    {
        // Act & Assert - Null name
        var nullResult = MenuCategoryEntity.Create(DefaultMenuId, null!, DefaultDisplayOrder);
        nullResult.ShouldBeFailure("Menu.InvalidCategoryName");

        // Act & Assert - Empty name
        var emptyResult = MenuCategoryEntity.Create(DefaultMenuId, "", DefaultDisplayOrder);
        emptyResult.ShouldBeFailure("Menu.InvalidCategoryName");

        // Act & Assert - Whitespace name
        var whitespaceResult = MenuCategoryEntity.Create(DefaultMenuId, "   ", DefaultDisplayOrder);
        whitespaceResult.ShouldBeFailure("Menu.InvalidCategoryName");
    }

    [Test]
    public void Create_WithInvalidDisplayOrder_ShouldFail()
    {
        // Act & Assert - Zero display order
        var zeroResult = MenuCategoryEntity.Create(DefaultMenuId, DefaultCategoryName, 0);
        zeroResult.ShouldBeFailure("Menu.InvalidDisplayOrder");

        // Act & Assert - Negative display order
        var negativeResult = MenuCategoryEntity.Create(DefaultMenuId, DefaultCategoryName, -1);
        negativeResult.ShouldBeFailure("Menu.InvalidDisplayOrder");

        // Act & Assert - Very negative display order
        var veryNegativeResult = MenuCategoryEntity.Create(DefaultMenuId, DefaultCategoryName, -100);
        veryNegativeResult.ShouldBeFailure("Menu.InvalidDisplayOrder");
    }

    #endregion

    #region UpdateName() Method Tests

    [Test]
    public void UpdateName_WithValidName_ShouldSucceedAndUpdateName()
    {
        // Arrange
        var category = CreateValidCategory();
        var newName = "Updated Category Name";

        // Act
        var result = category.UpdateName(newName);

        // Assert
        result.ShouldBeSuccessful();
        category.Name.Should().Be(newName);

        // Assert domain event (should have 2 events: MenuCategoryAdded from creation + MenuCategoryNameUpdated)
        category.DomainEvents.Should().HaveCount(2);
        category.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(MenuCategoryNameUpdated));
        var nameUpdatedEvent = category.DomainEvents.OfType<MenuCategoryNameUpdated>().Single();
        nameUpdatedEvent.MenuId.Should().Be(category.MenuId);
        nameUpdatedEvent.CategoryId.Should().Be(category.Id);
        nameUpdatedEvent.NewName.Should().Be(newName);
    }

    [Test]
    public void UpdateName_WithInvalidName_ShouldFail()
    {
        // Arrange
        var category = CreateValidCategory();
        var originalName = category.Name;
        var originalEventCount = category.DomainEvents.Count;

        // Act & Assert - Null name
        var nullResult = category.UpdateName(null!);
        nullResult.ShouldBeFailure("Menu.InvalidCategoryName");
        category.Name.Should().Be(originalName);
        category.DomainEvents.Should().HaveCount(originalEventCount); // No new event raised

        // Act & Assert - Empty name
        var emptyResult = category.UpdateName("");
        emptyResult.ShouldBeFailure("Menu.InvalidCategoryName");
        category.Name.Should().Be(originalName);
        category.DomainEvents.Should().HaveCount(originalEventCount); // No new event raised

        // Act & Assert - Whitespace name
        var whitespaceResult = category.UpdateName("   ");
        whitespaceResult.ShouldBeFailure("Menu.InvalidCategoryName");
        category.Name.Should().Be(originalName);
        category.DomainEvents.Should().HaveCount(originalEventCount); // No new event raised
    }

    [Test]
    public void UpdateName_WithSameName_ShouldSucceedAndKeepName()
    {
        // Arrange
        var category = CreateValidCategory();
        var originalName = category.Name;
        var originalEventCount = category.DomainEvents.Count;

        // Act
        var result = category.UpdateName(originalName);

        // Assert
        result.ShouldBeSuccessful();
        category.Name.Should().Be(originalName);

        // Assert that a new event is still raised even with the same name
        category.DomainEvents.Should().HaveCount(originalEventCount + 1);
        category.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(MenuCategoryNameUpdated));
    }

    [Test]
    public void UpdateName_WithDifferentCasing_ShouldSucceedAndUpdateName()
    {
        // Arrange
        var category = CreateValidCategory();
        var newName = DefaultCategoryName.ToUpper();

        // Act
        var result = category.UpdateName(newName);

        // Assert
        result.ShouldBeSuccessful();
        category.Name.Should().Be(newName);
        category.Name.Should().NotBe(DefaultCategoryName);

        // Assert domain event (should have 2 events: MenuCategoryAdded from creation + MenuCategoryNameUpdated)
        category.DomainEvents.Should().HaveCount(2);
        category.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(MenuCategoryNameUpdated));
        var nameUpdatedEvent = category.DomainEvents.OfType<MenuCategoryNameUpdated>().Single();
        nameUpdatedEvent.MenuId.Should().Be(category.MenuId);
        nameUpdatedEvent.CategoryId.Should().Be(category.Id);
        nameUpdatedEvent.NewName.Should().Be(newName);
    }

    #endregion

    #region UpdateDisplayOrder() Method Tests

    [Test]
    public void UpdateDisplayOrder_WithValidOrder_ShouldSucceedAndUpdateOrder()
    {
        // Arrange
        var category = CreateValidCategory();
        var newDisplayOrder = 5;

        // Act
        var result = category.UpdateDisplayOrder(newDisplayOrder);

        // Assert
        result.ShouldBeSuccessful();
        category.DisplayOrder.Should().Be(newDisplayOrder);

        // Assert domain event (should have 2 events: MenuCategoryAdded from creation + MenuCategoryDisplayOrderUpdated)
        category.DomainEvents.Should().HaveCount(2);
        category.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(MenuCategoryDisplayOrderUpdated));
        var displayOrderUpdatedEvent = category.DomainEvents.OfType<MenuCategoryDisplayOrderUpdated>().Single();
        displayOrderUpdatedEvent.MenuId.Should().Be(category.MenuId);
        displayOrderUpdatedEvent.CategoryId.Should().Be(category.Id);
        displayOrderUpdatedEvent.NewDisplayOrder.Should().Be(newDisplayOrder);
    }

    [Test]
    public void UpdateDisplayOrder_WithInvalidOrder_ShouldFail()
    {
        // Arrange
        var category = CreateValidCategory();
        var originalOrder = category.DisplayOrder;
        var originalEventCount = category.DomainEvents.Count;

        // Act & Assert - Zero display order
        var zeroResult = category.UpdateDisplayOrder(0);
        zeroResult.ShouldBeFailure("Menu.InvalidDisplayOrder");
        category.DisplayOrder.Should().Be(originalOrder);
        category.DomainEvents.Should().HaveCount(originalEventCount); // No new event raised

        // Act & Assert - Negative display order
        var negativeResult = category.UpdateDisplayOrder(-1);
        negativeResult.ShouldBeFailure("Menu.InvalidDisplayOrder");
        category.DisplayOrder.Should().Be(originalOrder);
        category.DomainEvents.Should().HaveCount(originalEventCount); // No new event raised

        // Act & Assert - Very negative display order
        var veryNegativeResult = category.UpdateDisplayOrder(-100);
        veryNegativeResult.ShouldBeFailure("Menu.InvalidDisplayOrder");
        category.DisplayOrder.Should().Be(originalOrder);
        category.DomainEvents.Should().HaveCount(originalEventCount); // No new event raised
    }

    [Test]
    public void UpdateDisplayOrder_WithSameOrder_ShouldSucceedAndKeepOrder()
    {
        // Arrange
        var category = CreateValidCategory();
        var originalOrder = category.DisplayOrder;
        var originalEventCount = category.DomainEvents.Count;

        // Act
        var result = category.UpdateDisplayOrder(originalOrder);

        // Assert
        result.ShouldBeSuccessful();
        category.DisplayOrder.Should().Be(originalOrder);

        // Assert that a new event is still raised even with the same order
        category.DomainEvents.Should().HaveCount(originalEventCount + 1);
        category.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(MenuCategoryDisplayOrderUpdated));
    }

    [Test]
    public void UpdateDisplayOrder_WithLargeOrder_ShouldSucceedAndUpdateOrder()
    {
        // Arrange
        var category = CreateValidCategory();
        var largeOrder = 1000;

        // Act
        var result = category.UpdateDisplayOrder(largeOrder);

        // Assert
        result.ShouldBeSuccessful();
        category.DisplayOrder.Should().Be(largeOrder);

        // Assert domain event (should have 2 events: MenuCategoryAdded from creation + MenuCategoryDisplayOrderUpdated)
        category.DomainEvents.Should().HaveCount(2);
        category.DomainEvents.Should().ContainSingle(e => e.GetType() == typeof(MenuCategoryDisplayOrderUpdated));
        var displayOrderUpdatedEvent = category.DomainEvents.OfType<MenuCategoryDisplayOrderUpdated>().Single();
        displayOrderUpdatedEvent.MenuId.Should().Be(category.MenuId);
        displayOrderUpdatedEvent.CategoryId.Should().Be(category.Id);
        displayOrderUpdatedEvent.NewDisplayOrder.Should().Be(largeOrder);
    }

    #endregion

    #region Property Immutability Tests

    [Test]
    public void MenuId_ShouldBeImmutableAfterCreation()
    {
        // Arrange
        var category = CreateValidCategory();
        var originalMenuId = category.MenuId;

        // Act & Assert - MenuId should not be changeable
        category.MenuId.Should().Be(originalMenuId);

        // Verify through updates that MenuId remains the same
        category.UpdateName("New Name");
        category.UpdateDisplayOrder(10);
        category.MenuId.Should().Be(originalMenuId);
    }

    [Test]
    public void Id_ShouldBeImmutableAfterCreation()
    {
        // Arrange
        var category = CreateValidCategory();
        var originalId = category.Id;

        // Act & Assert - Id should not be changeable
        category.Id.Should().Be(originalId);

        // Verify through updates that Id remains the same
        category.UpdateName("New Name");
        category.UpdateDisplayOrder(10);
        category.Id.Should().Be(originalId);
    }

    #endregion

    #region Helper Methods

    private static MenuCategoryEntity CreateValidCategory()
    {
        return MenuCategoryEntity.Create(DefaultMenuId, DefaultCategoryName, DefaultDisplayOrder).Value;
    }

    private static MenuCategoryEntity CreateValidCategory(string name, int displayOrder)
    {
        return MenuCategoryEntity.Create(DefaultMenuId, name, displayOrder).Value;
    }

    #endregion
}
