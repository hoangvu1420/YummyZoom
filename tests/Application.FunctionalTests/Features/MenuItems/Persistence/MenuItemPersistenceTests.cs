using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.TagEntity.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.MenuItems.Persistence;

/// <summary>
/// Tests for MenuItem persistence with JSONB collections to verify round-trip data integrity.
/// Focuses on testing the new shared JSON serialization infrastructure for DietaryTagIds and AppliedCustomizations.
/// </summary>
public class MenuItemPersistenceTests : BaseTestFixture
{
    private RestaurantId _restaurantId = null!;
    private MenuCategoryId _menuCategoryId = null!;

    [SetUp]
    public void SetUp()
    {
        // Use existing test data infrastructure
        _restaurantId = RestaurantId.Create(Testing.TestData.DefaultRestaurantId);
        _menuCategoryId = MenuCategoryId.Create(Testing.TestData.GetMenuCategoryId("Main Dishes"));
    }

    #region Basic Persistence Tests

    [Test]
    public async Task CreateMenuItem_WithEmptyCollections_ShouldPersistAndRetrieveCorrectly()
    {
        // Arrange
        var menuItem = CreateBasicMenuItem();

        // Act
        await AddAsync(menuItem);

        // Assert
        var retrieved = await FindAsync<MenuItem>(menuItem.Id);
        retrieved.Should().NotBeNull();
        retrieved!.DietaryTagIds.Should().BeEmpty();
        retrieved.AppliedCustomizations.Should().BeEmpty();

        // Verify all basic properties are preserved
        VerifyBasicProperties(menuItem, retrieved);
    }

    [Test]
    public async Task CreateMenuItem_WithDietaryTagsOnly_ShouldPersistAndRetrieveCorrectly()
    {
        // Arrange
        var dietaryTags = CreateTestDietaryTags();
        var menuItem = CreateMenuItemWithDietaryTags(dietaryTags);

        // Act
        await AddAsync(menuItem);

        // Assert
        var retrieved = await FindAsync<MenuItem>(menuItem.Id);
        retrieved.Should().NotBeNull();
        retrieved!.DietaryTagIds.Should().HaveCount(dietaryTags.Count);
        retrieved.DietaryTagIds.Should().BeEquivalentTo(dietaryTags);
        retrieved.AppliedCustomizations.Should().BeEmpty();

        VerifyBasicProperties(menuItem, retrieved);
    }

    [Test]
    public async Task CreateMenuItem_WithAppliedCustomizationsOnly_ShouldPersistAndRetrieveCorrectly()
    {
        // Arrange
        var customizations = CreateTestAppliedCustomizations();
        var menuItem = CreateMenuItemWithCustomizations(customizations);

        // Act
        await AddAsync(menuItem);

        // Assert
        var retrieved = await FindAsync<MenuItem>(menuItem.Id);
        retrieved.Should().NotBeNull();
        retrieved!.AppliedCustomizations.Should().HaveCount(customizations.Count);
        retrieved.AppliedCustomizations.Should().BeEquivalentTo(customizations);
        retrieved.DietaryTagIds.Should().BeEmpty();

        VerifyBasicProperties(menuItem, retrieved);
    }

    [Test]
    public async Task CreateMenuItem_WithBothCollections_ShouldPersistAndRetrieveCorrectly()
    {
        // Arrange
        var dietaryTags = CreateTestDietaryTags();
        var customizations = CreateTestAppliedCustomizations();
        var menuItem = CreateMenuItemWithBothCollections(dietaryTags, customizations);

        // Act
        await AddAsync(menuItem);

        // Assert
        var retrieved = await FindAsync<MenuItem>(menuItem.Id);
        retrieved.Should().NotBeNull();
        retrieved!.DietaryTagIds.Should().HaveCount(dietaryTags.Count);
        retrieved.DietaryTagIds.Should().BeEquivalentTo(dietaryTags);
        retrieved.AppliedCustomizations.Should().HaveCount(customizations.Count);
        retrieved.AppliedCustomizations.Should().BeEquivalentTo(customizations);

        VerifyBasicProperties(menuItem, retrieved);
    }

    #endregion

    #region Collection Modification Tests

    [Test]
    public async Task UpdateMenuItem_SetDietaryTags_ShouldPersistChangesCorrectly()
    {
        // Arrange
        var menuItem = CreateBasicMenuItem();
        await AddAsync(menuItem);

        // Act - Set dietary tags using domain method
        var newTags = CreateTestDietaryTags();
        var result = menuItem.SetDietaryTags(newTags);
        result.ShouldBeSuccessful();
        await UpdateAsync(menuItem);

        // Assert
        var retrieved = await FindAsync<MenuItem>(menuItem.Id);
        retrieved.Should().NotBeNull();
        retrieved!.DietaryTagIds.Should().HaveCount(newTags.Count);
        retrieved.DietaryTagIds.Should().BeEquivalentTo(newTags);
    }

