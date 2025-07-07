using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.MenuItemAggregate.Events;

namespace YummyZoom.Domain.UnitTests.MenuItemAggregate;

[TestFixture]
public class MenuItemPropertyUpdateTests : MenuItemTestHelpers
{
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
}
