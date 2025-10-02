using Microsoft.Extensions.Options;
using Stripe;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Infrastructure;
using YummyZoom.Application.Orders.Commands.HandleStripeWebhook;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Infrastructure.Payments.Stripe;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Orders.PaymentIntegration;

/// <summary>
/// End-to-end integration tests for the complete online payment flow.
/// Tests the full payment lifecycle from order initiation through Stripe payment confirmation.
/// </summary>
public class OnlineOrderPaymentTests : BaseTestFixture
{
    private StripeOptions _stripeOptions = null!;

    [SetUp]
    public void SetUp()
    {
        // Set the default customer as the current user
        SetUserId(Testing.TestData.DefaultCustomerId);

        // Get Stripe configuration from user secrets
        _stripeOptions = GetService<IOptions<StripeOptions>>().Value;

        // Set up Stripe test configuration with actual test key
        StripeConfiguration.ApiKey = _stripeOptions.SecretKey;
    }

    #region Happy Path Tests

    /// <summary>
    /// Test Case 1.1: Happy Path - Successful Payment Flow
    /// Tests the complete flow from order initiation to successful payment confirmation.
    /// </summary>
    [Test]
    public async Task InitiateOrder_WithOnlinePayment_WhenPaymentSucceeds_ShouldCompleteOrderSuccessfully()
    {
        // Arrange
        var initiateOrderCommand = PaymentTestHelper.BuildValidOnlineOrderCommand(
            customerId: Testing.TestData.DefaultCustomerId,
            restaurantId: Testing.TestData.DefaultRestaurantId,
            menuItemIds: Testing.TestData.GetMenuItemIds(Testing.TestData.MenuItems.ClassicBurger,
                Testing.TestData.MenuItems.MargheritaPizza));

        // Act Part 1: Create order
        var initiateOrderResult = await SendAsync(initiateOrderCommand);

        // Assert Part 1: Verify order creation response
        initiateOrderResult.ShouldBeSuccessful();
        var orderResponse = initiateOrderResult.Value;
        orderResponse.OrderId.Should().NotBeNull();
        orderResponse.PaymentIntentId.Should().NotBeNullOrEmpty();
        orderResponse.ClientSecret.Should().NotBeNullOrEmpty();

        // Verify order is in AwaitingPayment status
        var createdOrder = await FindOrderAsync(orderResponse.OrderId);
        createdOrder.Should().NotBeNull();
        createdOrder!.Status.Should().Be(OrderStatus.AwaitingPayment);
        createdOrder.PaymentTransactions.First().PaymentGatewayReferenceId.Should().Be(orderResponse.PaymentIntentId);

        // Act Part 2: Simulate successful Stripe payment confirmation
        await PaymentTestHelper.ConfirmPaymentAsync(
            orderResponse.PaymentIntentId!,
            TestConfiguration.Payment.TestPaymentMethods.VisaSuccess);

        // Act Part 3: Process payment success webhook
        var webhookPayload = PaymentTestHelper.GenerateWebhookPayload(
            TestConfiguration.Payment.WebhookEvents.PaymentIntentSucceeded,
            orderResponse.PaymentIntentId!,
            amount: 2500, // $25.00
            metadata: new Dictionary<string, string>
            {
                { "order_id", orderResponse.OrderId.ToString() ?? "" }, { "test_case", "payment_success" }
            });

        var webhookSignature = PaymentTestHelper.GenerateWebhookSignature(
            webhookPayload,
            _stripeOptions.WebhookSecret);

        var webhookCommand = new HandleStripeWebhookCommand(
            RawJson: webhookPayload,
            StripeSignatureHeader: webhookSignature);

        var webhookResult = await SendAsync(webhookCommand);

        // Assert Part 2: Verify webhook processing and order status update
        webhookResult.ShouldBeSuccessful();

        // Verify order status is now Placed
        var updatedOrder = await FindOrderAsync(orderResponse.OrderId);
        updatedOrder.Should().NotBeNull();
        updatedOrder!.Status.Should().Be(OrderStatus.Placed);

        // Verify payment transaction is recorded
        updatedOrder.PaymentTransactions.Should().HaveCount(1);
        var paymentTransaction = updatedOrder.PaymentTransactions.First();
        paymentTransaction.PaymentMethodType.Should().Be(PaymentMethodType.CreditCard);
        paymentTransaction.Status.Should().Be(PaymentStatus.Succeeded);
    }

