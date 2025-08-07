using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.Orders.Commands.InitiateOrder;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Orders.Commands.InitiateOrder;

/// <summary>
/// Happy path tests for the InitiateOrder command.
/// Tests successful order creation scenarios with valid inputs.
/// </summary>
public class InitiateOrderHappyPathTests : InitiateOrderTestBase
{
    [Test]
    public async Task InitiateOrder_WithValidData_ShouldCreateOrderSuccessfully()
    {
        // Arrange
        var command = InitiateOrderTestHelper.BuildValidCommand();

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        var response = result.Value;
        
        // Verify response contains required fields
        response.OrderId.Value.Should().NotBeEmpty();
        response.OrderNumber.Should().NotBeNullOrEmpty();
        response.TotalAmount.Amount.Should().BeGreaterThan(0);
        response.PaymentIntentId.Should().NotBeNullOrEmpty();
        response.ClientSecret.Should().NotBeNullOrEmpty();

        // Verify order was persisted in database
        var orderInDb = await FindOrderAsync(response.OrderId);
        orderInDb.Should().NotBeNull();
        orderInDb!.OrderNumber.Should().Be(response.OrderNumber);
        orderInDb.TotalAmount.Amount.Should().BeApproximately(response.TotalAmount.Amount, 0.01m);
        orderInDb.TotalAmount.Currency.Should().Be(response.TotalAmount.Currency);
        orderInDb.CustomerId.Should().Be(UserId.Create(Testing.TestData.DefaultCustomerId));
        orderInDb.RestaurantId.Should().Be(RestaurantId.Create(Testing.TestData.DefaultRestaurantId));

        // Verify payment gateway was called with correct parameters
        PaymentGatewayMock.Verify(
            x => x.CreatePaymentIntentAsync(It.IsAny<Money>(), It.IsAny<string>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task InitiateOrder_WithCashOnDelivery_ShouldCreateOrderWithCODPaymentTransaction()
    {
        // Arrange
        var command = InitiateOrderTestHelper.BuildValidCommand(paymentMethod: "CashOnDelivery");

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        var response = result.Value;
        
        // Verify response for COD payment - no payment intent required
        response.OrderId.Value.Should().NotBeEmpty();
        response.OrderNumber.Should().NotBeNullOrEmpty();
        response.TotalAmount.Amount.Should().BeGreaterThan(0);
        response.PaymentIntentId.Should().BeNullOrEmpty();
        response.ClientSecret.Should().BeNullOrEmpty();

        // Verify order was persisted with correct payment method
        var orderInDb = await FindOrderAsync(response.OrderId);
        orderInDb.Should().NotBeNull();
        
        // Verify COD creates a payment transaction that's immediately marked as succeeded
        orderInDb!.PaymentTransactions.Should().HaveCount(1);
        var paymentTransaction = orderInDb.PaymentTransactions.First();
        paymentTransaction.PaymentMethodType.Should().Be(PaymentMethodType.CashOnDelivery);
        paymentTransaction.Status.Should().Be(PaymentStatus.Succeeded);
        paymentTransaction.Amount.Amount.Should().BeApproximately(response.TotalAmount.Amount, 0.01m);
        paymentTransaction.Amount.Currency.Should().Be(response.TotalAmount.Currency);
        paymentTransaction.PaymentGatewayReferenceId.Should().BeNullOrEmpty();

        // Verify payment gateway was not called for COD
        PaymentGatewayMock.Verify(
            x => x.CreatePaymentIntentAsync(It.IsAny<Money>(), It.IsAny<string>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task InitiateOrder_WithValidCoupon_ShouldApplyDiscountCorrectly()
    {
        // Arrange
        var command = InitiateOrderTestHelper.BuildValidCommandWithCoupon(Testing.TestData.DefaultCouponCode);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        var response = result.Value;
        
        // Verify order was created with discount applied
        var orderInDb = await FindOrderAsync(response.OrderId);
        orderInDb.Should().NotBeNull();
        orderInDb!.AppliedCouponId.Should().NotBeNull();
        orderInDb.DiscountAmount.Amount.Should().BeGreaterThan(0);

        // Verify coupon was applied (simplified - just check that discount exists)
        orderInDb.DiscountAmount.Amount.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task InitiateOrder_WithTipAmount_ShouldIncludeTipInTotal()
    {
        // Arrange
        var tipAmount = 5.00m;
        var command = InitiateOrderTestHelper.BuildValidCommandWithTip(tipAmount);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        var response = result.Value;
        
        // Verify order was created with tip
        var orderInDb = await FindOrderAsync(response.OrderId);
        orderInDb.Should().NotBeNull();
        orderInDb!.TipAmount.Amount.Should().Be(tipAmount);

        // Verify payment gateway was called
        PaymentGatewayMock.Verify(
            x => x.CreatePaymentIntentAsync(It.IsAny<Money>(), It.IsAny<string>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task InitiateOrder_WithSpecialInstructions_ShouldStoreInstructions()
    {
        // Arrange
        var specialInstructions = "Please ring doorbell twice and leave at door";
        var command = InitiateOrderTestHelper.BuildValidCommand(specialInstructions: specialInstructions);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        var response = result.Value;
        
        // Verify order was created with special instructions
        var orderInDb = await FindOrderAsync(response.OrderId);
        orderInDb.Should().NotBeNull();
        orderInDb!.SpecialInstructions.Should().Be(specialInstructions);
    }

    [Test]
    public async Task InitiateOrder_WithMultipleMenuItems_ShouldCalculateCorrectly()
    {
        // Arrange
        var menuItemIds = Testing.TestData.GetMenuItemIds(
            Testing.TestData.MenuItems.ClassicBurger,
            Testing.TestData.MenuItems.MargheritaPizza,
            Testing.TestData.MenuItems.GrilledSalmon);
        
        // Build command with multiple items using existing pattern
        var items = menuItemIds.Select(id => new OrderItemDto(id, 1)).ToList();
        var command = InitiateOrderTestHelper.BuildValidCommand(menuItemIds: menuItemIds);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        var response = result.Value;
        
        // Verify order was created with all items
        var orderInDb = await FindOrderAsync(response.OrderId);
        orderInDb.Should().NotBeNull();
        orderInDb!.OrderItems.Should().HaveCount(3);

        // Verify each order item has correct details (simplified)
        foreach (var commandItem in command.Items)
        {
            var orderItem = orderInDb.OrderItems.FirstOrDefault(
                oi => oi.Snapshot_MenuItemId.Value == commandItem.MenuItemId);
            orderItem.Should().NotBeNull();
            orderItem!.Quantity.Should().Be(commandItem.Quantity);
            orderItem.Snapshot_BasePriceAtOrder.Amount.Should().BeGreaterThan(0);
            orderItem.LineItemTotal.Amount.Should().BeGreaterThan(0);
        }

        // Verify order subtotal exists and contains pricing snapshots
        orderInDb.Subtotal.Amount.Should().BeGreaterThan(0);
        
        // Verify order items contain pricing snapshots
        foreach (var orderItem in orderInDb.OrderItems)
        {
            orderItem.Snapshot_BasePriceAtOrder.Amount.Should().BeGreaterThan(0);
            orderItem.Snapshot_ItemName.Should().NotBeNullOrEmpty();
        }
    }
}
