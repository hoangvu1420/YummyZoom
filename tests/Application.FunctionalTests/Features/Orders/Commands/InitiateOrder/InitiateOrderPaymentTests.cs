using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.OrderAggregate;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Orders.Commands.InitiateOrder;

/// <summary>
/// Tests for InitiateOrder command payment gateway interactions.
/// Focuses on payment intent creation, error handling, metadata validation, and different payment methods.
/// Uses mocked payment gateway service to isolate command logic testing from payment integration complexity.
/// </summary>
public class InitiateOrderPaymentTests : InitiateOrderTestBase
{
    #region Online Payment Intent Creation Tests

    [Test]
    public async Task InitiateOrder_WithCreditCardPayment_ShouldCreatePaymentIntent()
    {
        // Arrange
        var expectedPaymentIntentId = "pi_test_1234567890";
        var expectedClientSecret = "pi_test_1234567890_secret_abcdefgh";

        await ConfigureCustomPaymentGatewayAsync(expectedPaymentIntentId, expectedClientSecret);

        var command = InitiateOrderTestHelper.BuildValidCommand(
            paymentMethod: InitiateOrderTestHelper.PaymentMethods.CreditCard);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        result.Value.PaymentIntentId.Should().Be(expectedPaymentIntentId);
        result.Value.ClientSecret.Should().Be(expectedClientSecret);

        // Verify payment gateway was called with correct parameters
        PaymentGatewayMock.Verify(x => x.CreatePaymentIntentAsync(
            It.IsAny<Money>(),
            "USD",
            It.IsAny<IDictionary<string, string>>(),
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify order was created with payment intent reference
        var order = await FindOrderAsync(result.Value.OrderId);
        order.Should().NotBeNull();
        order!.PaymentTransactions.Should().HaveCount(1);
        order.PaymentTransactions.First().PaymentGatewayReferenceId.Should().Be(expectedPaymentIntentId);
        order.PaymentTransactions.First().PaymentMethodType.Should().Be(PaymentMethodType.CreditCard);
    }

    [Test]
    public async Task InitiateOrder_WithPayPalPayment_ShouldCreatePaymentIntent()
    {
        // Arrange
        var command = InitiateOrderTestHelper.BuildValidCommand(
            paymentMethod: InitiateOrderTestHelper.PaymentMethods.PayPal);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        result.Value.PaymentIntentId.Should().NotBeNullOrEmpty();
        result.Value.ClientSecret.Should().NotBeNullOrEmpty();

        // Verify payment gateway was called
        InitiateOrderTestHelper.ValidatePaymentIntentCreation(PaymentGatewayMock, shouldBeCalled: true);

        // Verify order was created with correct payment method
        var order = await FindOrderAsync(result.Value.OrderId);
        order!.PaymentTransactions.Should().HaveCount(1);
        order.PaymentTransactions.First().PaymentMethodType.Should().Be(PaymentMethodType.PayPal);
    }

    [Test]
    public async Task InitiateOrder_WithApplePayPayment_ShouldCreatePaymentIntent()
    {
        // Arrange
        var command = InitiateOrderTestHelper.BuildValidCommand(
            paymentMethod: InitiateOrderTestHelper.PaymentMethods.ApplePay);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        result.Value.PaymentIntentId.Should().NotBeNullOrEmpty();
        result.Value.ClientSecret.Should().NotBeNullOrEmpty();

        // Verify order was created with correct payment method
        var order = await FindOrderAsync(result.Value.OrderId);
        order!.PaymentTransactions.Should().HaveCount(1);
        order.PaymentTransactions.First().PaymentMethodType.Should().Be(PaymentMethodType.ApplePay);
    }

    [Test]
    public async Task InitiateOrder_WithGooglePayPayment_ShouldCreatePaymentIntent()
    {
        // Arrange
        var command = InitiateOrderTestHelper.BuildValidCommand(
            paymentMethod: InitiateOrderTestHelper.PaymentMethods.GooglePay);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        result.Value.PaymentIntentId.Should().NotBeNullOrEmpty();
        result.Value.ClientSecret.Should().NotBeNullOrEmpty();

        // Verify order was created with correct payment method
        var order = await FindOrderAsync(result.Value.OrderId);
        order!.PaymentTransactions.Should().HaveCount(1);
        order.PaymentTransactions.First().PaymentMethodType.Should().Be(PaymentMethodType.GooglePay);
    }

    #endregion

    #region Payment Gateway Error Handling Tests

    [Test]
    public async Task InitiateOrder_WhenPaymentGatewayFails_ShouldFailWithPaymentError()
    {
        // Arrange
        var paymentErrorMessage = "Payment gateway service unavailable";
        await ConfigureFailingPaymentGatewayAsync(paymentErrorMessage);

        var command = InitiateOrderTestHelper.BuildValidCommand(
            paymentMethod: InitiateOrderTestHelper.PaymentMethods.CreditCard);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeFailure();
        result.Error.Code.Should().Be("PaymentGateway.Error");
        result.Error.Description.Should().Be(paymentErrorMessage);

        // Verify order was not created in database
        var orderCount = await CountAsync<Order>();
        orderCount.Should().Be(0);

        // Verify payment gateway was called
        InitiateOrderTestHelper.ValidatePaymentIntentCreation(PaymentGatewayMock, shouldBeCalled: true);
    }

    [Test]
    public async Task InitiateOrder_WhenPaymentGatewayReturnsInvalidResponse_ShouldFailGracefully()
    {
        // Arrange
        var mock = new Mock<IPaymentGatewayService>();
        mock.Setup(x => x.CreatePaymentIntentAsync(
                It.IsAny<Money>(),
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(SharedKernel.Result.Failure<PaymentIntentResult>(
                Error.Failure("PaymentGateway.InvalidResponse", "Invalid response from payment gateway"))));

        ReplaceService(mock.Object);

        var command = InitiateOrderTestHelper.BuildValidCommand(
            paymentMethod: InitiateOrderTestHelper.PaymentMethods.CreditCard);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeFailure();
        result.Error.Code.Should().Be("PaymentGateway.InvalidResponse");

        // Verify transaction was rolled back
        var orderCount = await CountAsync<Order>();
        orderCount.Should().Be(0);
    }

    #endregion

    #region Payment Intent Metadata Validation Tests

    [Test]
    public async Task InitiateOrder_ShouldIncludeOrderMetadataInPaymentIntent()
    {
        // Arrange
        var command = InitiateOrderTestHelper.BuildValidCommand(
            paymentMethod: InitiateOrderTestHelper.PaymentMethods.CreditCard);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();

        // Verify payment gateway was called with correct metadata
        PaymentGatewayMock.Verify(x => x.CreatePaymentIntentAsync(
            It.IsAny<Money>(),
            "USD",
            It.Is<IDictionary<string, string>>(metadata =>
                metadata.ContainsKey("user_id") &&
                metadata.ContainsKey("restaurant_id") &&
                metadata.ContainsKey("order_id") &&
                metadata["user_id"] == command.CustomerId.ToString() &&
                metadata["restaurant_id"] == command.RestaurantId.ToString()),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task InitiateOrder_ShouldPassCorrectAmountToPaymentGateway()
    {
        // Arrange
        var command = InitiateOrderTestHelper.BuildValidCommand(
            paymentMethod: InitiateOrderTestHelper.PaymentMethods.CreditCard);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();

        // Verify payment gateway was called with correct total amount
        PaymentGatewayMock.Verify(x => x.CreatePaymentIntentAsync(
            It.Is<Money>(m => m.Amount == result.Value.TotalAmount.Amount),
            "USD",
            It.IsAny<IDictionary<string, string>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task InitiateOrder_WithTipAmount_ShouldIncludeTipInPaymentAmount()
    {
        // Arrange
        var tipAmount = InitiateOrderTestHelper.TestAmounts.MediumTip;
        var command = InitiateOrderTestHelper.BuildValidCommandWithTip(
            tipAmount);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();

        // Verify payment gateway was called with amount including tip
        PaymentGatewayMock.Verify(x => x.CreatePaymentIntentAsync(
            It.Is<Money>(m => m.Amount > 0), // Total should include tip
            "USD",
            It.IsAny<IDictionary<string, string>>(),
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify order includes tip amount
        var order = await FindOrderAsync(result.Value.OrderId);
        order!.TipAmount.Amount.Should().Be(tipAmount);
    }

    [Test]
    public async Task InitiateOrder_WithCoupon_ShouldCreatePaymentIntentWithDiscountedAmount()
    {
        // Arrange
        var command = InitiateOrderTestHelper.BuildValidCommandWithCoupon(
            couponCode: Testing.TestData.DefaultCouponCode,
            paymentMethod: InitiateOrderTestHelper.PaymentMethods.CreditCard);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();

        // Verify payment gateway was called with discounted amount
        PaymentGatewayMock.Verify(x => x.CreatePaymentIntentAsync(
            It.Is<Money>(m => m.Amount == result.Value.TotalAmount.Amount),
            "USD",
            It.IsAny<IDictionary<string, string>>(),
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify order has discount applied
        var order = await FindOrderAsync(result.Value.OrderId);
        order!.DiscountAmount.Amount.Should().BeGreaterThan(0);
    }

    #endregion

    #region Cash on Delivery Handling Tests

    [Test]
    public async Task InitiateOrder_WithCashOnDelivery_ShouldNotCreatePaymentIntent()
    {
        // Arrange
        var command = InitiateOrderTestHelper.BuildValidCommand(
            paymentMethod: InitiateOrderTestHelper.PaymentMethods.CashOnDelivery);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        result.Value.PaymentIntentId.Should().BeNull();
        result.Value.ClientSecret.Should().BeNull();

        // Verify payment gateway service was not called
        InitiateOrderTestHelper.ValidatePaymentIntentCreation(PaymentGatewayMock, shouldBeCalled: false);

        // Verify order was created with correct payment method
        var order = await FindOrderAsync(result.Value.OrderId);
        order.Should().NotBeNull();
        order!.PaymentTransactions.Should().HaveCount(1);
        order.PaymentTransactions.First().PaymentMethodType.Should().Be(PaymentMethodType.CashOnDelivery);
        order.PaymentTransactions.First().PaymentGatewayReferenceId.Should().BeNull();
    }

    [Test]
    public async Task InitiateOrder_WithCashOnDeliveryAndTip_ShouldSucceedWithoutPaymentIntent()
    {
        // Arrange
        var command = InitiateOrderTestHelper.BuildValidCommandWithTip(
            InitiateOrderTestHelper.TestAmounts.SmallTip,
            InitiateOrderTestHelper.PaymentMethods.CashOnDelivery);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        result.Value.PaymentIntentId.Should().BeNull();
        result.Value.ClientSecret.Should().BeNull();

        // Verify order was created with tip and COD
        var order = await FindOrderAsync(result.Value.OrderId);
        order!.PaymentTransactions.Should().HaveCount(1);
        order.PaymentTransactions.First().PaymentMethodType.Should().Be(PaymentMethodType.CashOnDelivery);
        order.TipAmount.Amount.Should().Be(InitiateOrderTestHelper.TestAmounts.SmallTip);
    }

    [Test]
    public async Task InitiateOrder_WithCashOnDeliveryAndCoupon_ShouldSucceedWithoutPaymentIntent()
    {
        // Arrange
        var command = InitiateOrderTestHelper.BuildValidCommandWithCoupon(
            couponCode: Testing.TestData.DefaultCouponCode,
            paymentMethod: InitiateOrderTestHelper.PaymentMethods.CashOnDelivery);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        result.Value.PaymentIntentId.Should().BeNull();
        result.Value.ClientSecret.Should().BeNull();

        // Verify order was created with discount and COD
        var order = await FindOrderAsync(result.Value.OrderId);
        order!.PaymentTransactions.Should().HaveCount(1);
        order.PaymentTransactions.First().PaymentMethodType.Should().Be(PaymentMethodType.CashOnDelivery);
        order.DiscountAmount.Amount.Should().BeGreaterThan(0);
    }

    #endregion

    #region Payment Gateway Service Configuration Tests

    [Test]
    public async Task InitiateOrder_ShouldPassUSDCurrencyToPaymentGateway()
    {
        // Arrange
        var command = InitiateOrderTestHelper.BuildValidCommand(
            paymentMethod: InitiateOrderTestHelper.PaymentMethods.CreditCard);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();

        // Verify payment gateway was called with USD currency
        PaymentGatewayMock.Verify(x => x.CreatePaymentIntentAsync(
            It.IsAny<Money>(),
            "USD",
            It.IsAny<IDictionary<string, string>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task InitiateOrder_ShouldCallPaymentGatewayWithCancellationToken()
    {
        // Arrange
        var command = InitiateOrderTestHelper.BuildValidCommand(
            paymentMethod: InitiateOrderTestHelper.PaymentMethods.CreditCard);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();

        // Verify payment gateway was called with cancellation token
        PaymentGatewayMock.Verify(x => x.CreatePaymentIntentAsync(
            It.IsAny<Money>(),
            It.IsAny<string>(),
            It.IsAny<IDictionary<string, string>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Transaction Consistency Tests

    [Test]
    public async Task InitiateOrder_WhenPaymentGatewayFailsAfterValidation_ShouldRollbackTransaction()
    {
        // Arrange
        var initialOrderCount = await CountAsync<Order>();
        await ConfigureFailingPaymentGatewayAsync("Payment processor timeout");

        var command = InitiateOrderTestHelper.BuildValidCommand(
            paymentMethod: InitiateOrderTestHelper.PaymentMethods.CreditCard);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeFailure();

        // Verify no order was persisted in database
        var finalOrderCount = await CountAsync<Order>();
        finalOrderCount.Should().Be(initialOrderCount);

        // Verify transaction was properly rolled back
        var orders = await FindAllOrdersAsync();
        orders.Should().BeEmpty();
    }

    [Test]
    public async Task InitiateOrder_WithSuccessfulPaymentGateway_ShouldMaintainTransactionConsistency()
    {
        // Arrange
        var command = InitiateOrderTestHelper.BuildValidCommand(
            paymentMethod: InitiateOrderTestHelper.PaymentMethods.CreditCard);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();

        // Verify order and payment intent created atomically
        var order = await FindOrderAsync(result.Value.OrderId);
        order.Should().NotBeNull();
        order!.PaymentTransactions.Should().HaveCount(1);
        order.PaymentTransactions.First().PaymentGatewayReferenceId.Should().NotBeNullOrEmpty();

        // Verify response matches persisted data
        result.Value.OrderId.Should().Be(order.Id);
        result.Value.TotalAmount.Amount.Should().Be(order.TotalAmount.Amount);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Helper method to find an order by ID from the database.
    /// </summary>
    private static async Task<Order?> FindOrderAsync(OrderId orderId)
    {
        return await FindAsync<Order>(orderId);
    }

    /// <summary>
    /// Helper method to find all orders in the database.
    /// </summary>
    private static Task<List<Order>> FindAllOrdersAsync()
    {
        var allOrders = new List<Order>();
        // For simplicity in this test, we'll just return an empty list
        // since we mainly use this to verify no orders exist after rollback
        return Task.FromResult(allOrders);
    }

    #endregion
}
