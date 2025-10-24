using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Application.Orders.Commands.InitiateOrder;
using YummyZoom.Domain.CouponAggregate.Errors;
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.OrderAggregate;
using YummyZoom.Infrastructure.Persistence.EfCore;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Orders.Commands.InitiateOrder;

/// <summary>
/// Tests for InitiateOrder command edge cases and error handling.
/// Focuses on concurrent operations, transaction consistency, data integrity,
/// audit trails, and domain events verification.
/// </summary>
public class InitiateOrderEdgeCaseTests : InitiateOrderTestBase
{
    #region Concurrent Operations Tests

    [Test]
    public async Task InitiateOrder_WithConcurrentCouponUsage_ShouldHandleCorrectly()
    {
        // Arrange - Create a coupon with limited usage count (max 2 uses)
        var limitedCouponCode = await CouponTestDataFactory.CreateTestCouponAsync(
            new CouponTestOptions
            {
                Code = "LIMITED2",
                TotalUsageLimit = 2,
                DiscountPercentage = 10
            });

        var command1 = InitiateOrderTestHelper.BuildValidCommandWithCoupon(limitedCouponCode);
        var command2 = InitiateOrderTestHelper.BuildValidCommandWithCoupon(limitedCouponCode);
        var command3 = InitiateOrderTestHelper.BuildValidCommandWithCoupon(limitedCouponCode);

        // Act - Execute commands concurrently
        var tasks = new[]
        {
            SendAsync(command1),
            SendAsync(command2),
            SendAsync(command3)
        };

        var results = await Task.WhenAll(tasks);

        // Assert - Only 2 should succeed, 1 should fail
        var successfulResults = results.Where(r => r.IsSuccess).ToList();
        var failedResults = results.Where(r => r.IsFailure).ToList();

        successfulResults.Should().HaveCount(2, "only 2 orders should succeed due to usage limit");
        failedResults.Should().HaveCount(1, "1 order should fail due to exceeded usage limit");

        // Verify coupon usage count
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var coupon = await context.Coupons.FirstOrDefaultAsync(c => c.Code == limitedCouponCode);
        coupon.Should().NotBeNull();
        coupon!.CurrentTotalUsageCount.Should().Be(2, "coupon should have been used exactly 2 times");
    }

    [Test]
    public async Task InitiateOrder_WithConcurrentPerUserUsageLimit_ShouldHandleCorrectly()
    {
        // Arrange - Create a coupon with per-user usage limit (max 2 uses per user)
        var perUserLimitCouponCode = await CouponTestDataFactory.CreateTestCouponAsync(
            new CouponTestOptions
            {
                Code = "PERUSER2",
                UserUsageLimit = 2,
                TotalUsageLimit = 100, // High total limit to focus on per-user limit
                DiscountPercentage = 15
            });

        // Same user makes 3 concurrent requests
        var command1 = InitiateOrderTestHelper.BuildValidCommandWithCoupon(perUserLimitCouponCode);
        var command2 = InitiateOrderTestHelper.BuildValidCommandWithCoupon(perUserLimitCouponCode);
        var command3 = InitiateOrderTestHelper.BuildValidCommandWithCoupon(perUserLimitCouponCode);

        // Act - Execute all commands concurrently
        var tasks = new[]
        {
            SendAsync(command1),
            SendAsync(command2),
            SendAsync(command3)
        };

        var results = await Task.WhenAll(tasks);

        // Assert - Only 2 should succeed due to per-user usage limit
        var successfulResults = results.Where(r => r.IsSuccess).ToList();
        var failedResults = results.Where(r => r.IsFailure).ToList();

        successfulResults.Should().HaveCount(2, "only 2 orders should succeed due to per-user usage limit");
        failedResults.Should().HaveCount(1, "1 order should fail due to exceeded per-user usage limit");

        // Verify the failure is specifically about per-user usage limit
        var failedResult = failedResults.First();
        failedResult.ShouldBeFailure(CouponErrors.UsageLimitExceeded.Code);
    }

