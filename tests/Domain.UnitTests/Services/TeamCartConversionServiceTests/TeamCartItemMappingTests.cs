using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
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
            0,
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
        var teamCart = TeamCartTestHelpers.CreateTeamCartWithCustomizations();
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
            0,
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
            0,
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
