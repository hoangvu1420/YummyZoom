using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.MenuAggregate.Entities;

namespace YummyZoom.Domain.UnitTests.MenuAggregate.Entities;

/// <summary>
/// Tests for MenuItem entity functionality.
/// </summary>
[TestFixture]
public class MenuItemTests
{
    private const string DefaultName = "Test Menu Item";
    private const string DefaultDescription = "Test Description";
    private static readonly Money DefaultBasePrice = new Money(12.99m, Currencies.Default);
    private const string DefaultImageUrl = "https://example.com/image.jpg";

    #region Create() Method Tests

    [Test]
    public void Create_WithValidInputs_ShouldSucceedAndInitializeCorrectly()
    {
        // Arrange & Act
        var result = MenuItem.Create(DefaultName, DefaultDescription, DefaultBasePrice);

        // Assert
        result.ShouldBeSuccessful();
        var menuItem = result.Value;
        
        menuItem.Id.Value.Should().NotBe(Guid.Empty);
        menuItem.Name.Should().Be(DefaultName);
        menuItem.Description.Should().Be(DefaultDescription);
        menuItem.BasePrice.Should().Be(DefaultBasePrice);
        menuItem.ImageUrl.Should().BeNull();
        menuItem.IsAvailable.Should().BeTrue();
        menuItem.DietaryTagIds.Should().BeEmpty();
        menuItem.AppliedCustomizations.Should().BeEmpty();
    }

    [Test]
    public void Create_WithAllParameters_ShouldSucceedAndInitializeCorrectly()
    {
        // Arrange & Act
        var result = MenuItem.Create(
            DefaultName,
            DefaultDescription,
            DefaultBasePrice,
            DefaultImageUrl,
            isAvailable: false);

        // Assert
        result.ShouldBeSuccessful();
        var menuItem = result.Value;
        
        menuItem.Name.Should().Be(DefaultName);
        menuItem.Description.Should().Be(DefaultDescription);
        menuItem.BasePrice.Should().Be(DefaultBasePrice);
        menuItem.ImageUrl.Should().Be(DefaultImageUrl);
        menuItem.IsAvailable.Should().BeFalse();
    }

    [Test]
    public void Create_WithNullOrEmptyName_ShouldFail()
    {
        // Act & Assert - Null name
        var nullResult = MenuItem.Create(null!, DefaultDescription, DefaultBasePrice);
        nullResult.ShouldBeFailure("Menu.InvalidItemName");

        // Act & Assert - Empty name
        var emptyResult = MenuItem.Create("", DefaultDescription, DefaultBasePrice);
        emptyResult.ShouldBeFailure("Menu.InvalidItemName");

        // Act & Assert - Whitespace name
        var whitespaceResult = MenuItem.Create("   ", DefaultDescription, DefaultBasePrice);
        whitespaceResult.ShouldBeFailure("Menu.InvalidItemName");
    }

    [Test]
    public void Create_WithNullOrEmptyDescription_ShouldFail()
    {
        // Act & Assert - Null description
        var nullResult = MenuItem.Create(DefaultName, null!, DefaultBasePrice);
        nullResult.ShouldBeFailure("Menu.InvalidItemDescription");

        // Act & Assert - Empty description
        var emptyResult = MenuItem.Create(DefaultName, "", DefaultBasePrice);
        emptyResult.ShouldBeFailure("Menu.InvalidItemDescription");

        // Act & Assert - Whitespace description
        var whitespaceResult = MenuItem.Create(DefaultName, "   ", DefaultBasePrice);
        whitespaceResult.ShouldBeFailure("Menu.InvalidItemDescription");
    }

    [Test]
    public void Create_WithNegativeOrZeroPrice_ShouldFail()
    {
        // Arrange
        var zeroPrice = new Money(0m, Currencies.Default);
        var negativePrice = new Money(-5.99m, Currencies.Default);

        // Act & Assert - Zero price
        var zeroResult = MenuItem.Create(DefaultName, DefaultDescription, zeroPrice);
        zeroResult.ShouldBeFailure("Menu.NegativeMenuItemPrice");

        // Act & Assert - Negative price
        var negativeResult = MenuItem.Create(DefaultName, DefaultDescription, negativePrice);
        negativeResult.ShouldBeFailure("Menu.NegativeMenuItemPrice");
    }

    #endregion

    #region Availability Methods Tests

    [Test]
    public void MarkAsUnavailable_WhenAvailable_ShouldMakeUnavailable()
    {
        // Arrange
        var menuItem = CreateValidMenuItem(isAvailable: true);

        // Act
        menuItem.MarkAsUnavailable();

        // Assert
        menuItem.IsAvailable.Should().BeFalse();
    }