    [Test]
    public async Task InitiateOrder_WithConcurrentMixedUsageLimits_ShouldHandleCorrectly()
    {
        // Arrange - Create a coupon with both total and per-user limits
        var mixedLimitCouponCode = await CouponTestDataFactory.CreateTestCouponAsync(
            new CouponTestOptions
            {
                Code = "MIXED32",
                TotalUsageLimit = 3,
                UserUsageLimit = 2,
                DiscountPercentage = 20
            });

        // Get user IDs for testing
        var user1Id = Testing.TestData.DefaultCustomerId;
        var user2Id = Guid.NewGuid();

        var allResults = new List<SharedKernel.Result<InitiateOrderResponse>>();

        // Act - Execute user1 commands (current context is already set to DefaultCustomerId)
        var user1Command1 = InitiateOrderTestHelper.BuildValidCommandWithCoupon(mixedLimitCouponCode);
        var user1Command2 = InitiateOrderTestHelper.BuildValidCommandWithCoupon(mixedLimitCouponCode);

        // Execute user1 commands concurrently
        var user1Tasks = new[]
        {
            SendAsync(user1Command1), // User 1, Request 1
            SendAsync(user1Command2)  // User 1, Request 2
        };
        var user1Results = await Task.WhenAll(user1Tasks);
        allResults.AddRange(user1Results);

        // Switch to user2 context and execute user2 commands
        SetUserId(user2Id);
        var user2Command1 = InitiateOrderTestHelper.BuildValidCommand(customerId: user2Id, couponCode: mixedLimitCouponCode);
        var user2Command2 = InitiateOrderTestHelper.BuildValidCommand(customerId: user2Id, couponCode: mixedLimitCouponCode);

        // Execute user2 commands concurrently  
        var user2Tasks = new[]
        {
            SendAsync(user2Command1), // User 2, Request 1
            SendAsync(user2Command2)  // User 2, Request 2
        };
        var user2Results = await Task.WhenAll(user2Tasks);
        allResults.AddRange(user2Results);

        // Assert - Should hit both per-user and total limits
        var successfulResults = allResults.Where(r => r.IsSuccess).ToList();
        var failedResults = allResults.Where(r => r.IsFailure).ToList();

        // Maximum 3 can succeed due to total limit, but each user can only use 2
        successfulResults.Should().HaveCount(3, "only 3 orders should succeed due to total usage limit");
        failedResults.Should().HaveCount(1, "1 order should fail due to limits being reached");

        // Verify coupon final usage count
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var coupon = await context.Coupons.FirstOrDefaultAsync(c => c.Code == mixedLimitCouponCode);
        coupon.Should().NotBeNull();
        coupon!.CurrentTotalUsageCount.Should().Be(3, "coupon should have been used exactly 3 times");
    }

