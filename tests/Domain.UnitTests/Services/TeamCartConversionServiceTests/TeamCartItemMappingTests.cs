using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Domain.UnitTests.TeamCartAggregate;

namespace YummyZoom.Domain.UnitTests.Services.TeamCartConversionServiceTests;

/// <summary>
/// Tests focused on the mapping of TeamCartItems to OrderItems during conversion.
/// </summary>
[TestFixture]
public class TeamCartItemMappingTests : TeamCartConversionServiceTestsBase
{
    [Test]
    public void ConvertToOrder_ShouldMapTeamCartItemsToOrderItemsCorrectly()
    {
        // Arrange
        var teamCart = TeamCartTestHelpers.CreateTeamCartWithMultipleItems();
        teamCart.LockForPayment(teamCart.HostUserId);
        foreach (var member in teamCart.Members)
        {
            var memberTotal = teamCart.Items
                .Where(i => i.AddedByUserId == member.UserId)
                .Sum(i => i.LineItemTotal.Amount);
            teamCart.CommitToCashOnDelivery(member.UserId, new Money(memberTotal, "USD"));
        }

        var deliveryAddress = DeliveryAddress.Create(
            "123 Main St", "Anytown", "Anystate", "12345", "USA"
        ).Value;

        // Act
        var result = TeamCartConversionService.ConvertToOrder(
            teamCart,
            deliveryAddress,
            "No special instructions",
            null,
            Money.Zero("USD"),
            new Money(5, "USD"),
            new Money(2, "USD")
        );

        // Assert
        result.ShouldBeSuccessful();
        var (order, _) = result.Value;

        // Verify item count matches
        order.OrderItems.Should().HaveCount(teamCart.Items.Count);

        // Verify each item was mapped correctly
        foreach (var teamCartItem in teamCart.Items)
        {
            var orderItem = order.OrderItems.FirstOrDefault(i =>
                i.Snapshot_MenuItemId == teamCartItem.Snapshot_MenuItemId &&
                i.Quantity == teamCartItem.Quantity
            );
            orderItem.Should().NotBeNull();

            // Verify basic properties
            orderItem!.Snapshot_BasePriceAtOrder.Should().Be(teamCartItem.Snapshot_BasePriceAtOrder);
            orderItem.Snapshot_ItemName.Should().Be(teamCartItem.Snapshot_ItemName);

            // Verify customizations
            orderItem.SelectedCustomizations.Should().HaveCount(teamCartItem.SelectedCustomizations.Count);

            foreach (var teamCartCustomization in teamCartItem.SelectedCustomizations)
            {
                var orderCustomization = orderItem.SelectedCustomizations.FirstOrDefault(c =>
                    c.Snapshot_CustomizationGroupName == teamCartCustomization.Snapshot_CustomizationGroupName &&
                    c.Snapshot_ChoiceName == teamCartCustomization.Snapshot_ChoiceName
                );

                orderCustomization.Should().NotBeNull();
                orderCustomization!.Snapshot_ChoicePriceAdjustmentAtOrder
                    .Should().Be(teamCartCustomization.Snapshot_ChoicePriceAdjustmentAtOrder);
            }
        }
    }

