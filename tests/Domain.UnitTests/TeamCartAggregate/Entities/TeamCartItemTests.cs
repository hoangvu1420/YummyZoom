using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate.Entities;
using YummyZoom.Domain.TeamCartAggregate.Errors;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.TeamCartAggregate.Entities;

[TestFixture]
public class TeamCartItemTests
{
    private static readonly UserId DefaultUserId = UserId.CreateUnique();
    private static readonly MenuItemId DefaultMenuItemId = MenuItemId.CreateUnique();
    private static readonly MenuCategoryId DefaultMenuCategoryId = MenuCategoryId.CreateUnique();
    private const string DefaultItemName = "Margherita Pizza";
    private static readonly Money DefaultBasePrice = new Money(12.99m, "USD");
    private const int DefaultQuantity = 2;

    [Test]
    public void Create_WithValidInputs_ShouldSucceedAndCalculateLineItemTotal()
    {
        // Act
        var result = TeamCartItem.Create(
            DefaultUserId,
            DefaultMenuItemId,
            DefaultMenuCategoryId,
            DefaultItemName,
            DefaultBasePrice,
            DefaultQuantity);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var item = result.Value;
        
        item.Id.Should().NotBeNull();
        item.AddedByUserId.Should().Be(DefaultUserId);
        item.Snapshot_MenuItemId.Should().Be(DefaultMenuItemId);
        item.Snapshot_MenuCategoryId.Should().Be(DefaultMenuCategoryId);
        item.Snapshot_ItemName.Should().Be(DefaultItemName);
        item.Snapshot_BasePriceAtOrder.Should().Be(DefaultBasePrice);
        item.Quantity.Should().Be(DefaultQuantity);
        item.SelectedCustomizations.Should().BeEmpty();
        
        // Line item total should be base price * quantity
        var expectedTotal = new Money(DefaultBasePrice.Amount * DefaultQuantity, DefaultBasePrice.Currency);
        item.LineItemTotal.Should().Be(expectedTotal);
    }

    [Test]
    public void Create_WithCustomizations_ShouldCalculateLineItemTotalWithCustomizations()
    {
        // Arrange
        var customizations = new List<TeamCartItemCustomization>
        {
            TeamCartItemCustomization.Create("Size", "Large", new Money(2.00m, "USD")).Value,
            TeamCartItemCustomization.Create("Toppings", "Extra Cheese", new Money(1.50m, "USD")).Value
        };

        // Act
        var result = TeamCartItem.Create(
            DefaultUserId,
            DefaultMenuItemId,
            DefaultMenuCategoryId,
            DefaultItemName,
            DefaultBasePrice,
            DefaultQuantity,
            customizations);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var item = result.Value;
        
        item.SelectedCustomizations.Should().HaveCount(2);
        
        // Line item total should be (base price + customization total) * quantity
        var customizationTotal = 2.00m + 1.50m; // 3.50
        var itemTotal = DefaultBasePrice.Amount + customizationTotal; // 12.99 + 3.50 = 16.49
        var expectedTotal = new Money(itemTotal * DefaultQuantity, DefaultBasePrice.Currency); // 16.49 * 2 = 32.98
        
        item.LineItemTotal.Should().Be(expectedTotal);
    }