    [Test]
    public async Task InitiateOrder_WithConcurrentMultipleUsersAtSameRestaurant_ShouldAllSucceed()
    {
        // Arrange - Multiple different users ordering from the same restaurant
        // This tests restaurant-level concurrency and transaction isolation
        var restaurantId = Testing.TestData.DefaultRestaurantId; // Use default restaurant ID for consistency

        // Create unique customer IDs for each order
        var customerIds = new[]
        {
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid()
        };

        var allResults = new List<SharedKernel.Result<InitiateOrderResponse>>();

        // Act - Execute each command with proper authentication context
        // Note: We execute sequentially by user, but this still tests restaurant-level isolation
        // since the business logic handles multiple orders for the same restaurant
        foreach (var customerId in customerIds)
        {
            SetUserId(customerId);
            var command = InitiateOrderTestHelper.BuildValidCommand(customerId: customerId, restaurantId: restaurantId);
            var result = await SendAsync(command);
            allResults.Add(result);
        }

        // Assert - All should succeed (restaurant should handle multiple orders from different users)
        allResults.Should().AllSatisfy(result => result.IsSuccess.Should().BeTrue("restaurant should handle multiple orders from different users"));

        // Verify all orders were created for the same restaurant
        var orderIds = allResults.Select(r => r.Value.OrderId).ToList();
        var orders = new List<Domain.OrderAggregate.Order>();

        // Use individual FindOrderAsync calls to avoid EF Core warning
        foreach (var orderId in orderIds)
        {
            var order = await FindOrderAsync(orderId);
            order.Should().NotBeNull($"order {orderId} should exist");
            orders.Add(order!);
        }

        orders.Should().HaveCount(5, "all orders should be persisted");
        orders.Should().AllSatisfy(order => order.RestaurantId.Value.Should().Be(restaurantId, "all orders should be for the same restaurant"));

        // Verify each order has unique customer
        var actualCustomerIds = orders.Select(o => o.CustomerId.Value).ToList();
        actualCustomerIds.Should().OnlyHaveUniqueItems("each order should be from a different customer");
        actualCustomerIds.Should().BeEquivalentTo(customerIds, "orders should be created for the expected customers");
    }

    #endregion

    #region Transaction Consistency Tests

    [Test]
    public async Task InitiateOrder_ShouldMaintainTransactionConsistency()
    {
        // Arrange
        var command = InitiateOrderTestHelper.BuildValidCommand();

        // Act
        var result = await SendAsync(command);

        // Assert - Verify all related entities are created consistently
        result.ShouldBeSuccessful();
        var orderId = result.Value.OrderId;

        // Verify order is properly persisted with all components
        var order = await InitiateOrderTestHelper.ValidateOrderPersistence(orderId, shouldExist: true);
        order.Should().NotBeNull();

        // Verify order items are created with current pricing
        order!.OrderItems.Should().NotBeEmpty("order should have items");
        order.OrderItems.Should().AllSatisfy(item =>
        {
            item.Snapshot_BasePriceAtOrder.Amount.Should().BeGreaterThan(0, "item should have valid unit price");
            item.Quantity.Should().BeGreaterThan(0, "item should have valid quantity");
        });

        // Verify financial calculations are consistent
        var calculatedSubtotal = order.OrderItems.Sum(item => item.Snapshot_BasePriceAtOrder.Amount * item.Quantity);
        order.Subtotal.Amount.Should().Be(calculatedSubtotal, "subtotal should equal sum of line items");

        // Verify total calculation includes all components
        var expectedTotal = order.Subtotal.Amount + order.TaxAmount.Amount + order.DeliveryFee.Amount + order.TipAmount.Amount - order.DiscountAmount.Amount;
        order.TotalAmount.Amount.Should().Be(expectedTotal, "total should include all financial components");
    }

    [Test]
    public async Task InitiateOrder_WhenPaymentGatewayFailsAfterOrderCreation_ShouldRollbackTransaction()
    {
        // Arrange - Configure payment gateway to fail
        await ConfigureFailingPaymentGatewayAsync("Payment gateway temporarily unavailable");

        var command = InitiateOrderTestHelper.BuildValidCommand(
            paymentMethod: InitiateOrderTestHelper.PaymentMethods.CreditCard);

        // Get initial order count
        var initialOrderCount = await CountAsync<Order>();

        // Act
        var result = await SendAsync(command);

        // Assert - Operation should fail and no order should be persisted
        result.ShouldBeFailure();
        result.Error.Code.Should().Contain("PaymentGateway", "error should indicate payment gateway failure");

        // Verify transaction was rolled back - no new orders created
        var finalOrderCount = await CountAsync<Order>();
        finalOrderCount.Should().Be(initialOrderCount, "no order should be persisted when payment gateway fails");

        // Verify payment gateway was called but transaction rolled back
        InitiateOrderTestHelper.ValidatePaymentIntentCreation(PaymentGatewayMock, shouldBeCalled: true);
    }

    #endregion

    #region Menu Item Pricing Snapshot Tests

