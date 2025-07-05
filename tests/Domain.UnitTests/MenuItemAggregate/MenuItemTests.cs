using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;
using YummyZoom.Domain.Menu.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.Events;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.TagAggregate.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate;

namespace YummyZoom.Domain.UnitTests.MenuItemAggregate;

/// <summary>
/// Tests for MenuItem aggregate functionality including creation, updates, and lifecycle management.
/// </summary>
[TestFixture]
public class MenuItemTests
{
    private static readonly RestaurantId DefaultRestaurantId = RestaurantId.CreateUnique();
    private static readonly MenuCategoryId DefaultMenuCategoryId = MenuCategoryId.CreateUnique();
    private const string DefaultItemName = "Test Item";
    private const string DefaultItemDescription = "Test Item Description";
    private static readonly Money DefaultBasePrice = new Money(12.99m, Currencies.Default);
    private const string DefaultImageUrl = "https://example.com/image.jpg";

    #region Create() Method Tests

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

    #endregion

    #region UpdateDetails() Method Tests

    [Test]
    public void UpdateDetails_WithValidInputs_ShouldSucceedAndUpdateProperties()
    {
        // Arrange
        var item = CreateValidMenuItem();
        var newName = "Updated Item Name";
        var newDescription = "Updated Item Description";

        // Act
        var result = item.UpdateDetails(newName, newDescription);

        // Assert
        result.ShouldBeSuccessful();
        item.Name.Should().Be(newName);
        item.Description.Should().Be(newDescription);
    }

    [Test]
    public void UpdateDetails_WithInvalidName_ShouldFail()
    {
        // Arrange
        var item = CreateValidMenuItem();
        var originalName = item.Name;
        var originalDescription = item.Description;

        // Act & Assert - Null name
        var nullResult = item.UpdateDetails(null!, DefaultItemDescription);
        nullResult.ShouldBeFailure("MenuItem.InvalidName");
        item.Name.Should().Be(originalName);
        item.Description.Should().Be(originalDescription);

        // Act & Assert - Empty name
        var emptyResult = item.UpdateDetails("", DefaultItemDescription);
        emptyResult.ShouldBeFailure("MenuItem.InvalidName");
        item.Name.Should().Be(originalName);
        item.Description.Should().Be(originalDescription);

        // Act & Assert - Whitespace name
        var whitespaceResult = item.UpdateDetails("   ", DefaultItemDescription);
        whitespaceResult.ShouldBeFailure("MenuItem.InvalidName");
        item.Name.Should().Be(originalName);
        item.Description.Should().Be(originalDescription);
    }

    [Test]
    public void UpdateDetails_WithInvalidDescription_ShouldFail()
    {
        // Arrange
        var item = CreateValidMenuItem();
        var originalName = item.Name;
        var originalDescription = item.Description;

        // Act & Assert - Null description
        var nullResult = item.UpdateDetails(DefaultItemName, null!);
        nullResult.ShouldBeFailure("MenuItem.InvalidDescription");
        item.Name.Should().Be(originalName);
        item.Description.Should().Be(originalDescription);

        // Act & Assert - Empty description
        var emptyResult = item.UpdateDetails(DefaultItemName, "");
        emptyResult.ShouldBeFailure("MenuItem.InvalidDescription");
        item.Name.Should().Be(originalName);
        item.Description.Should().Be(originalDescription);

        // Act & Assert - Whitespace description
        var whitespaceResult = item.UpdateDetails(DefaultItemName, "   ");
        whitespaceResult.ShouldBeFailure("MenuItem.InvalidDescription");
        item.Name.Should().Be(originalName);
        item.Description.Should().Be(originalDescription);
    }

    [Test]
    public void UpdateDetails_ShouldNotUpdateName_WhenDescriptionIsInvalid()
    {
        // Arrange
        var item = CreateValidMenuItem();
        var originalName = item.Name;
        var originalDescription = item.Description;

        // Act
        var result = item.UpdateDetails("New Name", "");

        // Assert
        result.ShouldBeFailure("MenuItem.InvalidDescription");
        item.Name.Should().Be(originalName);
        item.Description.Should().Be(originalDescription);
    }

    #endregion

    #region UpdatePrice() Method Tests

    [Test]
    public void UpdatePrice_WithValidPrice_ShouldSucceedAndUpdatePrice()
    {
        // Arrange
        var item = CreateValidMenuItem();
        var newPrice = new Money(19.99m, Currencies.Default);
        item.ClearDomainEvents();

        // Act
        var result = item.UpdatePrice(newPrice);

        // Assert
        result.ShouldBeSuccessful();
        item.BasePrice.Should().Be(newPrice);
        
        item.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<MenuItemPriceChanged>()
            .Which.Should().Match<MenuItemPriceChanged>(e => 
                e.MenuItemId == item.Id && e.NewPrice == newPrice);
    }

