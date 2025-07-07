using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.TagEntity.ValueObjects;

namespace YummyZoom.Domain.UnitTests.MenuItemAggregate;

[TestFixture]
public class MenuItemPropertyImmutabilityTests : MenuItemTestHelpers
{
    [Test]
    public void RestaurantId_ShouldBeImmutableAfterCreation()
    {
        // Arrange
        var item = CreateValidMenuItem();
        var originalRestaurantId = item.RestaurantId;

        // Act & Assert - RestaurantId should not be changeable
        item.RestaurantId.Should().Be(originalRestaurantId);
        
        // Verify through updates that RestaurantId remains the same
        item.UpdateDetails("New Name", "New Description");
        item.UpdatePrice(new Money(20.00m, Currencies.Default));
        item.MarkAsUnavailable();
        item.AssignToCategory(MenuCategoryId.CreateUnique());
        
        item.RestaurantId.Should().Be(originalRestaurantId);
    }

    [Test]
    public void Id_ShouldBeImmutableAfterCreation()
    {
        // Arrange
        var item = CreateValidMenuItem();
        var originalId = item.Id;

        // Act & Assert - Id should not be changeable
        item.Id.Should().Be(originalId);
        
        // Verify through updates that Id remains the same
        item.UpdateDetails("New Name", "New Description");
        item.UpdatePrice(new Money(20.00m, Currencies.Default));
        item.MarkAsUnavailable();
        item.AssignToCategory(MenuCategoryId.CreateUnique());
        
        item.Id.Should().Be(originalId);
    }

    [Test]
    public void DietaryTagIds_ShouldBeReadOnly()
    {
        // Arrange
        var dietaryTags = new List<TagId> { TagId.CreateUnique() };

        // Act
        var item = MenuItem.Create(
            DefaultRestaurantId,
            DefaultMenuCategoryId,
            DefaultItemName,
            DefaultItemDescription,
            DefaultBasePrice,
            dietaryTagIds: dietaryTags).Value;

        // Assert
        // Type check
        var property = typeof(MenuItem).GetProperty(nameof(MenuItem.DietaryTagIds));
        property.Should().NotBeNull();
        typeof(IReadOnlyList<TagId>).IsAssignableFrom(property!.PropertyType).Should().BeTrue();

        // Immutability check: mutation should throw
        Action mutate = () => ((ICollection<TagId>)item.DietaryTagIds).Add(TagId.CreateUnique());
        mutate.Should().Throw<NotSupportedException>();

        // Verify that modifying the original list doesn't affect the item
        dietaryTags.Add(TagId.CreateUnique());
        item.DietaryTagIds.Should().HaveCount(1);
    }

    [Test]
    public void AppliedCustomizations_ShouldBeReadOnly()
    {
        // Arrange
        var customizations = new List<AppliedCustomization> 
        { 
            AppliedCustomization.Create(CustomizationGroupId.CreateUnique(), "Extra Cheese", 1)
        };

        // Act
        var item = MenuItem.Create(
            DefaultRestaurantId,
            DefaultMenuCategoryId,
            DefaultItemName,
            DefaultItemDescription,
            DefaultBasePrice,
            appliedCustomizations: customizations).Value;

        // Assert
        // Type check
        var property = typeof(MenuItem).GetProperty(nameof(MenuItem.AppliedCustomizations));
        property.Should().NotBeNull();
        typeof(IReadOnlyList<AppliedCustomization>).IsAssignableFrom(property!.PropertyType).Should().BeTrue();

        // Immutability check: mutation should throw
        Action mutate = () => ((ICollection<AppliedCustomization>)item.AppliedCustomizations).Add(
            AppliedCustomization.Create(CustomizationGroupId.CreateUnique(), "Extra Sauce", 2));
        mutate.Should().Throw<NotSupportedException>();

        // Verify that modifying the original list doesn't affect the item
        customizations.Add(AppliedCustomization.Create(CustomizationGroupId.CreateUnique(), "Extra Sauce", 2));
        item.AppliedCustomizations.Should().HaveCount(1);
    }
}