    [Test]
    public void MarkAsUnavailable_WhenAlreadyUnavailable_ShouldRemainUnavailable()
    {
        // Arrange
        var menuItem = CreateValidMenuItem(isAvailable: false);

        // Act
        menuItem.MarkAsUnavailable();

        // Assert
        menuItem.IsAvailable.Should().BeFalse();
    }

    [Test]
    public void MarkAsAvailable_WhenUnavailable_ShouldMakeAvailable()
    {
        // Arrange
        var menuItem = CreateValidMenuItem(isAvailable: false);

        // Act
        menuItem.MarkAsAvailable();

        // Assert
        menuItem.IsAvailable.Should().BeTrue();
    }

    [Test]
    public void MarkAsAvailable_WhenAlreadyAvailable_ShouldRemainAvailable()
    {
        // Arrange
        var menuItem = CreateValidMenuItem(isAvailable: true);

        // Act
        menuItem.MarkAsAvailable();

        // Assert
        menuItem.IsAvailable.Should().BeTrue();
    }

    #endregion

    #region UpdateDetails() Method Tests

    [Test]
    public void UpdateDetails_WithValidInputs_ShouldSucceedAndUpdateProperties()
    {
        // Arrange
        var menuItem = CreateValidMenuItem();
        var newName = "Updated Item Name";
        var newDescription = "Updated Item Description";

        // Act
        var result = menuItem.UpdateDetails(newName, newDescription);

        // Assert
        result.ShouldBeSuccessful();
        menuItem.Name.Should().Be(newName);
        menuItem.Description.Should().Be(newDescription);
    }

    [Test]
    public void UpdateDetails_WithNullOrEmptyName_ShouldFail()
    {
        // Arrange
        var menuItem = CreateValidMenuItem();

        // Act & Assert - Null name
        var nullResult = menuItem.UpdateDetails(null!, DefaultDescription);
        nullResult.ShouldBeFailure("Menu.InvalidItemName");

        // Act & Assert - Empty name
        var emptyResult = menuItem.UpdateDetails("", DefaultDescription);
        emptyResult.ShouldBeFailure("Menu.InvalidItemName");

        // Act & Assert - Whitespace name
        var whitespaceResult = menuItem.UpdateDetails("   ", DefaultDescription);
        whitespaceResult.ShouldBeFailure("Menu.InvalidItemName");
    }

    [Test]
    public void UpdateDetails_WithNullOrEmptyDescription_ShouldFail()
    {
        // Arrange
        var menuItem = CreateValidMenuItem();

        // Act & Assert - Null description
        var nullResult = menuItem.UpdateDetails(DefaultName, null!);
        nullResult.ShouldBeFailure("Menu.InvalidItemDescription");

        // Act & Assert - Empty description
        var emptyResult = menuItem.UpdateDetails(DefaultName, "");
        emptyResult.ShouldBeFailure("Menu.InvalidItemDescription");

        // Act & Assert - Whitespace description
        var whitespaceResult = menuItem.UpdateDetails(DefaultName, "   ");
        whitespaceResult.ShouldBeFailure("Menu.InvalidItemDescription");
    }

    #endregion

    #region UpdatePrice() Method Tests

    [Test]
    public void UpdatePrice_WithValidPrice_ShouldSucceedAndUpdatePrice()
    {
        // Arrange
        var menuItem = CreateValidMenuItem();
        var newPrice = new Money(19.99m, Currencies.Default);

        // Act
        var result = menuItem.UpdatePrice(newPrice);

        // Assert
        result.ShouldBeSuccessful();
        menuItem.BasePrice.Should().Be(newPrice);
    }

    [Test]
    public void UpdatePrice_WithNegativeOrZeroPrice_ShouldFail()
    {
        // Arrange
        var menuItem = CreateValidMenuItem();
        var zeroPrice = new Money(0m, Currencies.Default);
        var negativePrice = new Money(-5.99m, Currencies.Default);

        // Act & Assert - Zero price
        var zeroResult = menuItem.UpdatePrice(zeroPrice);
        zeroResult.ShouldBeFailure("Menu.NegativeMenuItemPrice");

        // Act & Assert - Negative price
        var negativeResult = menuItem.UpdatePrice(negativePrice);
        negativeResult.ShouldBeFailure("Menu.NegativeMenuItemPrice");

        // Original price should remain unchanged
        menuItem.BasePrice.Should().Be(DefaultBasePrice);
    }

    #endregion

    #region UpdateImageUrl() Method Tests

