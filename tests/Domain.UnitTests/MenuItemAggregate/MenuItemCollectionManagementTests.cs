using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.Events;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.TagEntity.ValueObjects;

namespace YummyZoom.Domain.UnitTests.MenuItemAggregate;

[TestFixture]
public class MenuItemCustomizationManagementTests : MenuItemTestHelpers
{
    [Test]
    public void AssignCustomizationGroup_WithValidCustomization_ShouldSucceedAndAddToCollection()
    {
        // Arrange
        var menuItem = CreateValidMenuItem();
        var customization = AppliedCustomization.Create(
            CustomizationGroupId.CreateUnique(),
            "Test Customization",
            1);

        // Act
        var result = menuItem.AssignCustomizationGroup(customization);

        // Assert
        result.ShouldBeSuccessful();
        menuItem.AppliedCustomizations.Should().HaveCount(1);
        menuItem.AppliedCustomizations.Should().Contain(customization);
        
        // Verify domain event was raised
        menuItem.DomainEvents.Should().HaveCount(2); // MenuItemCreated + MenuItemCustomizationAssigned
        var assignedEvent = menuItem.DomainEvents.OfType<MenuItemCustomizationAssigned>().FirstOrDefault();
        assignedEvent.Should().NotBeNull();
        assignedEvent!.MenuItemId.Should().Be(menuItem.Id);
        assignedEvent.CustomizationGroupId.Should().Be(customization.CustomizationGroupId);
        assignedEvent.DisplayTitle.Should().Be(customization.DisplayTitle);
    }

    [Test]
    public void AssignCustomizationGroup_WithDuplicateCustomization_ShouldFail()
    {
        // Arrange
        var menuItem = CreateValidMenuItem();
        var customizationId = CustomizationGroupId.CreateUnique();
        var customization1 = AppliedCustomization.Create(customizationId, "Test Customization 1", 1);
        var customization2 = AppliedCustomization.Create(customizationId, "Test Customization 2", 2);
        
        menuItem.AssignCustomizationGroup(customization1);

        // Act
        var result = menuItem.AssignCustomizationGroup(customization2);

        // Assert
        result.ShouldBeFailure();
        result.Error.Code.Should().Be("MenuItem.CustomizationAlreadyAssigned");
        menuItem.AppliedCustomizations.Should().HaveCount(1);
        menuItem.AppliedCustomizations.Should().Contain(customization1);
    }

    [Test]
    public void AssignCustomizationGroup_WithNullCustomization_ShouldFail()
    {
        // Arrange
        var menuItem = CreateValidMenuItem();

        // Act
        var result = menuItem.AssignCustomizationGroup(null!);

        // Assert
        result.ShouldBeFailure();
        result.Error.Code.Should().Be("MenuItem.InvalidCustomization");
        menuItem.AppliedCustomizations.Should().BeEmpty();
    }

    [Test]
    public void RemoveCustomizationGroup_WithExistingCustomization_ShouldSucceedAndRemoveFromCollection()
    {
        // Arrange
        var menuItem = CreateValidMenuItem();
        var customization = AppliedCustomization.Create(
            CustomizationGroupId.CreateUnique(),
            "Test Customization",
            1);
        menuItem.AssignCustomizationGroup(customization);

        // Act
        var result = menuItem.RemoveCustomizationGroup(customization.CustomizationGroupId);

        // Assert
        result.ShouldBeSuccessful();
        menuItem.AppliedCustomizations.Should().BeEmpty();
        
        // Verify domain event was raised
        var removedEvent = menuItem.DomainEvents.OfType<MenuItemCustomizationRemoved>().FirstOrDefault();
        removedEvent.Should().NotBeNull();
        removedEvent!.MenuItemId.Should().Be(menuItem.Id);
        removedEvent.CustomizationGroupId.Should().Be(customization.CustomizationGroupId);
    }

    [Test]
    public void RemoveCustomizationGroup_WithNonExistentCustomization_ShouldFail()
    {
        // Arrange
        var menuItem = CreateValidMenuItem();
        var nonExistentId = CustomizationGroupId.CreateUnique();

        // Act
        var result = menuItem.RemoveCustomizationGroup(nonExistentId);

        // Assert
        result.ShouldBeFailure();
        result.Error.Code.Should().Be("MenuItem.CustomizationNotFound");
        menuItem.AppliedCustomizations.Should().BeEmpty();
    }