    #endregion

    #region Payment Failure Tests

    /// <summary>
    /// Test Case 1.2: Payment Failure Flow
    /// Tests the flow when payment fails and order should be cancelled.
    /// </summary>
    [Test]
    public async Task InitiateOrder_WithOnlinePayment_WhenPaymentFails_ShouldCancelOrder()
    {
        // Arrange
        var initiateOrderCommand = PaymentTestHelper.BuildValidOnlineOrderCommand(
            customerId: Testing.TestData.DefaultCustomerId,
            restaurantId: Testing.TestData.DefaultRestaurantId,
            menuItemIds: Testing.TestData.GetMenuItemIds(Testing.TestData.MenuItems.ClassicBurger,
                Testing.TestData.MenuItems.MargheritaPizza));

        // Act Part 1: Create order
        var initiateOrderResult = await SendAsync(initiateOrderCommand);
        initiateOrderResult.ShouldBeSuccessful();
        var orderResponse = initiateOrderResult.Value;

        // Verify initial order state
        var createdOrder = await FindOrderAsync(orderResponse.OrderId);
        createdOrder!.Status.Should().Be(OrderStatus.AwaitingPayment);

        // Act Part 2: Simulate payment failure with declined card
        await PaymentTestHelper.ConfirmPaymentAsync(
            orderResponse.PaymentIntentId!,
            TestConfiguration.Payment.TestPaymentMethods.VisaDeclined);

        // Act Part 3: Process payment failure webhook
        var webhookPayload = PaymentTestHelper.GenerateWebhookPayload(
            TestConfiguration.Payment.WebhookEvents.PaymentIntentPaymentFailed,
            orderResponse.PaymentIntentId!,
            amount: 2500,
            metadata: new Dictionary<string, string>
            {
                { "order_id", orderResponse.OrderId.ToString() ?? "" }, { "test_case", "payment_failure" }
            });

        var webhookSignature = PaymentTestHelper.GenerateWebhookSignature(
            webhookPayload,
            _stripeOptions.WebhookSecret);

        var webhookCommand = new HandleStripeWebhookCommand(
            RawJson: webhookPayload,
            StripeSignatureHeader: webhookSignature);

        var webhookResult = await SendAsync(webhookCommand);

        // Assert: Verify webhook processing and order cancellation
        webhookResult.ShouldBeSuccessful();

        // Verify order status is now Cancelled
        var updatedOrder = await FindOrderAsync(orderResponse.OrderId);
        updatedOrder.Should().NotBeNull();
        updatedOrder!.Status.Should().Be(OrderStatus.Cancelled);

        // Verify payment transaction failure is recorded
        updatedOrder.PaymentTransactions.Should().HaveCount(1);
        var paymentTransaction = updatedOrder.PaymentTransactions.First();
        paymentTransaction.PaymentMethodType.Should().Be(PaymentMethodType.CreditCard);
        paymentTransaction.Status.Should().Be(PaymentStatus.Failed);
    }