    [Test]
    public void UpdateImageUrl_WithValidUrl_ShouldUpdateImageUrl()
    {
        // Arrange
        var menuItem = CreateValidMenuItem();
        var newImageUrl = "https://example.com/new-image.jpg";

        // Act
        menuItem.UpdateImageUrl(newImageUrl);

        // Assert
        menuItem.ImageUrl.Should().Be(newImageUrl);
    }

    [Test]
    public void UpdateImageUrl_WithNull_ShouldSetToNull()
    {
        // Arrange
        var menuItem = CreateValidMenuItem(imageUrl: DefaultImageUrl);

        // Act
        menuItem.UpdateImageUrl(null);

        // Assert
        menuItem.ImageUrl.Should().BeNull();
    }

    #endregion

    #region UpdateFullDetails() Method Tests

    [Test]
    public void UpdateFullDetails_WithValidInputs_ShouldSucceedAndUpdateAllProperties()
    {
        // Arrange
        var menuItem = CreateValidMenuItem();
        var newName = "Updated Name";
        var newDescription = "Updated Description";
        var newPrice = new Money(25.99m, Currencies.Default);
        var newImageUrl = "https://example.com/updated-image.jpg";

        // Act
        var result = menuItem.UpdateFullDetails(newName, newDescription, newPrice, newImageUrl);

        // Assert
        result.ShouldBeSuccessful();
        menuItem.Name.Should().Be(newName);
        menuItem.Description.Should().Be(newDescription);
        menuItem.BasePrice.Should().Be(newPrice);
        menuItem.ImageUrl.Should().Be(newImageUrl);
    }

    [Test]
    public void UpdateFullDetails_WithNullImageUrl_ShouldUpdateWithNullImageUrl()
    {
        // Arrange
        var menuItem = CreateValidMenuItem(imageUrl: DefaultImageUrl);
        var newName = "Updated Name";
        var newDescription = "Updated Description";
        var newPrice = new Money(25.99m, Currencies.Default);

        // Act
        var result = menuItem.UpdateFullDetails(newName, newDescription, newPrice, null);

        // Assert
        result.ShouldBeSuccessful();
        menuItem.Name.Should().Be(newName);
        menuItem.Description.Should().Be(newDescription);
        menuItem.BasePrice.Should().Be(newPrice);
        menuItem.ImageUrl.Should().BeNull();
    }

    [Test]
    public void UpdateFullDetails_WithInvalidName_ShouldFail()
    {
        // Arrange
        var menuItem = CreateValidMenuItem();
        var newPrice = new Money(25.99m, Currencies.Default);

        // Act & Assert - Null name
        var nullResult = menuItem.UpdateFullDetails(null!, DefaultDescription, newPrice);
        nullResult.ShouldBeFailure("Menu.InvalidItemName");

        // Act & Assert - Empty name
        var emptyResult = menuItem.UpdateFullDetails("", DefaultDescription, newPrice);
        emptyResult.ShouldBeFailure("Menu.InvalidItemName");
    }

    [Test]
    public void UpdateFullDetails_WithInvalidDescription_ShouldFail()
    {
        // Arrange
        var menuItem = CreateValidMenuItem();
        var newPrice = new Money(25.99m, Currencies.Default);

        // Act & Assert - Null description
        var nullResult = menuItem.UpdateFullDetails(DefaultName, null!, newPrice);
        nullResult.ShouldBeFailure("Menu.InvalidItemDescription");

        // Act & Assert - Empty description
        var emptyResult = menuItem.UpdateFullDetails(DefaultName, "", newPrice);
        emptyResult.ShouldBeFailure("Menu.InvalidItemDescription");
    }

    [Test]
    public void UpdateFullDetails_WithInvalidPrice_ShouldFail()
    {
        // Arrange
        var menuItem = CreateValidMenuItem();
        var zeroPrice = new Money(0m, Currencies.Default);

        // Act
        var result = menuItem.UpdateFullDetails(DefaultName, DefaultDescription, zeroPrice);

        // Assert
        result.ShouldBeFailure("Menu.NegativeMenuItemPrice");
        
        // Original values should remain unchanged
        menuItem.Name.Should().Be(DefaultName);
        menuItem.Description.Should().Be(DefaultDescription);
        menuItem.BasePrice.Should().Be(DefaultBasePrice);
    }

    #endregion

    #region Helper Methods

    private static MenuItem CreateValidMenuItem(bool isAvailable = true, string? imageUrl = null)
    {
        return MenuItem.Create(DefaultName, DefaultDescription, DefaultBasePrice, imageUrl, isAvailable).Value;
    }

    #endregion
}
