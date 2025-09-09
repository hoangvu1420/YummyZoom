using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Features.Orders.Commands.InitiateOrder;
using YummyZoom.Application.Orders.Broadcasting;
using YummyZoom.Application.Orders.EventHandlers;
using YummyZoom.Infrastructure.Persistence.EfCore;
using YummyZoom.Infrastructure.Persistence.EfCore.Models;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Orders.Events;

/// <summary>
/// Functional tests for <see cref="OrderAcceptedEventHandler"/> verifying:
/// 1. Outbox -> handler execution broadcasts via realtime notifier abstraction.
/// 2. Inbox idempotency prevents duplicate side-effects on repeated outbox draining.
/// </summary>
public class OrderAcceptedEventHandlerTests : BaseTestFixture
{
    [SetUp]
    public async Task SetUpUserAndPaymentGateway()
    {
        // Authenticate as default customer (authorization pipeline requires a user)
        SetUserId(Testing.TestData.DefaultCustomerId);

        // Mock payment gateway (order initiation uses payment service for non-COD methods)
        var paymentGatewayMock = InitiateOrderTestHelper.SetupSuccessfulPaymentGatewayMock();
        ReplaceService<IPaymentGatewayService>(paymentGatewayMock.Object);

        await Task.CompletedTask;
    }

