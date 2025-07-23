using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.TeamCartAggregate;

[TestFixture]
public class TeamCartCustomizationTests
{
    private static readonly UserId DefaultHostUserId = UserId.CreateUnique();
    private static readonly RestaurantId DefaultRestaurantId = RestaurantId.CreateUnique();
    private static readonly MenuItemId DefaultMenuItemId = MenuItemId.CreateUnique();
    private static readonly MenuCategoryId DefaultMenuCategoryId = MenuCategoryId.CreateUnique();
    private const string DefaultHostName = "Host User";
    private const string DefaultItemName = "Custom Pizza";
    private static readonly Money DefaultBasePrice = new Money(10.00m, "USD");

    private TeamCart CreateTeamCart()
    {
        var teamCart = TeamCart.Create(
            DefaultHostUserId,
            DefaultRestaurantId,
            DefaultHostName).Value;
        
        teamCart.ClearDomainEvents();
        return teamCart;
    }

    [Test]
    public void AddItem_WithSingleCustomization_ShouldCalculatePriceCorrectly()
    {
        // Arrange
        var teamCart = CreateTeamCart();
        var customizations = new List<TeamCartItemCustomization>
        {
            TeamCartItemCustomization.Create("Size", "Large", new Money(3.00m, "USD")).Value
        };

        // Act
        var result = teamCart.AddItem(
            DefaultHostUserId,
            DefaultMenuItemId,
            DefaultMenuCategoryId,
            DefaultItemName,
            DefaultBasePrice,
            1,
            customizations);

        // Assert
        result.ShouldBeSuccessful();
        var item = teamCart.Items.First();
        
        // Base price (10.00) + customization (3.00) = 13.00
        var expectedTotal = new Money(13.00m, "USD");
        item.LineItemTotal.Should().Be(expectedTotal);
        item.SelectedCustomizations.Should().HaveCount(1);
    }

    [Test]
    public void AddItem_WithMultipleCustomizations_ShouldCalculatePriceCorrectly()
    {
        // Arrange
        var teamCart = CreateTeamCart();
        var customizations = new List<TeamCartItemCustomization>
        {
            TeamCartItemCustomization.Create("Size", "Large", new Money(3.00m, "USD")).Value,
            TeamCartItemCustomization.Create("Crust", "Thin", new Money(1.50m, "USD")).Value,
            TeamCartItemCustomization.Create("Toppings", "Extra Cheese", new Money(2.00m, "USD")).Value,
            TeamCartItemCustomization.Create("Toppings", "Pepperoni", new Money(1.75m, "USD")).Value
        };

        // Act
        var result = teamCart.AddItem(
            DefaultHostUserId,
            DefaultMenuItemId,
            DefaultMenuCategoryId,
            DefaultItemName,
            DefaultBasePrice,
            2, // Quantity of 2
            customizations);

        // Assert
        result.ShouldBeSuccessful();
        var item = teamCart.Items.First();
        
        // Base price: 10.00
        // Customizations: 3.00 + 1.50 + 2.00 + 1.75 = 8.25
        // Item total: 10.00 + 8.25 = 18.25
        // Line total: 18.25 * 2 = 36.50
        var expectedTotal = new Money(36.50m, "USD");
        item.LineItemTotal.Should().Be(expectedTotal);
        item.SelectedCustomizations.Should().HaveCount(4);
    }

    [Test]
    public void AddItem_WithNegativePriceAdjustmentCustomization_ShouldCalculatePriceCorrectly()
    {
        // Arrange
        var teamCart = CreateTeamCart();
        var customizations = new List<TeamCartItemCustomization>
        {
            TeamCartItemCustomization.Create("Size", "Small", new Money(-2.00m, "USD")).Value,
            TeamCartItemCustomization.Create("Promotion", "Student Discount", new Money(-1.50m, "USD")).Value
        };

        // Act
        var result = teamCart.AddItem(
            DefaultHostUserId,
            DefaultMenuItemId,
            DefaultMenuCategoryId,
            DefaultItemName,
            DefaultBasePrice,
            1,
            customizations);

        // Assert
        result.ShouldBeSuccessful();
        var item = teamCart.Items.First();
        
        // Base price: 10.00
        // Customizations: -2.00 + (-1.50) = -3.50
        // Item total: 10.00 - 3.50 = 6.50
        var expectedTotal = new Money(6.50m, "USD");
        item.LineItemTotal.Should().Be(expectedTotal);
    }

    [Test]
    public void AddItem_WithZeroPriceAdjustmentCustomization_ShouldCalculatePriceCorrectly()
    {
        // Arrange
        var teamCart = CreateTeamCart();
        var customizations = new List<TeamCartItemCustomization>
        {
            TeamCartItemCustomization.Create("Preparation", "Normal", new Money(0.00m, "USD")).Value,
            TeamCartItemCustomization.Create("Spice Level", "Medium", new Money(0.00m, "USD")).Value
        };

        // Act
        var result = teamCart.AddItem(
            DefaultHostUserId,
            DefaultMenuItemId,
            DefaultMenuCategoryId,
            DefaultItemName,
            DefaultBasePrice,
            1,
            customizations);

        // Assert
        result.ShouldBeSuccessful();
        var item = teamCart.Items.First();
        
        // Base price should remain unchanged
        item.LineItemTotal.Should().Be(DefaultBasePrice);
        item.SelectedCustomizations.Should().HaveCount(2);
    }