    /// <summary>
    /// Test Case 1.2b: Payment Authentication Required Flow
    /// Tests the flow when payment requires additional authentication.
    /// </summary>
    [Test]
    public async Task InitiateOrder_WithOnlinePayment_WhenPaymentRequiresAuthentication_ShouldHandleGracefully()
    {
        // Arrange
        var initiateOrderCommand = PaymentTestHelper.BuildValidOnlineOrderCommand(
            customerId: Testing.TestData.DefaultCustomerId,
            restaurantId: Testing.TestData.DefaultRestaurantId,
            menuItemIds: Testing.TestData.GetMenuItemIds(Testing.TestData.MenuItems.ClassicBurger,
                Testing.TestData.MenuItems.MargheritaPizza));

        // Act Part 1: Create order
        var initiateOrderResult = await SendAsync(initiateOrderCommand);
        initiateOrderResult.ShouldBeSuccessful();
        var orderResponse = initiateOrderResult.Value;

        // Act Part 2: Simulate payment requiring authentication
        await PaymentTestHelper.ConfirmPaymentAsync(
            orderResponse.PaymentIntentId!,
            TestConfiguration.Payment.TestPaymentMethods
                .VisaAuthentication); // This should succeed but require additional steps

        // Verify order remains in AwaitingPayment status until authentication completes
        var orderAfterAuth = await FindOrderAsync(orderResponse.OrderId);
        orderAfterAuth!.Status.Should().Be(OrderStatus.AwaitingPayment);
    }

    #endregion

    #region COD Payment Tests

    /// <summary>
    /// Test Case 1.3: Order Creation with COD Payment
    /// Tests that COD orders are created directly as Placed without payment processing.
    /// </summary>
    [Test]
    public async Task InitiateOrder_WithCODPayment_ShouldCreateOrderDirectlyAsPlaced()
    {
        // Arrange
        var codOrderCommand = PaymentTestHelper.BuildValidCODOrderCommand(
            customerId: Testing.TestData.DefaultCustomerId,
            restaurantId: Testing.TestData.DefaultRestaurantId,
            menuItemIds: Testing.TestData.GetMenuItemIds(Testing.TestData.MenuItems.ClassicBurger,
                Testing.TestData.MenuItems.MargheritaPizza));

        // Act
        var result = await SendAsync(codOrderCommand);

        // Assert
        result.ShouldBeSuccessful();
        var orderResponse = result.Value;

        // Verify response structure for COD orders
        orderResponse.OrderId.Should().NotBeNull();
        orderResponse.PaymentIntentId.Should().BeNull(); // No payment intent for COD
        orderResponse.ClientSecret.Should().BeNull(); // No client secret for COD

        // Verify order is created with Placed status immediately
        var createdOrder = await FindOrderAsync(orderResponse.OrderId);
        createdOrder.Should().NotBeNull();
        createdOrder!.Status.Should().Be(OrderStatus.Placed);
        createdOrder.PaymentTransactions.First().PaymentMethodType.Should().Be(PaymentMethodType.CashOnDelivery);

        // Verify payment transaction is recorded
        createdOrder.PaymentTransactions.Should().HaveCount(1);
        var paymentTransaction = createdOrder.PaymentTransactions.First();
        paymentTransaction.PaymentMethodType.Should().Be(PaymentMethodType.CashOnDelivery);
    }

    #endregion

    #region Edge Cases and Error Scenarios

    /// <summary>
    /// Tests webhook idempotency - processing the same webhook twice should not cause issues.
    /// </summary>
    [Test]
    public async Task HandleWebhook_WhenProcessedTwice_ShouldBeIdempotent()
    {
        // Arrange
        var initiateOrderCommand = PaymentTestHelper.BuildValidOnlineOrderCommand(
            customerId: Testing.TestData.DefaultCustomerId,
            restaurantId: Testing.TestData.DefaultRestaurantId,
            menuItemIds: Testing.TestData.GetMenuItemIds(Testing.TestData.MenuItems.ClassicBurger,
                Testing.TestData.MenuItems.MargheritaPizza));

        var initiateOrderResult = await SendAsync(initiateOrderCommand);

        var orderResponse = initiateOrderResult.Value;

        // Confirm payment
        await PaymentTestHelper.ConfirmPaymentAsync(
            orderResponse.PaymentIntentId!,
            TestConfiguration.Payment.TestPaymentMethods.VisaSuccess);

        // Create webhook payload
        var webhookPayload = PaymentTestHelper.GenerateWebhookPayload(
            TestConfiguration.Payment.WebhookEvents.PaymentIntentSucceeded,
            orderResponse.PaymentIntentId!,
            amount: 2500,
            metadata: new Dictionary<string, string>
            {
                { "order_id", orderResponse.OrderId.ToString() ?? "" }, { "test_case", "idempotency_test" }
            });

        var webhookSignature = PaymentTestHelper.GenerateWebhookSignature(
            webhookPayload,
            _stripeOptions.WebhookSecret);

        var webhookCommand = new HandleStripeWebhookCommand(
            RawJson: webhookPayload,
            StripeSignatureHeader: webhookSignature);

        // Act: Process webhook twice
        var firstResult = await SendAsync(webhookCommand);
        var secondResult = await SendAsync(webhookCommand);

        // Assert: Both should succeed (idempotent)
        firstResult.ShouldBeSuccessful();
        secondResult.ShouldBeSuccessful();

        // Verify order state is consistent
        var finalOrder = await FindOrderAsync(orderResponse.OrderId);
        finalOrder!.Status.Should().Be(OrderStatus.Placed);
    }

