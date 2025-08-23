using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Features.Orders.Commands.InitiateOrder;
using YummyZoom.Application.FunctionalTests.Features.Orders.Commands.Lifecycle;
using YummyZoom.Application.Orders.Broadcasting;
using YummyZoom.Application.Orders.Commands.AcceptOrder;
using YummyZoom.Application.Orders.Commands.MarkOrderDelivered;
using YummyZoom.Application.Orders.Commands.MarkOrderPreparing;
using YummyZoom.Application.Orders.Commands.MarkOrderReadyForDelivery;
using YummyZoom.Application.Orders.EventHandlers;
using YummyZoom.Domain.OrderAggregate.Events;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAccountAggregate.Events;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Infrastructure.Data;
using YummyZoom.Infrastructure.Data.Models;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Orders.Events;

public class OrderDeliveredEventHandlerTests : BaseTestFixture
{
    [SetUp]
    public async Task SetUpUserAndPaymentGateway()
    {
        SetUserId(Testing.TestData.DefaultCustomerId);
        var paymentGatewayMock = InitiateOrderTestHelper.SetupSuccessfulPaymentGatewayMock();
        ReplaceService<IPaymentGatewayService>(paymentGatewayMock.Object);
        await Task.CompletedTask;
    }

    [Test]
    public async Task OrderDelivered_Should_RecordRevenue_And_BroadcastStatus()
    {
        // Arrange: setup mocks
        var notifierMock = new Mock<IOrderRealtimeNotifier>(MockBehavior.Strict);
        OrderStatusBroadcastDto? deliveredDto = null;
        
        notifierMock.Setup(n => n.NotifyOrderPaymentSucceeded(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notifierMock.Setup(n => n.NotifyOrderPlaced(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notifierMock.Setup(n => n.NotifyOrderPaymentFailed(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notifierMock.Setup(n => n.NotifyOrderStatusChanged(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<CancellationToken>()))
            .Callback<OrderStatusBroadcastDto, CancellationToken>((dto, _) => deliveredDto = dto)
            .Returns(Task.CompletedTask);
        
        ReplaceService<IOrderRealtimeNotifier>(notifierMock.Object);

        // Create and complete order through delivery
        var cmd = InitiateOrderTestHelper.BuildValidCommand(paymentMethod: InitiateOrderTestHelper.PaymentMethods.CreditCard);
        var initiateResponse = await SendAndUnwrapAsync(cmd);

        // Process payment success
        await SimulatePaymentSuccessAsync(initiateResponse.OrderId);

        // Switch to restaurant staff context for restaurant operations
        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();

        // Accept order
        var acceptCommand = new AcceptOrderCommand(
            initiateResponse.OrderId.Value, Testing.TestData.DefaultRestaurantId, DateTime.UtcNow.AddHours(1));
        await SendAndUnwrapAsync(acceptCommand);

        // Mark as preparing
        var preparingCommand = new MarkOrderPreparingCommand(
            initiateResponse.OrderId.Value, Testing.TestData.DefaultRestaurantId);
        await SendAndUnwrapAsync(preparingCommand);

        // Mark as ready for delivery
        var readyCommand = new MarkOrderReadyForDeliveryCommand(
            initiateResponse.OrderId.Value, Testing.TestData.DefaultRestaurantId);
        await SendAndUnwrapAsync(readyCommand);

        // Get initial restaurant account balance
        decimal initialBalance = 0;
        using (var scope = CreateScope())
        {
            var restaurantAccountRepo = scope.ServiceProvider.GetRequiredService<IRestaurantAccountRepository>();
            var restaurantId = RestaurantId.Create(Testing.TestData.DefaultRestaurantId);
            var account = await restaurantAccountRepo.GetByRestaurantIdAsync(restaurantId, CancellationToken.None);
            initialBalance = account?.CurrentBalance.Amount ?? 0;
        }

        // Mark order as delivered
        var deliveredAt = DateTime.UtcNow;
        var deliveredCommand = new MarkOrderDeliveredCommand(
            initiateResponse.OrderId.Value, Testing.TestData.DefaultRestaurantId, deliveredAt);
        var deliveredResponse = await SendAndUnwrapAsync(deliveredCommand);

        // Verify outbox has OrderDelivered event
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var pending = await db.Set<OutboxMessage>()
                .Where(m => m.Type.Contains(nameof(OrderDelivered)) && m.ProcessedOnUtc == null)
                .ToListAsync();
            pending.Should().NotBeEmpty("OrderDelivered event should be in outbox");
        }

        // Drain outbox to trigger event handler
        await DrainOutboxAsync();

        // Assert: verify revenue was recorded
        using (var scope = CreateScope())
        {
            var restaurantAccountRepo = scope.ServiceProvider.GetRequiredService<IRestaurantAccountRepository>();
            var restaurantId = RestaurantId.Create(Testing.TestData.DefaultRestaurantId);
            var account = await restaurantAccountRepo.GetByRestaurantIdAsync(restaurantId, CancellationToken.None);
            
            account.Should().NotBeNull("Restaurant account should exist");
            account!.CurrentBalance.Amount.Should().BeGreaterThan(initialBalance, "Revenue should have been recorded");
            
            // Verify the revenue amount matches order total
            var orderRepo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var order = await orderRepo.GetByIdAsync(initiateResponse.OrderId, CancellationToken.None);
            var expectedBalance = initialBalance + order!.TotalAmount.Amount;
            account.CurrentBalance.Amount.Should().Be(expectedBalance, "Revenue should match order total");
        }

        // Assert: verify RevenueRecorded event was emitted
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var revenueEvents = await db.Set<OutboxMessage>()
                .Where(m => m.Type.Contains(nameof(RevenueRecorded)))
                .ToListAsync();
            revenueEvents.Should().NotBeEmpty("RevenueRecorded event should be emitted");
        }

        // Assert: verify broadcast was called
        notifierMock.Verify(n => n.NotifyOrderStatusChanged(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        deliveredDto.Should().NotBeNull("Delivered broadcast should have been called");
        deliveredDto!.OrderId.Should().Be(initiateResponse.OrderId.Value);
        deliveredDto.Status.Should().Be("Delivered");
        deliveredDto.DeliveredAt.Should().BeCloseTo(deliveredAt, TimeSpan.FromSeconds(5));

        // Assert: verify inbox entry
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var handlerName = typeof(OrderDeliveredEventHandler).FullName!;
            var inboxEntries = await db.Set<InboxMessage>().Where(x => x.Handler == handlerName).ToListAsync();
            inboxEntries.Should().HaveCount(1, "Should have exactly one inbox entry for OrderDeliveredEventHandler");
        }
    }

    [Test]
    public async Task OrderDelivered_Should_AutoCreateRestaurantAccount_WhenNotExists()
    {
        // Arrange: Create order with a different restaurant that doesn't have an account
        var notifierMock = new Mock<IOrderRealtimeNotifier>(MockBehavior.Strict);
        notifierMock.Setup(n => n.NotifyOrderPaymentSucceeded(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notifierMock.Setup(n => n.NotifyOrderPlaced(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notifierMock.Setup(n => n.NotifyOrderPaymentFailed(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notifierMock.Setup(n => n.NotifyOrderStatusChanged(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        ReplaceService<IOrderRealtimeNotifier>(notifierMock.Object);

        // Use the existing default restaurant but ensure no account exists for it
        var restaurantId = RestaurantId.Create(Testing.TestData.DefaultRestaurantId);
        
        // First, ensure no restaurant account exists for the default restaurant
        using (var scope = CreateScope())
        {
            var restaurantAccountRepo = scope.ServiceProvider.GetRequiredService<IRestaurantAccountRepository>();
            var existingAccount = await restaurantAccountRepo.GetByRestaurantIdAsync(restaurantId, CancellationToken.None);
            if (existingAccount is not null)
            {
                // Remove any existing account to test auto-creation
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                dbContext.RestaurantAccounts.Remove(existingAccount);
                await dbContext.SaveChangesAsync();
            }
        }
        
        var cmd = InitiateOrderTestHelper.BuildValidCommand(
            paymentMethod: InitiateOrderTestHelper.PaymentMethods.CreditCard,
            restaurantId: restaurantId.Value);
        var initiateResponse = await SendAndUnwrapAsync(cmd);

        // Process through to delivery
        await SimulatePaymentSuccessAsync(initiateResponse.OrderId);
        
        // Switch to restaurant staff context for restaurant operations
        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();
        
        var acceptCommand = new AcceptOrderCommand(
            initiateResponse.OrderId.Value, restaurantId.Value, DateTime.UtcNow.AddHours(1));
        await SendAndUnwrapAsync(acceptCommand);

        var preparingCommand = new MarkOrderPreparingCommand(
            initiateResponse.OrderId.Value, restaurantId.Value);
        await SendAndUnwrapAsync(preparingCommand);

        var readyCommand = new MarkOrderReadyForDeliveryCommand(
            initiateResponse.OrderId.Value, restaurantId.Value);
        await SendAndUnwrapAsync(readyCommand);

        // Verify no restaurant account exists before delivery
        using (var scope = CreateScope())
        {
            var restaurantAccountRepo = scope.ServiceProvider.GetRequiredService<IRestaurantAccountRepository>();
            var account = await restaurantAccountRepo.GetByRestaurantIdAsync(restaurantId, CancellationToken.None);
            account.Should().BeNull("Restaurant account should not exist before delivery");
        }

        // Mark order as delivered
        var deliveredCommand = new MarkOrderDeliveredCommand(
            initiateResponse.OrderId.Value, restaurantId.Value, DateTime.UtcNow);
        await SendAndUnwrapAsync(deliveredCommand);

        // Drain outbox
        await DrainOutboxAsync();

        // Assert: restaurant account should be auto-created with correct balance
        using (var scope = CreateScope())
        {
            var restaurantAccountRepo = scope.ServiceProvider.GetRequiredService<IRestaurantAccountRepository>();
            var account = await restaurantAccountRepo.GetByRestaurantIdAsync(restaurantId, CancellationToken.None);
            
            account.Should().NotBeNull("Restaurant account should be auto-created");
            account!.RestaurantId.Should().Be(restaurantId);
            account.CurrentBalance.Amount.Should().BeGreaterThan(0, "Revenue should have been recorded to new account");
        }
    }

        [Test]
    public async Task DrainingTwice_Should_Not_Duplicate_Revenue_Recording()
    {
        // Arrange
        var notifierMock = new Mock<IOrderRealtimeNotifier>(MockBehavior.Loose);
        ReplaceService<IOrderRealtimeNotifier>(notifierMock.Object);

        // Create and complete order through delivery
        var cmd = InitiateOrderTestHelper.BuildValidCommand(paymentMethod: InitiateOrderTestHelper.PaymentMethods.CreditCard);
        var initiateResponse = await SendAndUnwrapAsync(cmd);

        await SimulatePaymentSuccessAsync(initiateResponse.OrderId);
        
        // Switch to restaurant staff context for restaurant operations
        await OrderLifecycleTestHelper.RunAsDefaultRestaurantStaffAsync();
        
        var acceptCommand = new AcceptOrderCommand(
            initiateResponse.OrderId.Value, Testing.TestData.DefaultRestaurantId, DateTime.UtcNow.AddHours(1));
        await SendAndUnwrapAsync(acceptCommand);

        var preparingCommand = new MarkOrderPreparingCommand(
            initiateResponse.OrderId.Value, Testing.TestData.DefaultRestaurantId);
        await SendAndUnwrapAsync(preparingCommand);

        var readyCommand = new MarkOrderReadyForDeliveryCommand(
            initiateResponse.OrderId.Value, Testing.TestData.DefaultRestaurantId);
        await SendAndUnwrapAsync(readyCommand);

        // Get initial balance
        decimal initialBalance = 0;
        using (var scope = CreateScope())
        {
            var restaurantAccountRepo = scope.ServiceProvider.GetRequiredService<IRestaurantAccountRepository>();
            var restaurantId = RestaurantId.Create(Testing.TestData.DefaultRestaurantId);
            var account = await restaurantAccountRepo.GetByRestaurantIdAsync(restaurantId, CancellationToken.None);
            initialBalance = account?.CurrentBalance.Amount ?? 0;
        }

        // Mark order as delivered
        var deliveredCommand = new MarkOrderDeliveredCommand(
            initiateResponse.OrderId.Value, Testing.TestData.DefaultRestaurantId, DateTime.UtcNow);
        await SendAndUnwrapAsync(deliveredCommand);

        // Drain outbox twice - this should not duplicate revenue recording
        await DrainOutboxAsync();
        await DrainOutboxAsync();

        // Assert: Check that revenue was only recorded once
        using (var scope = CreateScope())
        {
            var restaurantAccountRepo = scope.ServiceProvider.GetRequiredService<IRestaurantAccountRepository>();
            var restaurantId = RestaurantId.Create(Testing.TestData.DefaultRestaurantId);
            var account = await restaurantAccountRepo.GetByRestaurantIdAsync(restaurantId, CancellationToken.None);
            
            account.Should().NotBeNull("Restaurant account should exist after revenue recording");
            
            // The revenue should be recorded exactly once, regardless of how many times outbox is drained
            var expectedBalance = initialBalance + 34.29m; // Order total is $34.29 based on the logs
            account!.CurrentBalance.Amount.Should().Be(expectedBalance, 
                "Revenue should only be recorded once even after draining outbox twice");
        }
    }

    private async Task SimulatePaymentSuccessAsync(OrderId orderId)
    {
        using var scope = CreateScope();
        var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var order = await orderRepository.GetByIdAsync(orderId, CancellationToken.None);
        order.Should().NotBeNull("Order should exist after creation");
        
        var paymentTransaction = order!.PaymentTransactions.FirstOrDefault();
        paymentTransaction.Should().NotBeNull("Order should have a payment transaction");
        var paymentIntentId = paymentTransaction!.PaymentGatewayReferenceId;
        paymentIntentId.Should().NotBeNullOrEmpty("Payment transaction should have gateway reference ID");
        
        var paymentResult = order.RecordPaymentSuccess(paymentIntentId!);
        paymentResult.ShouldBeSuccessful();
        
        await orderRepository.UpdateAsync(order, CancellationToken.None);
        await dbContext.SaveChangesAsync(CancellationToken.None);
    }
}
