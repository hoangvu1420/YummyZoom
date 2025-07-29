using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.MenuItemAggregate.Events;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.TagEntity.ValueObjects;

namespace YummyZoom.Domain.UnitTests.MenuItemAggregate;

[TestFixture]
public class MenuItemCreationTests : MenuItemTestHelpers
{
    [Test]
    public void Create_WithValidInputs_ShouldSucceedAndInitializeItemCorrectly()
    {
        // Arrange & Act
        var result = MenuItem.Create(
            DefaultRestaurantId,
            DefaultMenuCategoryId,
            DefaultItemName,
            DefaultItemDescription,
            DefaultBasePrice);

        // Assert
        result.ShouldBeSuccessful();
        var item = result.Value;
        
        item.Id.Value.Should().NotBe(Guid.Empty);
        item.RestaurantId.Should().Be(DefaultRestaurantId);
        item.MenuCategoryId.Should().Be(DefaultMenuCategoryId);
        item.Name.Should().Be(DefaultItemName);
        item.Description.Should().Be(DefaultItemDescription);
        item.BasePrice.Should().Be(DefaultBasePrice);
        item.ImageUrl.Should().BeNull();
        item.IsAvailable.Should().BeTrue();
        item.DietaryTagIds.Should().BeEmpty();
        item.AppliedCustomizations.Should().BeEmpty();
        
        item.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<MenuItemCreated>()
            .Which.Should().Match<MenuItemCreated>(e => 
                e.MenuItemId == item.Id && 
                e.RestaurantId == DefaultRestaurantId && 
                e.MenuCategoryId == DefaultMenuCategoryId);
    }

    [Test]
    public void Create_WithAllOptionalParameters_ShouldInitializeCorrectly()
    {
        // Arrange
        var dietaryTags = new List<TagId> { TagId.CreateUnique(), TagId.CreateUnique() };
        var customizations = new List<AppliedCustomization> 
        { 
            AppliedCustomization.Create(CustomizationGroupId.CreateUnique(), "Extra Cheese", 1)
        };

        // Act
        var result = MenuItem.Create(
            DefaultRestaurantId,
            DefaultMenuCategoryId,
            DefaultItemName,
            DefaultItemDescription,
            DefaultBasePrice,
            DefaultImageUrl,
            false, // isAvailable
            dietaryTags,
            customizations);

        // Assert
        result.ShouldBeSuccessful();
        var item = result.Value;
        
        item.ImageUrl.Should().Be(DefaultImageUrl);
        item.IsAvailable.Should().BeFalse();
        item.DietaryTagIds.Should().HaveCount(2);
        item.DietaryTagIds.Should().BeEquivalentTo(dietaryTags);
        item.AppliedCustomizations.Should().HaveCount(1);
        item.AppliedCustomizations.Should().BeEquivalentTo(customizations);
    }

    [Test]
    public void Create_WithInvalidName_ShouldFail()
    {
        // Act & Assert - Null name
        var nullResult = MenuItem.Create(DefaultRestaurantId, DefaultMenuCategoryId, null!, DefaultItemDescription, DefaultBasePrice);
        nullResult.ShouldBeFailure("MenuItem.InvalidName");

        // Act & Assert - Empty name
        var emptyResult = MenuItem.Create(DefaultRestaurantId, DefaultMenuCategoryId, "", DefaultItemDescription, DefaultBasePrice);
        emptyResult.ShouldBeFailure("MenuItem.InvalidName");

        // Act & Assert - Whitespace name
        var whitespaceResult = MenuItem.Create(DefaultRestaurantId, DefaultMenuCategoryId, "   ", DefaultItemDescription, DefaultBasePrice);
        whitespaceResult.ShouldBeFailure("MenuItem.InvalidName");
    }

    [Test]
    public void Create_WithInvalidDescription_ShouldFail()
    {
        // Act & Assert - Null description
        var nullResult = MenuItem.Create(DefaultRestaurantId, DefaultMenuCategoryId, DefaultItemName, null!, DefaultBasePrice);
        nullResult.ShouldBeFailure("MenuItem.InvalidDescription");

        // Act & Assert - Empty description
        var emptyResult = MenuItem.Create(DefaultRestaurantId, DefaultMenuCategoryId, DefaultItemName, "", DefaultBasePrice);
        emptyResult.ShouldBeFailure("MenuItem.InvalidDescription");

        // Act & Assert - Whitespace description
        var whitespaceResult = MenuItem.Create(DefaultRestaurantId, DefaultMenuCategoryId, DefaultItemName, "   ", DefaultBasePrice);
        whitespaceResult.ShouldBeFailure("MenuItem.InvalidDescription");
    }

    [Test]
    public void Create_WithNegativeOrZeroPrice_ShouldFail()
    {
        // Act & Assert - Zero price
        var zeroPrice = new Money(0, Currencies.Default);
        var zeroResult = MenuItem.Create(DefaultRestaurantId, DefaultMenuCategoryId, DefaultItemName, DefaultItemDescription, zeroPrice);
        zeroResult.ShouldBeFailure("MenuItem.NegativePrice");

        // Act & Assert - Negative price
        var negativePrice = new Money(-5.00m, Currencies.Default);
        var negativeResult = MenuItem.Create(DefaultRestaurantId, DefaultMenuCategoryId, DefaultItemName, DefaultItemDescription, negativePrice);
        negativeResult.ShouldBeFailure("MenuItem.NegativePrice");
    }
}