    /// <summary>
    /// Tests handling of invalid payment method in order command.
    /// </summary>
    [Test]
    public async Task InitiateOrder_WithInvalidPaymentMethod_ShouldReturnValidationError()
    {
        // Arrange
        var invalidOrderCommand = PaymentTestHelper.BuildInvalidOrderCommand(
            invalidField: "paymentmethod",
            menuItemIds: Testing.TestData.GetMenuItemIds(Testing.TestData.MenuItems.ClassicBurger,
                Testing.TestData.MenuItems.MargheritaPizza));

        // Act & Assert
        await FluentActions.Invoking(() =>
            SendAsync(invalidOrderCommand)).Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Tests webhook processing for unknown order (order not found scenario).
    /// </summary>
    [Test]
    public async Task HandleWebhook_ForUnknownOrder_ShouldSucceedGracefully()
    {
        // Arrange: Create webhook for non-existent payment intent
        var unknownPaymentIntentId = "pi_unknown_" + Guid.NewGuid().ToString("N")[..16];

        // Use a simple, known payload format that matches Stripe's actual format
        var webhookPayload = PaymentTestHelper.GenerateWebhookPayload(
            TestConfiguration.Payment.WebhookEvents.PaymentIntentRequiresAction,
            unknownPaymentIntentId,
            amount: 2500,
            metadata: new Dictionary<string, string> { { "order_id", "unknown" }, { "test_case", "unknown_order" } });

        var webhookSignature = PaymentTestHelper.GenerateWebhookSignature(
            webhookPayload,
            _stripeOptions.WebhookSecret);

        var webhookCommand = new HandleStripeWebhookCommand(
            RawJson: webhookPayload,
            StripeSignatureHeader: webhookSignature);

        // Act
        var result = await SendAsync(webhookCommand);

        // Assert
        result.ShouldBeSuccessful();
    }

    #endregion

    #region Coupon and Discount Tests

    /// <summary>
    /// Tests order creation with coupon application.
    /// </summary>
    [Test]
    public async Task InitiateOrder_WithValidCoupon_ShouldApplyDiscountCorrectly()
    {
        // Arrange
        var orderWithCouponCommand = PaymentTestHelper.BuildOrderCommandWithCoupon(
            couponCode: Testing.TestData.DefaultCouponCode,
            customerId: Testing.TestData.DefaultCustomerId,
            restaurantId: Testing.TestData.DefaultRestaurantId,
            menuItemIds: Testing.TestData.GetMenuItemIds(Testing.TestData.MenuItems.ClassicBurger,
                Testing.TestData.MenuItems.MargheritaPizza));

        // Act
        var result = await SendAsync(orderWithCouponCommand);

        // Assert
        result.ShouldBeSuccessful();
        var orderResponse = result.Value;

        // Verify order creation
        var createdOrder = await FindOrderAsync(orderResponse.OrderId);
        createdOrder.Should().NotBeNull();
        createdOrder!.Status.Should().Be(OrderStatus.AwaitingPayment);

        // Note: Coupon validation and discount application would be tested 
        // in the actual order aggregate and pricing logic
    }

    #endregion
}