    [Test]
    public void RemoveCustomizationGroup_WithMultipleCustomizations_ShouldRemoveOnlySpecified()
    {
        // Arrange
        var menuItem = CreateValidMenuItem();
        var customization1 = AppliedCustomization.Create(CustomizationGroupId.CreateUnique(), "Customization 1", 1);
        var customization2 = AppliedCustomization.Create(CustomizationGroupId.CreateUnique(), "Customization 2", 2);
        
        menuItem.AssignCustomizationGroup(customization1);
        menuItem.AssignCustomizationGroup(customization2);

        // Act
        var result = menuItem.RemoveCustomizationGroup(customization1.CustomizationGroupId);

        // Assert
        result.ShouldBeSuccessful();
        menuItem.AppliedCustomizations.Should().HaveCount(1);
        menuItem.AppliedCustomizations.Should().Contain(customization2);
        menuItem.AppliedCustomizations.Should().NotContain(customization1);
    }
}

[TestFixture]
public class MenuItemDietaryTagManagementTests : MenuItemTestHelpers
{
    [Test]
    public void SetDietaryTags_WithValidTags_ShouldSucceedAndReplaceCollection()
    {
        // Arrange
        var menuItem = CreateValidMenuItem();
        var initialTags = new List<TagId> { TagId.CreateUnique(), TagId.CreateUnique() };
        var newTags = new List<TagId> { TagId.CreateUnique(), TagId.CreateUnique(), TagId.CreateUnique() };

        // Set initial tags
        menuItem.SetDietaryTags(initialTags);

        // Act
        var result = menuItem.SetDietaryTags(newTags);

        // Assert
        result.ShouldBeSuccessful();
        menuItem.DietaryTagIds.Should().HaveCount(3);
        menuItem.DietaryTagIds.Should().BeEquivalentTo(newTags);
        menuItem.DietaryTagIds.Should().NotContain(initialTags);
        
        // Verify domain event was raised
        var updatedEvent = menuItem.DomainEvents.OfType<MenuItemDietaryTagsUpdated>().LastOrDefault();
        updatedEvent.Should().NotBeNull();
        updatedEvent!.MenuItemId.Should().Be(menuItem.Id);
        updatedEvent.TagIds.Should().BeEquivalentTo(newTags);
    }

    [Test]
    public void SetDietaryTags_WithEmptyList_ShouldSucceedAndClearCollection()
    {
        // Arrange
        var menuItem = CreateValidMenuItem();
        var initialTags = new List<TagId> { TagId.CreateUnique(), TagId.CreateUnique() };
        menuItem.SetDietaryTags(initialTags);

        // Act
        var result = menuItem.SetDietaryTags(new List<TagId>());

        // Assert
        result.ShouldBeSuccessful();
        menuItem.DietaryTagIds.Should().BeEmpty();
        
        // Verify domain event was raised
        var updatedEvent = menuItem.DomainEvents.OfType<MenuItemDietaryTagsUpdated>().LastOrDefault();
        updatedEvent.Should().NotBeNull();
        updatedEvent!.TagIds.Should().BeEmpty();
    }

    [Test]
    public void SetDietaryTags_WithNullList_ShouldSucceedAndClearCollection()
    {
        // Arrange
        var menuItem = CreateValidMenuItem();
        var initialTags = new List<TagId> { TagId.CreateUnique(), TagId.CreateUnique() };
        menuItem.SetDietaryTags(initialTags);

        // Act
        var result = menuItem.SetDietaryTags(null);

        // Assert
        result.ShouldBeSuccessful();
        menuItem.DietaryTagIds.Should().BeEmpty();
        
        // Verify domain event was raised
        var updatedEvent = menuItem.DomainEvents.OfType<MenuItemDietaryTagsUpdated>().LastOrDefault();
        updatedEvent.Should().NotBeNull();
        updatedEvent!.TagIds.Should().BeEmpty();
    }

    [Test]
    public void SetDietaryTags_WithSameTags_ShouldSucceedAndKeepSameCollection()
    {
        // Arrange
        var menuItem = CreateValidMenuItem();
        var tags = new List<TagId> { TagId.CreateUnique(), TagId.CreateUnique() };

        // Act
        var result = menuItem.SetDietaryTags(tags);

        // Assert
        result.ShouldBeSuccessful();
        menuItem.DietaryTagIds.Should().HaveCount(2);
        menuItem.DietaryTagIds.Should().BeEquivalentTo(tags);
    }
}