    [Test]
    public void AddItem_WithoutCustomizations_ShouldCalculateBasePriceOnly()
    {
        // Arrange
        var teamCart = CreateTeamCart();

        // Act
        var result = teamCart.AddItem(
            DefaultHostUserId,
            DefaultMenuItemId,
            DefaultMenuCategoryId,
            DefaultItemName,
            DefaultBasePrice,
            3); // No customizations parameter

        // Assert
        result.IsSuccess.Should().BeTrue();
        var item = teamCart.Items.First();
        
        // Should be base price * quantity
        var expectedTotal = new Money(DefaultBasePrice.Amount * 3, "USD");
        item.LineItemTotal.Should().Be(expectedTotal);
        item.SelectedCustomizations.Should().BeEmpty();
    }

    [Test]
    public void AddItem_WithEmptyCustomizationsList_ShouldCalculateBasePriceOnly()
    {
        // Arrange
        var teamCart = CreateTeamCart();
        var emptyCustomizations = new List<TeamCartItemCustomization>();

        // Act
        var result = teamCart.AddItem(
            DefaultHostUserId,
            DefaultMenuItemId,
            DefaultMenuCategoryId,
            DefaultItemName,
            DefaultBasePrice,
            2,
            emptyCustomizations);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var item = teamCart.Items.First();

        var expectedTotal = new Money(DefaultBasePrice.Amount * 2, "USD");
        item.LineItemTotal.Should().Be(expectedTotal);
        item.SelectedCustomizations.Should().BeEmpty();
    }

    [Test]
    public void AddItem_CustomizationSnapshotting_ShouldPreserveCustomizationData()
    {
        // Arrange
        var teamCart = CreateTeamCart();
        var customizationGroupName = "Sauce Selection";
        var choiceName = "BBQ Sauce";
        var priceAdjustment = new Money(0.75m, "USD");

        var customizations = new List<TeamCartItemCustomization>
        {
            TeamCartItemCustomization.Create(customizationGroupName, choiceName, priceAdjustment).Value
        };

        // Act
        var result = teamCart.AddItem(
            DefaultHostUserId,
            DefaultMenuItemId,
            DefaultMenuCategoryId,
            DefaultItemName,
            DefaultBasePrice,
            1,
            customizations);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var item = teamCart.Items.First();
        var savedCustomization = item.SelectedCustomizations.First();
        
        // Verify snapshot data is preserved
        savedCustomization.Snapshot_CustomizationGroupName.Should().Be(customizationGroupName);
        savedCustomization.Snapshot_ChoiceName.Should().Be(choiceName);
        savedCustomization.Snapshot_ChoicePriceAdjustmentAtOrder.Should().Be(priceAdjustment);
    }

    [Test]
    public void AddItem_MultipleItemsWithDifferentCustomizations_ShouldCalculateIndependently()
    {
        // Arrange
        var teamCart = CreateTeamCart();
        
        var pizzaCustomizations = new List<TeamCartItemCustomization>
        {
            TeamCartItemCustomization.Create("Size", "Large", new Money(3.00m, "USD")).Value
        };
        
        var burgerCustomizations = new List<TeamCartItemCustomization>
        {
            TeamCartItemCustomization.Create("Add-ons", "Extra Patty", new Money(4.00m, "USD")).Value,
            TeamCartItemCustomization.Create("Add-ons", "Bacon", new Money(2.50m, "USD")).Value
        };

        // Act
        var pizzaResult = teamCart.AddItem(
            DefaultHostUserId,
            DefaultMenuItemId,
            DefaultMenuCategoryId,
            "Custom Pizza",
            new Money(12.00m, "USD"),
            1,
            pizzaCustomizations);

        var burgerResult = teamCart.AddItem(
            DefaultHostUserId,
            MenuItemId.CreateUnique(),
            DefaultMenuCategoryId,
            "Custom Burger",
            new Money(8.00m, "USD"),
            1,
            burgerCustomizations);

        // Assert
        pizzaResult.ShouldBeSuccessful();
        burgerResult.ShouldBeSuccessful();
        teamCart.Items.Should().HaveCount(2);
        
        var pizzaItem = teamCart.Items.First(i => i.Snapshot_ItemName == "Custom Pizza");
        var burgerItem = teamCart.Items.First(i => i.Snapshot_ItemName == "Custom Burger");
        
        // Pizza: 12.00 + 3.00 = 15.00
        pizzaItem.LineItemTotal.Should().Be(new Money(15.00m, "USD"));
        pizzaItem.SelectedCustomizations.Should().HaveCount(1);
        
        // Burger: 8.00 + 4.00 + 2.50 = 14.50
        burgerItem.LineItemTotal.Should().Be(new Money(14.50m, "USD"));
        burgerItem.SelectedCustomizations.Should().HaveCount(2);
    }
}