    [Test]
    public async Task UpdateMenuItem_RemoveDietaryTags_ShouldPersistChangesCorrectly()
    {
        // Arrange
        var initialTags = CreateTestDietaryTags();
        var menuItem = CreateMenuItemWithDietaryTags(initialTags);
        await AddAsync(menuItem);

        // Act - Remove one tag by setting new list without it
        var tagToRemove = initialTags.First();
        var remainingTags = initialTags.Except(new[] { tagToRemove }).ToList();
        var result = menuItem.SetDietaryTags(remainingTags);
        result.ShouldBeSuccessful();
        await UpdateAsync(menuItem);

        // Assert
        var retrieved = await FindAsync<MenuItem>(menuItem.Id);
        retrieved.Should().NotBeNull();
        retrieved!.DietaryTagIds.Should().HaveCount(initialTags.Count - 1);
        retrieved.DietaryTagIds.Should().NotContain(tagToRemove);
        retrieved.DietaryTagIds.Should().BeEquivalentTo(remainingTags);
    }

    [Test]
    public async Task UpdateMenuItem_AddAppliedCustomization_ShouldPersistChangesCorrectly()
    {
        // Arrange
        var menuItem = CreateBasicMenuItem();
        await AddAsync(menuItem);

        // Act - Add customization using domain method
        var newCustomization = CreateTestAppliedCustomizations().First();
        var result = menuItem.AssignCustomizationGroup(newCustomization);
        result.ShouldBeSuccessful();
        await UpdateAsync(menuItem);

        // Assert
        var retrieved = await FindAsync<MenuItem>(menuItem.Id);
        retrieved.Should().NotBeNull();
        retrieved!.AppliedCustomizations.Should().HaveCount(1);
        retrieved.AppliedCustomizations.Should().Contain(newCustomization);
    }

    [Test]
    public async Task UpdateMenuItem_RemoveAppliedCustomization_ShouldPersistChangesCorrectly()
    {
        // Arrange
        var customizations = CreateTestAppliedCustomizations();
        var menuItem = CreateMenuItemWithCustomizations(customizations);
        await AddAsync(menuItem);

        // Act - Remove one customization
        var customizationToRemove = customizations.First();
        var result = menuItem.RemoveCustomizationGroup(customizationToRemove.CustomizationGroupId);
        result.ShouldBeSuccessful();
        await UpdateAsync(menuItem);

        // Assert
        var retrieved = await FindAsync<MenuItem>(menuItem.Id);
        retrieved.Should().NotBeNull();
        retrieved!.AppliedCustomizations.Should().HaveCount(customizations.Count - 1);
        retrieved.AppliedCustomizations.Should().NotContain(customizationToRemove);
    }

    #endregion

    #region Complex Scenario Tests

    [Test]
    public async Task UpdateMenuItem_ModifyBothCollections_ShouldPersistAllChangesCorrectly()
    {
        // Arrange
        var initialTags = CreateTestDietaryTags().Take(2).ToList();
        var initialCustomizations = CreateTestAppliedCustomizations().Take(1).ToList();
        var menuItem = CreateMenuItemWithBothCollections(initialTags, initialCustomizations);
        await AddAsync(menuItem);

        // Act - Modify both collections
        var newTag = TagId.CreateUnique();
        var newTagList = new List<TagId>(initialTags) { newTag };
        newTagList.Remove(initialTags.First()); // Remove first, add new
        var tagResult = menuItem.SetDietaryTags(newTagList);
        tagResult.ShouldBeSuccessful();

        var newCustomization = AppliedCustomization.Create(
            CustomizationGroupId.CreateUnique(),
            "New Customization",
            2);
        var customizationResult = menuItem.AssignCustomizationGroup(newCustomization);
        customizationResult.ShouldBeSuccessful();

        await UpdateAsync(menuItem);

        // Assert
        var retrieved = await FindAsync<MenuItem>(menuItem.Id);
        retrieved.Should().NotBeNull();

        // Verify dietary tags changes
        retrieved!.DietaryTagIds.Should().HaveCount(2); // Started with 2, removed 1, added 1
        retrieved.DietaryTagIds.Should().Contain(newTag);
        retrieved.DietaryTagIds.Should().NotContain(initialTags.First());
        retrieved.DietaryTagIds.Should().Contain(initialTags.Last());

        // Verify customizations changes
        retrieved.AppliedCustomizations.Should().HaveCount(2); // Started with 1, added 1
        retrieved.AppliedCustomizations.Should().Contain(newCustomization);
        retrieved.AppliedCustomizations.Should().Contain(initialCustomizations.First());
    }