    [Test]
    public void UpdatePrice_WithNegativeOrZeroPrice_ShouldFail()
    {
        // Arrange
        var item = CreateValidMenuItem();
        var originalPrice = item.BasePrice;
        item.ClearDomainEvents();

        // Act & Assert - Zero price
        var zeroPrice = new Money(0, Currencies.Default);
        var zeroResult = item.UpdatePrice(zeroPrice);
        zeroResult.ShouldBeFailure("MenuItem.NegativePrice");
        item.BasePrice.Should().Be(originalPrice);
        item.DomainEvents.Should().BeEmpty();

        // Act & Assert - Negative price
        var negativePrice = new Money(-5.00m, Currencies.Default);
        var negativeResult = item.UpdatePrice(negativePrice);
        negativeResult.ShouldBeFailure("MenuItem.NegativePrice");
        item.BasePrice.Should().Be(originalPrice);
        item.DomainEvents.Should().BeEmpty();
    }

    [Test]
    public void UpdatePrice_WithSamePrice_ShouldSucceedAndRaiseEvent()
    {
        // Arrange
        var item = CreateValidMenuItem();
        var currentPrice = item.BasePrice;
        item.ClearDomainEvents();

        // Act
        var result = item.UpdatePrice(currentPrice);

        // Assert
        result.ShouldBeSuccessful();
        item.BasePrice.Should().Be(currentPrice);
        
        item.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<MenuItemPriceChanged>();
    }

    #endregion

    #region Availability Methods Tests

    [Test]
    public void MarkAsUnavailable_WhenAvailable_ShouldMakeUnavailableAndRaiseEvent()
    {
        // Arrange
        var item = CreateValidMenuItem(isAvailable: true);
        item.ClearDomainEvents();

        // Act
        item.MarkAsUnavailable();

        // Assert
        item.IsAvailable.Should().BeFalse();
        
        item.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<MenuItemAvailabilityChanged>()
            .Which.Should().Match<MenuItemAvailabilityChanged>(e => 
                e.MenuItemId == item.Id && e.IsAvailable == false);
    }

    [Test]
    public void MarkAsUnavailable_WhenAlreadyUnavailable_ShouldNotRaiseEvent()
    {
        // Arrange
        var item = CreateValidMenuItem(isAvailable: false);
        item.ClearDomainEvents();

        // Act
        item.MarkAsUnavailable();

        // Assert
        item.IsAvailable.Should().BeFalse();
        item.DomainEvents.Should().BeEmpty();
    }

    [Test]
    public void MarkAsAvailable_WhenUnavailable_ShouldMakeAvailableAndRaiseEvent()
    {
        // Arrange
        var item = CreateValidMenuItem(isAvailable: false);
        item.ClearDomainEvents();

        // Act
        item.MarkAsAvailable();

        // Assert
        item.IsAvailable.Should().BeTrue();
        
        item.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<MenuItemAvailabilityChanged>()
            .Which.Should().Match<MenuItemAvailabilityChanged>(e => 
                e.MenuItemId == item.Id && e.IsAvailable == true);
    }

    [Test]
    public void MarkAsAvailable_WhenAlreadyAvailable_ShouldNotRaiseEvent()
    {
        // Arrange
        var item = CreateValidMenuItem(isAvailable: true);
        item.ClearDomainEvents();

        // Act
        item.MarkAsAvailable();

        // Assert
        item.IsAvailable.Should().BeTrue();
        item.DomainEvents.Should().BeEmpty();
    }

    #endregion

    #region AssignToCategory() Method Tests

    [Test]
    public void AssignToCategory_WithDifferentCategory_ShouldSucceedAndRaiseEvent()
    {
        // Arrange
        var item = CreateValidMenuItem();
        var oldCategoryId = item.MenuCategoryId;
        var newCategoryId = MenuCategoryId.CreateUnique();
        item.ClearDomainEvents();

        // Act
        var result = item.AssignToCategory(newCategoryId);

        // Assert
        result.ShouldBeSuccessful();
        item.MenuCategoryId.Should().Be(newCategoryId);
        
        item.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<MenuItemAssignedToCategory>()
            .Which.Should().Match<MenuItemAssignedToCategory>(e => 
                e.MenuItemId == item.Id && 
                e.OldCategoryId == oldCategoryId && 
                e.NewCategoryId == newCategoryId);
    }

    [Test]
    public void AssignToCategory_WithSameCategory_ShouldSucceedAndRaiseEvent()
    {
        // Arrange
        var item = CreateValidMenuItem();
        var currentCategoryId = item.MenuCategoryId;
        item.ClearDomainEvents();

        // Act
        var result = item.AssignToCategory(currentCategoryId);

        // Assert
        result.ShouldBeSuccessful();
        item.MenuCategoryId.Should().Be(currentCategoryId);
        
        item.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<MenuItemAssignedToCategory>();
    }

    #endregion

    #region Property Immutability Tests

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

    #endregion

    #region Helper Methods

    private static MenuItem CreateValidMenuItem(bool isAvailable = true)
    {
        return MenuItem.Create(
            DefaultRestaurantId,
            DefaultMenuCategoryId,
            DefaultItemName,
            DefaultItemDescription,
            DefaultBasePrice,
            isAvailable: isAvailable).Value;
    }

    private static MenuItem CreateValidMenuItem(string name, string description, Money price)
    {
        return MenuItem.Create(
            DefaultRestaurantId,
            DefaultMenuCategoryId,
            name,
            description,
            price).Value;
    }

    #endregion
} 