    [Test]
    public void ConvertToOrder_WithCustomizationsOnMultipleItems_ShouldMapAllCorrectly()
    {
        // Arrange
        var teamCart = TeamCartTestHelpers.CreateTeamCartWithGuest();

        // Add items with customizations for both host and guest
        var menuItemId1 = MenuItemId.CreateUnique();
        var menuItemId2 = MenuItemId.CreateUnique();
        var menuCategoryId = MenuCategoryId.CreateUnique();

        var hostCustomizations = new List<TeamCartItemCustomization>
            {
                TeamCartItemCustomization.Create("Size", "Extra Cheese", new Money(2.50m, Currencies.Default)).Value,
                TeamCartItemCustomization.Create("Toppings", "Pepperoni", new Money(3.00m, Currencies.Default)).Value
            };

        var guestCustomizations = new List<TeamCartItemCustomization>
            {
                TeamCartItemCustomization.Create("Size", "Large", new Money(3.00m, Currencies.Default)).Value,
                TeamCartItemCustomization.Create("Crust", "Thin", new Money(1.50m, Currencies.Default)).Value
            };

        // Host adds item with customizations
        teamCart.AddItem(teamCart.HostUserId, menuItemId1, menuCategoryId, "Custom Pizza", new Money(20.00m, Currencies.Default), 1, hostCustomizations);

        // Guest adds item with customizations
        var guestUserId = teamCart.Members.First(m => m.Role == MemberRole.Guest).UserId;
        teamCart.AddItem(guestUserId, menuItemId2, menuCategoryId, "Custom Burger", new Money(15.00m, Currencies.Default), 1, guestCustomizations);

        teamCart.LockForPayment(teamCart.HostUserId);
        foreach (var member in teamCart.Members)
        {
            var memberTotal = teamCart.Items
                .Where(i => i.AddedByUserId == member.UserId)
                .Sum(i => i.LineItemTotal.Amount);
            teamCart.CommitToCashOnDelivery(member.UserId, new Money(memberTotal, "USD"));
        }

        var deliveryAddress = DeliveryAddress.Create(
            "123 Main St", "Anytown", "Anystate", "12345", "USA"
        ).Value;

        // Act
        var result = TeamCartConversionService.ConvertToOrder(
            teamCart,
            deliveryAddress,
            "No special instructions",
            null,
            Money.Zero("USD"),
            new Money(5, "USD"),
            new Money(2, "USD")
        );

        // Assert
        result.ShouldBeSuccessful();
        var (order, _) = result.Value;

        // Verify all items and their customizations were mapped correctly
        foreach (var teamCartItem in teamCart.Items)
        {
            var orderItem = order.OrderItems.FirstOrDefault(i =>
                i.Snapshot_MenuItemId == teamCartItem.Snapshot_MenuItemId
            );
            orderItem.Should().NotBeNull();

            // Verify customizations count
            orderItem!.SelectedCustomizations.Should().HaveCount(teamCartItem.SelectedCustomizations.Count);

            // Verify each customization
            foreach (var teamCartCustomization in teamCartItem.SelectedCustomizations)
            {
                var orderCustomization = orderItem.SelectedCustomizations.FirstOrDefault(c =>
                    c.Snapshot_CustomizationGroupName == teamCartCustomization.Snapshot_CustomizationGroupName &&
                    c.Snapshot_ChoiceName == teamCartCustomization.Snapshot_ChoiceName
                );

                orderCustomization.Should().NotBeNull();
                orderCustomization!.Snapshot_ChoicePriceAdjustmentAtOrder
                    .Should().Be(teamCartCustomization.Snapshot_ChoicePriceAdjustmentAtOrder);
            }
        }
    }

    [Test]
    public void ConvertToOrder_ShouldCalculateItemTotalCorrectly()
    {
        // Arrange
        var teamCart = TeamCartTestHelpers.CreateTeamCartWithMultipleItems();
        teamCart.LockForPayment(teamCart.HostUserId);
        foreach (var member in teamCart.Members)
        {
            var memberTotal = teamCart.Items
                .Where(i => i.AddedByUserId == member.UserId)
                .Sum(i => i.LineItemTotal.Amount);
            teamCart.CommitToCashOnDelivery(member.UserId, new Money(memberTotal, "USD"));
        }

        var deliveryAddress = DeliveryAddress.Create(
            "123 Main St", "Anytown", "Anystate", "12345", "USA"
        ).Value;

        // Act
        var result = TeamCartConversionService.ConvertToOrder(
            teamCart,
            deliveryAddress,
            "No special instructions",
            null,
            Money.Zero("USD"),
            new Money(5, "USD"),
            new Money(2, "USD")
        );

        // Assert
        result.ShouldBeSuccessful();
        var (order, _) = result.Value;

        // Verify each item total was calculated correctly
        foreach (var teamCartItem in teamCart.Items)
        {
            var orderItem = order.OrderItems.FirstOrDefault(i =>
                i.Snapshot_MenuItemId == teamCartItem.Snapshot_MenuItemId &&
                i.Quantity == teamCartItem.Quantity
            );
            orderItem.Should().NotBeNull();

            // The LineItemTotal in the mapped OrderItem should be identical to the one in the TeamCartItem.
            orderItem!.LineItemTotal.Should().Be(teamCartItem.LineItemTotal);
        }
    }
}