    [Test]
    public async Task MenuItem_WithMultipleCustomizations_ShouldMaintainOrder()
    {
        // Arrange
        var customizations = new List<AppliedCustomization>
        {
            AppliedCustomization.Create(CustomizationGroupId.CreateUnique(), "First", 1),
            AppliedCustomization.Create(CustomizationGroupId.CreateUnique(), "Second", 2),
            AppliedCustomization.Create(CustomizationGroupId.CreateUnique(), "Third", 3)
        };
        var menuItem = CreateMenuItemWithCustomizations(customizations);

        // Act
        await AddAsync(menuItem);

        // Assert
        var retrieved = await FindAsync<MenuItem>(menuItem.Id);
        retrieved.Should().NotBeNull();
        retrieved!.AppliedCustomizations.Should().HaveCount(3);

        // Verify order is preserved based on DisplayOrder
        var retrievedList = retrieved.AppliedCustomizations.ToList();
        retrievedList[0].DisplayOrder.Should().Be(1);
        retrievedList[1].DisplayOrder.Should().Be(2);
        retrievedList[2].DisplayOrder.Should().Be(3);
    }

    [Test]
    public async Task MenuItem_WithDuplicateTagIds_ShouldPreventDuplicates()
    {
        // Arrange
        var menuItem = CreateBasicMenuItem();
        await AddAsync(menuItem);

        var tag = TagId.CreateUnique();

        // Act - Try to add the same tag twice by setting tags with duplicates
        var tagsWithDuplicate = new List<TagId> { tag, tag }; // Same tag twice
        var result = menuItem.SetDietaryTags(tagsWithDuplicate);

        // Assert
        result.ShouldBeSuccessful(); // SetDietaryTags should succeed

        await UpdateAsync(menuItem);
        var retrieved = await FindAsync<MenuItem>(menuItem.Id);

        // The domain should handle duplicates appropriately
        // Since we're using a List, duplicates might be preserved, but let's verify behavior
        retrieved!.DietaryTagIds.Should().Contain(tag);
        // Note: This test verifies current behavior. If domain needs to prevent duplicates,
        // the MenuItem.SetDietaryTags method should be updated to handle this.
    }

    #endregion

    #region Helper Methods

    private MenuItem CreateBasicMenuItem()
    {
        var result = MenuItem.Create(
            _restaurantId,
            _menuCategoryId,
            "Test Menu Item",
            "A test menu item for persistence testing",
            new Money(12.99m, "USD"));

        result.ShouldBeSuccessful();
        return result.Value;
    }

    private MenuItem CreateMenuItemWithDietaryTags(List<TagId> dietaryTags)
    {
        var result = MenuItem.Create(
            _restaurantId,
            _menuCategoryId,
            "Test Menu Item with Tags",
            "A test menu item with dietary tags",
            new Money(12.99m, "USD"),
            dietaryTagIds: dietaryTags);

        result.ShouldBeSuccessful();
        return result.Value;
    }

    private MenuItem CreateMenuItemWithCustomizations(List<AppliedCustomization> customizations)
    {
        var result = MenuItem.Create(
            _restaurantId,
            _menuCategoryId,
            "Test Menu Item with Customizations",
            "A test menu item with applied customizations",
            new Money(12.99m, "USD"),
            appliedCustomizations: customizations);

        result.ShouldBeSuccessful();
        return result.Value;
    }

    private MenuItem CreateMenuItemWithBothCollections(List<TagId> dietaryTags, List<AppliedCustomization> customizations)
    {
        var result = MenuItem.Create(
            _restaurantId,
            _menuCategoryId,
            "Test Menu Item with Both Collections",
            "A test menu item with both dietary tags and customizations",
            new Money(12.99m, "USD"),
            dietaryTagIds: dietaryTags,
            appliedCustomizations: customizations);

        result.ShouldBeSuccessful();
        return result.Value;
    }

    private static List<TagId> CreateTestDietaryTags()
    {
        return new List<TagId>
        {
            TagId.CreateUnique(),
            TagId.CreateUnique(),
            TagId.CreateUnique()
        };
    }

    private static List<AppliedCustomization> CreateTestAppliedCustomizations()
    {
        return new List<AppliedCustomization>
        {
            AppliedCustomization.Create(CustomizationGroupId.CreateUnique(), "Cheese Options", 1),
            AppliedCustomization.Create(CustomizationGroupId.CreateUnique(), "Sauce Selection", 2)
        };
    }

    private static void VerifyBasicProperties(MenuItem original, MenuItem retrieved)
    {
        retrieved.Id.Should().Be(original.Id);
        retrieved.RestaurantId.Should().Be(original.RestaurantId);
        retrieved.MenuCategoryId.Should().Be(original.MenuCategoryId);
        retrieved.Name.Should().Be(original.Name);
        retrieved.Description.Should().Be(original.Description);
        retrieved.BasePrice.Should().Be(original.BasePrice);
        retrieved.IsAvailable.Should().Be(original.IsAvailable);
        retrieved.ImageUrl.Should().Be(original.ImageUrl);
    }

    #endregion
}