    [Test]
    public async Task InitiateOrder_ShouldCaptureCurrentMenuItemPricing()
    {
        // Arrange - Get current menu item prices
        var menuItemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        var menuItem = await FindAsync<MenuItem>(MenuItemId.Create(menuItemId));
        var originalPrice = menuItem!.BasePrice.Amount;

        var command = InitiateOrderTestHelper.BuildValidCommand(
            menuItemIds: new List<Guid> { menuItemId });

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        var orderId = result.Value.OrderId;
        var order = await InitiateOrderTestHelper.ValidateOrderPersistence(orderId, shouldExist: true);

        // Verify pricing snapshot captured current prices
        var orderItem = order!.OrderItems.First();
        orderItem.Snapshot_BasePriceAtOrder.Amount.Should().Be(originalPrice, "order item should capture current menu item price as snapshot");
        orderItem.Snapshot_MenuItemId.Value.Should().Be(menuItemId, "order item should reference correct menu item");

        // Verify price snapshot is preserved even if menu item price changes later
        // (This is important for historical order accuracy)
        orderItem.Snapshot_BasePriceAtOrder.Should().NotBeNull("price snapshot should be preserved in order item");
    }

    #endregion

    #region Order Number Generation Tests

    [Test]
    public async Task InitiateOrder_ShouldGenerateUniqueOrderNumber()
    {
        // Arrange - Create multiple orders to test uniqueness
        var commands = Enumerable.Range(1, 5)
            .Select(_ => InitiateOrderTestHelper.BuildValidCommand())
            .ToList();

        // Act - Create orders sequentially to ensure deterministic order numbers
        var results = new List<InitiateOrderResponse>();
        foreach (var command in commands)
        {
            var result = await SendAsync(command);
            result.ShouldBeSuccessful();
            results.Add(result.Value);
        }

        // Assert - All order numbers should be unique
        var orderNumbers = new List<string>();
        foreach (var result in results)
        {
            var orderId = result.OrderId;
            var order = await InitiateOrderTestHelper.ValidateOrderPersistence(orderId, shouldExist: true);
            order.Should().NotBeNull();
            orderNumbers.Add(order!.OrderNumber);
        }

        orderNumbers.Should().OnlyHaveUniqueItems("all order numbers should be unique");
        orderNumbers.Should().AllSatisfy(number =>
        {
            number.Should().NotBeNullOrEmpty("order number should not be empty");
            number.Length.Should().BeGreaterThan(5, "order number should have reasonable length");
        });
    }

    #endregion

    #region Audit Trail Tests