    [Test]
    public void Create_WithNullUserId_ShouldFailWithValidationError()
    {
        // Act
        var result = TeamCartItem.Create(
            null!,
            DefaultMenuItemId,
            DefaultMenuCategoryId,
            DefaultItemName,
            DefaultBasePrice,
            DefaultQuantity);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TeamCartErrors.ItemUserIdRequired);
    }

    [Test]
    public void Create_WithNullMenuItemId_ShouldFailWithMenuItemRequiredError()
    {
        // Act
        var result = TeamCartItem.Create(
            DefaultUserId,
            null!,
            DefaultMenuCategoryId,
            DefaultItemName,
            DefaultBasePrice,
            DefaultQuantity);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TeamCartErrors.MenuItemRequired);
    }

    [Test]
    public void Create_WithEmptyItemName_ShouldFailWithValidationError()
    {
        // Act
        var result = TeamCartItem.Create(
            DefaultUserId,
            DefaultMenuItemId,
            DefaultMenuCategoryId,
            "",
            DefaultBasePrice,
            DefaultQuantity);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TeamCartErrors.ItemNameRequired);
    }

    [Test]
    public void Create_WithNullItemName_ShouldFailWithValidationError()
    {
        // Act
        var result = TeamCartItem.Create(
            DefaultUserId,
            DefaultMenuItemId,
            DefaultMenuCategoryId,
            null!,
            DefaultBasePrice,
            DefaultQuantity);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TeamCartErrors.ItemNameRequired);
    }

    [Test]
    public void Create_WithZeroQuantity_ShouldFailWithInvalidQuantityError()
    {
        // Act
        var result = TeamCartItem.Create(
            DefaultUserId,
            DefaultMenuItemId,
            DefaultMenuCategoryId,
            DefaultItemName,
            DefaultBasePrice,
            0);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TeamCartErrors.InvalidQuantity);
    }

    [Test]
    public void Create_WithNegativeQuantity_ShouldFailWithInvalidQuantityError()
    {
        // Act
        var result = TeamCartItem.Create(
            DefaultUserId,
            DefaultMenuItemId,
            DefaultMenuCategoryId,
            DefaultItemName,
            DefaultBasePrice,
            -1);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TeamCartErrors.InvalidQuantity);
    }

    [Test]
    public void UpdateQuantity_WithValidQuantity_ShouldUpdateAndRecalculateTotal()
    {
        // Arrange
        var item = TeamCartItem.Create(
            DefaultUserId,
            DefaultMenuItemId,
            DefaultMenuCategoryId,
            DefaultItemName,
            DefaultBasePrice,
            DefaultQuantity).Value;

        var newQuantity = 5;

        // Act
        var result = item.UpdateQuantity(newQuantity);

        // Assert
        result.IsSuccess.Should().BeTrue();
        item.Quantity.Should().Be(newQuantity);
        
        var expectedTotal = new Money(DefaultBasePrice.Amount * newQuantity, DefaultBasePrice.Currency);
        item.LineItemTotal.Should().Be(expectedTotal);
    }

    [Test]
    public void UpdateQuantity_WithCustomizations_ShouldRecalculateTotalWithCustomizations()
    {
        // Arrange
        var customizations = new List<TeamCartItemCustomization>
        {
            TeamCartItemCustomization.Create("Size", "Large", new Money(2.00m, "USD")).Value
        };

        var item = TeamCartItem.Create(
            DefaultUserId,
            DefaultMenuItemId,
            DefaultMenuCategoryId,
            DefaultItemName,
            DefaultBasePrice,
            DefaultQuantity,
            customizations).Value;

        var newQuantity = 3;

        // Act
        var result = item.UpdateQuantity(newQuantity);

        // Assert
        result.IsSuccess.Should().BeTrue();
        item.Quantity.Should().Be(newQuantity);
        
        // (12.99 + 2.00) * 3 = 44.97
        var itemTotal = DefaultBasePrice.Amount + 2.00m;
        var expectedTotal = new Money(itemTotal * newQuantity, DefaultBasePrice.Currency);
        item.LineItemTotal.Should().Be(expectedTotal);
    }

    [Test]
    public void UpdateQuantity_WithZeroQuantity_ShouldFailWithInvalidQuantityError()
    {
        // Arrange
        var item = TeamCartItem.Create(
            DefaultUserId,
            DefaultMenuItemId,
            DefaultMenuCategoryId,
            DefaultItemName,
            DefaultBasePrice,
            DefaultQuantity).Value;

        // Act
        var result = item.UpdateQuantity(0);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TeamCartErrors.InvalidQuantity);
    }

    [Test]
    public void UpdateQuantity_WithNegativeQuantity_ShouldFailWithInvalidQuantityError()
    {
        // Arrange
        var item = TeamCartItem.Create(
            DefaultUserId,
            DefaultMenuItemId,
            DefaultMenuCategoryId,
            DefaultItemName,
            DefaultBasePrice,
            DefaultQuantity).Value;

        // Act
        var result = item.UpdateQuantity(-1);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TeamCartErrors.InvalidQuantity);
    }
}
