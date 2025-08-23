using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Features.Orders.Commands.InitiateOrder;
using YummyZoom.Application.Orders.Broadcasting;
using YummyZoom.Application.Orders.EventHandlers;
using YummyZoom.Infrastructure.Data;
using YummyZoom.Infrastructure.Data.Models;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Orders.Events;

/// <summary>
/// Functional tests for <see cref="OrderPreparingEventHandler"/> verifying:
/// 1. Outbox -> handler execution broadcasts via realtime notifier abstraction.
/// 2. Inbox idempotency prevents duplicate side-effects on repeated outbox draining.
/// </summary>
public class OrderPreparingEventHandlerTests : BaseTestFixture
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
    public async Task MarkOrderAsPreparing_Should_Trigger_OrderPreparing_Broadcast_And_Inbox_Record()
    {
        // Arrange
        var notifierMock = new Mock<IOrderRealtimeNotifier>(MockBehavior.Strict);
        OrderStatusBroadcastDto? capturedDto = null;
        notifierMock
            .Setup(n => n.NotifyOrderPlaced(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notifierMock
            .Setup(n => n.NotifyOrderPaymentSucceeded(It.IsAny<OrderStatusBroadcastDto>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notifierMock
            .Setup(n => n.NotifyOrderPaymentFailed(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notifierMock
            .Setup(n => n.NotifyOrderStatusChanged(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<CancellationToken>()))
            .Callback<OrderStatusBroadcastDto, CancellationToken>((dto, _) => capturedDto = dto)
            .Returns(Task.CompletedTask);

        ReplaceService<IOrderRealtimeNotifier>(notifierMock.Object);

        // Act: Create order first (using COD to get Placed status)
        var cmd = InitiateOrderTestHelper.BuildValidCommand(paymentMethod: InitiateOrderTestHelper.PaymentMethods.CashOnDelivery);
        var initResponse = await SendAndUnwrapAsync(cmd);
        
        // Drain outbox to process OrderPlaced event
        await DrainOutboxAsync();

        // Accept the order first (prerequisite for preparing)
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

        // Drain outbox to process OrderAccepted event
        await DrainOutboxAsync();

        // Mark order as preparing (this will emit OrderPreparing domain event)
        using (var preparingScope = CreateScope())
        {
            var orderRepository = preparingScope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var db = preparingScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            var order = await orderRepository.GetByIdAsync(initResponse.OrderId, CancellationToken.None);
            order.Should().NotBeNull("Order should exist after acceptance");
            
            // Mark as preparing
            var preparingResult = order!.MarkAsPreparing();
            preparingResult.ShouldBeSuccessful();
            
            // Save the order changes
            await orderRepository.UpdateAsync(order, CancellationToken.None);
            await db.SaveChangesAsync(CancellationToken.None);
        }

        // Pre-drain assertions: outbox message exists but not processed; no inbox record yet
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var pendingOutbox = await db.Set<OutboxMessage>()
                .Where(m => m.Type.Contains("OrderPreparing") && m.ProcessedOnUtc == null)
                .ToListAsync();
            pendingOutbox.Should().NotBeEmpty();

            var handlerName = typeof(OrderPreparingEventHandler).FullName!;
            var inboxPre = await db.Set<InboxMessage>()
                .AnyAsync(x => x.Handler == handlerName);
            inboxPre.Should().BeFalse();
        }

        await DrainOutboxAsync();

        // Assert broadcast invoked once with expected dto
        notifierMock.Verify(
            n => n.NotifyOrderStatusChanged(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        capturedDto.Should().NotBeNull();
        capturedDto!.OrderId.Should().Be(initResponse.OrderId.Value);
        capturedDto.RestaurantId.Should().Be(Testing.TestData.DefaultRestaurantId);
        capturedDto.Status.Should().Be("Preparing");
        capturedDto.EventId.Should().NotBe(Guid.Empty);
        capturedDto.OccurredOnUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));

        // Assert inbox entry created and outbox processed
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var handlerName = typeof(OrderPreparingEventHandler).FullName!;
            var inboxEntries = await db.Set<InboxMessage>()
                .Where(x => x.Handler == handlerName)
                .ToListAsync();
            inboxEntries.Should().HaveCount(1);

            var processedOutbox = await db.Set<OutboxMessage>()
                .Where(m => m.Type.Contains("OrderPreparing"))
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
        var capturedDtos = new List<OrderStatusBroadcastDto>();
        notifierMock
            .Setup(n => n.NotifyOrderPlaced(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notifierMock
            .Setup(n => n.NotifyOrderPaymentSucceeded(It.IsAny<OrderStatusBroadcastDto>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notifierMock
            .Setup(n => n.NotifyOrderPaymentFailed(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notifierMock
            .Setup(n => n.NotifyOrderStatusChanged(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<CancellationToken>()))
            .Callback<OrderStatusBroadcastDto, CancellationToken>((dto, _) => capturedDtos.Add(dto))
            .Returns(Task.CompletedTask);

        ReplaceService<IOrderRealtimeNotifier>(notifierMock.Object);

        // Act: Create order first (using COD to get Placed status)
        var cmd = InitiateOrderTestHelper.BuildValidCommand(paymentMethod: InitiateOrderTestHelper.PaymentMethods.CashOnDelivery);
        var initResponse = await SendAndUnwrapAsync(cmd);
        
        // Drain outbox to process OrderPlaced event
        await DrainOutboxAsync();

        // Accept the order first (prerequisite for preparing)
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

        // Drain outbox to process OrderAccepted event
        await DrainOutboxAsync();

        // Mark order as preparing (this will emit OrderPreparing domain event)
        using (var preparingScope = CreateScope())
        {
            var orderRepository = preparingScope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var db = preparingScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            var order = await orderRepository.GetByIdAsync(initResponse.OrderId, CancellationToken.None);
            order.Should().NotBeNull("Order should exist after acceptance");
            
            // Mark as preparing
            var preparingResult = order!.MarkAsPreparing();
            preparingResult.ShouldBeSuccessful();
            
            // Save the order changes
            await orderRepository.UpdateAsync(order, CancellationToken.None);
            await db.SaveChangesAsync(CancellationToken.None);
        }

        // Drain outbox twice to test idempotency
        await DrainOutboxAsync();
        await DrainOutboxAsync();

        // Assert broadcast invoked correctly despite draining twice
        // Should have exactly 2 calls: 1 for "Accepted" and 1 for "Preparing" (no duplicates from second drain)
        notifierMock.Verify(
            n => n.NotifyOrderStatusChanged(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        capturedDtos.Should().HaveCount(2);
        
        // Verify both status changes are captured (Accepted and Preparing)
        var acceptedDto = capturedDtos.Single(dto => dto.Status == "Accepted");
        var preparingDto = capturedDtos.Single(dto => dto.Status == "Preparing");
        
        // Verify the preparing broadcast (the focus of this test)
        preparingDto.OrderId.Should().Be(initResponse.OrderId.Value);
        preparingDto.RestaurantId.Should().Be(Testing.TestData.DefaultRestaurantId);
        preparingDto.Status.Should().Be("Preparing");
        preparingDto.EventId.Should().NotBe(Guid.Empty);
        preparingDto.OccurredOnUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));

        // Assert only one inbox entry created despite draining twice
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var handlerName = typeof(OrderPreparingEventHandler).FullName!;
            var inboxEntries = await db.Set<InboxMessage>()
                .Where(x => x.Handler == handlerName)
                .ToListAsync();
            inboxEntries.Should().HaveCount(1);

            var processedOutbox = await db.Set<OutboxMessage>()
                .Where(m => m.Type.Contains("OrderPreparing"))
                .ToListAsync();
            processedOutbox.Should().NotBeEmpty();
            processedOutbox.Should().OnlyContain(m => m.ProcessedOnUtc != null && m.Error == null);
        }
    }
}