    [Test]
    public async Task AcceptOrder_Should_Trigger_OrderAccepted_Broadcast_And_Inbox_Record()
    {
        // Arrange
        var notifierMock = new Mock<IOrderRealtimeNotifier>(MockBehavior.Strict);
        OrderStatusBroadcastDto? capturedDto = null;
        notifierMock
            .Setup(n => n.NotifyOrderPlaced(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<NotificationTarget>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notifierMock
            .Setup(n => n.NotifyOrderPaymentSucceeded(It.IsAny<OrderStatusBroadcastDto>(),
                It.IsAny<NotificationTarget>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notifierMock
            .Setup(n => n.NotifyOrderPaymentFailed(It.IsAny<OrderStatusBroadcastDto>(),
                It.IsAny<NotificationTarget>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notifierMock
            .Setup(n => n.NotifyOrderStatusChanged(It.IsAny<OrderStatusBroadcastDto>(),
                It.IsAny<NotificationTarget>(),
                It.IsAny<CancellationToken>()))
            .Callback<OrderStatusBroadcastDto, NotificationTarget, CancellationToken>((dto, _, __) => capturedDto = dto)
            .Returns(Task.CompletedTask);

        ReplaceService<IOrderRealtimeNotifier>(notifierMock.Object);

        // Act: Create order first (using COD to get Placed status)
        var cmd = InitiateOrderTestHelper.BuildValidCommand(paymentMethod: InitiateOrderTestHelper.PaymentMethods.CashOnDelivery);
        var initResponse = await SendAndUnwrapAsync(cmd);
        
        // Drain outbox to process OrderPlaced event
        await DrainOutboxAsync();

        // Accept the order (this will emit OrderAccepted domain event)
        using (var acceptScope = CreateScope())
        {
            var orderRepository = acceptScope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var db = acceptScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            var order = await orderRepository.GetByIdAsync(initResponse.OrderId, CancellationToken.None);
            order.Should().NotBeNull("Order should exist after creation");
            
            // Accept the order with estimated delivery time
            var estimatedDeliveryTime = DateTime.UtcNow.AddHours(1);
            var acceptResult = order!.Accept(estimatedDeliveryTime);
            acceptResult.ShouldBeSuccessful();
            
            // Save the order changes
            await orderRepository.UpdateAsync(order, CancellationToken.None);
            await db.SaveChangesAsync(CancellationToken.None);
        }

        // Pre-drain assertions: outbox message exists but not processed; no inbox record yet
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var pendingOutbox = await db.Set<OutboxMessage>()
                .Where(m => m.Type.Contains("OrderAccepted") && m.ProcessedOnUtc == null)
                .ToListAsync();
            pendingOutbox.Should().NotBeEmpty();

            var handlerName = typeof(OrderAcceptedEventHandler).FullName!;
            var inboxPre = await db.Set<InboxMessage>()
                .AnyAsync(x => x.Handler == handlerName);
            inboxPre.Should().BeFalse();
        }

        await DrainOutboxAsync();

        // Assert broadcast invoked once with expected dto
        notifierMock.Verify(
            n => n.NotifyOrderStatusChanged(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<NotificationTarget>(), It.IsAny<CancellationToken>()), Times.Once);
        capturedDto.Should().NotBeNull();
        capturedDto!.OrderId.Should().Be(initResponse.OrderId.Value);
        capturedDto.RestaurantId.Should().Be(Testing.TestData.DefaultRestaurantId);
        capturedDto.Status.Should().Be("Accepted");
        capturedDto.EventId.Should().NotBe(Guid.Empty);
        capturedDto.OccurredOnUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));

        // Assert inbox entry created and outbox processed
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var handlerName = typeof(OrderAcceptedEventHandler).FullName!;
            var inboxEntries = await db.Set<InboxMessage>()
                .Where(x => x.Handler == handlerName)
                .ToListAsync();
            inboxEntries.Should().HaveCount(1);

            var processedOutbox = await db.Set<OutboxMessage>()
                .Where(m => m.Type.Contains("OrderAccepted"))
                .ToListAsync();
            processedOutbox.Should().NotBeEmpty();
            processedOutbox.Should().OnlyContain(m => m.ProcessedOnUtc != null && m.Error == null);
        }
    }

    [Test]
    public async Task DrainingOutbox_Twice_Should_Not_Duplicate_Broadcast()
    {
        // Arrange
        var notifierMock = new Mock<IOrderRealtimeNotifier>(MockBehavior.Strict);
        int callCount = 0;
        notifierMock
            .Setup(n => n.NotifyOrderPlaced(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<NotificationTarget>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notifierMock
            .Setup(n => n.NotifyOrderPaymentSucceeded(It.IsAny<OrderStatusBroadcastDto>(),
                It.IsAny<NotificationTarget>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notifierMock
            .Setup(n => n.NotifyOrderPaymentFailed(It.IsAny<OrderStatusBroadcastDto>(),
                It.IsAny<NotificationTarget>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notifierMock
            .Setup(n => n.NotifyOrderStatusChanged(It.IsAny<OrderStatusBroadcastDto>(),
                It.IsAny<NotificationTarget>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => callCount++)
            .Returns(Task.CompletedTask);

        ReplaceService<IOrderRealtimeNotifier>(notifierMock.Object);

        // Create and accept order
        var initResponse = await SendAndUnwrapAsync(InitiateOrderTestHelper.BuildValidCommand(paymentMethod: InitiateOrderTestHelper.PaymentMethods.CashOnDelivery));
        await DrainOutboxAsync(); // Process OrderPlaced
        
        // Accept the order
        using (var acceptScope2 = CreateScope())
        {
            var orderRepository = acceptScope2.ServiceProvider.GetRequiredService<IOrderRepository>();
            var dbContext = acceptScope2.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            var order = await orderRepository.GetByIdAsync(initResponse.OrderId, CancellationToken.None);
            var estimatedDeliveryTime = DateTime.UtcNow.AddHours(1);
            var acceptResult = order!.Accept(estimatedDeliveryTime);
            acceptResult.ShouldBeSuccessful();
            
            await orderRepository.UpdateAsync(order, CancellationToken.None);
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }
        
        await DrainOutboxAsync();
        await DrainOutboxAsync(); // second drain should be idempotent

        callCount.Should().Be(1);
        notifierMock.Verify(
            n => n.NotifyOrderStatusChanged(It.IsAny<OrderStatusBroadcastDto>(),
                It.IsAny<NotificationTarget>(),
                It.IsAny<CancellationToken>()), Times.Once);

        // Verify single inbox record
        using var finalScope = CreateScope();
        var db = finalScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var handlerName = typeof(OrderAcceptedEventHandler).FullName!;
        var inboxEntries = await db.Set<InboxMessage>()
            .Where(x => x.Handler == handlerName)
            .ToListAsync();
        inboxEntries.Should().HaveCount(1);

        // Ensure outbox processed once
        var outboxMessages = await db.Set<OutboxMessage>()
            .Where(m => m.Type.Contains("OrderAccepted"))
            .ToListAsync();
        outboxMessages.Should().NotBeEmpty();
        outboxMessages.Should().OnlyContain(m => m.ProcessedOnUtc != null && m.Error == null);
    }
}