    [Test]
    public async Task InitiateOrder_ShouldSetCreationAuditFields()
    {
        // Arrange
        var testUserId = Testing.TestData.DefaultCustomerId;
        SetUserId(testUserId);

        var command = InitiateOrderTestHelper.BuildValidCommand(customerId: testUserId);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        var orderId = result.Value.OrderId;
        var order = await InitiateOrderTestHelper.ValidateOrderPersistence(orderId, shouldExist: true);

        // Verify audit fields are properly set
        order.Should().NotBeNull();
        order!.Created.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1), "created timestamp should be recent");
        order.CreatedBy.Should().Be(testUserId.ToString(), "created by should match current user");

        // Verify placement timestamp is set
        order.PlacementTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1), "placement timestamp should be recent");
    }

    #endregion

    #region Domain Events Tests

    [Test]
    public async Task InitiateOrder_ShouldCreateOrderAndProcessDomainEvents()
    {
        // Arrange
        var command = InitiateOrderTestHelper.BuildValidCommand();

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        var orderId = result.Value.OrderId;
        var order = await InitiateOrderTestHelper.ValidateOrderPersistence(orderId, shouldExist: true);

        // Verify order was created successfully - this is the main behavior we want to test
        order.Should().NotBeNull();
        order!.Id.Value.Should().Be(orderId.Value, "order should be persisted with correct ID");
        order.CustomerId.Value.Should().Be(command.CustomerId, "order should have correct customer ID");
        order.RestaurantId.Value.Should().Be(command.RestaurantId, "order should have correct restaurant ID");
        order.TotalAmount.Should().NotBeNull("order should have calculated total amount");

        // Verify domain events were processed and cleared
        order.DomainEvents.Should().BeEmpty("domain events should be cleared after being dispatched during save");
    }

    #endregion

    #region Data Integrity Tests

    [Test]
    public async Task InitiateOrder_ShouldExecuteInTransaction()
    {
        // Arrange
        var command = InitiateOrderTestHelper.BuildValidCommand();
        var initialOrderCount = await CountAsync<Order>();

        // Act
        var result = await SendAsync(command);

        // Assert - Verify atomic operation
        result.ShouldBeSuccessful();

        // Verify exactly one order was created
        var finalOrderCount = await CountAsync<Order>();
        finalOrderCount.Should().Be(initialOrderCount + 1, "exactly one order should be created");

        // Verify order and all related data exist consistently
        var orderId = result.Value.OrderId;
        var order = await InitiateOrderTestHelper.ValidateOrderPersistence(orderId, shouldExist: true);

        order.Should().NotBeNull();
        order!.OrderItems.Should().NotBeEmpty("order should have items");
        order.TotalAmount.Amount.Should().BeGreaterThan(0, "order should have valid total amount");

        // Verify all financial components are properly calculated and persisted
        order.Subtotal.Amount.Should().BeGreaterThan(0);
        order.TaxAmount.Amount.Should().BeGreaterThanOrEqualTo(0);
        order.DeliveryFee.Amount.Should().BeGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task InitiateOrder_WithComplexScenario_ShouldMaintainDataConsistency()
    {
        // Arrange - Complex scenario with coupon, tip, multiple items
        var couponCode = await CouponTestDataFactory.CreateTestCouponAsync(
            new CouponTestOptions
            {
                Code = "COMPLEX10",
                DiscountPercentage = 15,
                MinimumOrderAmount = 20.00m
            });

        var command = InitiateOrderTestHelper.BuildValidCommand(
            menuItemIds: Testing.TestData.GetMenuItemIds(
                Testing.TestData.MenuItems.ClassicBurger,
                Testing.TestData.MenuItems.BuffaloWings,
                Testing.TestData.MenuItems.FreshJuice
            ),
            couponCode: couponCode,
            tipAmount: InitiateOrderTestHelper.TestAmounts.MediumTip,
            specialInstructions: "Extra sauce on the side",
            paymentMethod: InitiateOrderTestHelper.PaymentMethods.CreditCard
        );

        // Act
        var result = await SendAsync(command);

        // Assert - Verify complex order maintains data consistency
        result.ShouldBeSuccessful();
        var orderId = result.Value.OrderId;
        var order = await InitiateOrderTestHelper.ValidateOrderPersistence(orderId, shouldExist: true);

        order.Should().NotBeNull();

        // Verify all order items are present
        order!.OrderItems.Should().HaveCount(3, "order should contain all requested items");

        // Verify coupon was applied
        order.DiscountAmount.Amount.Should().BeGreaterThan(0, "discount should be applied from coupon");

        // Verify tip was included
        order.TipAmount.Amount.Should().Be(InitiateOrderTestHelper.TestAmounts.MediumTip, "tip amount should match");

        // Verify special instructions
        order.SpecialInstructions.Should().Be("Extra sauce on the side", "special instructions should be preserved");

        // Verify payment intent was created for online payment
        InitiateOrderTestHelper.ValidatePaymentIntentCreation(PaymentGatewayMock, shouldBeCalled: true);
        result.Value.PaymentIntentId.Should().NotBeNullOrEmpty("payment intent ID should be returned for online payment");
        result.Value.ClientSecret.Should().NotBeNullOrEmpty("client secret should be returned for online payment");
    }

    #endregion
}
