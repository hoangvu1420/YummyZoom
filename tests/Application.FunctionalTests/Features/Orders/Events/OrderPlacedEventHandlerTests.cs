using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
/// Functional tests for <see cref="OrderPlacedEventHandler"/> verifying:
/// 1. Outbox -> handler execution broadcasts via realtime notifier abstraction.
/// 2. Inbox idempotency prevents duplicate side-effects on repeated outbox draining.
/// </summary>
public class OrderPlacedEventHandlerTests : BaseTestFixture
{
    [SetUp]
    public async Task SetUpUserAndPaymentGateway()
    {
        // Authenticate as default customer (authorization pipeline requires a user)
        SetUserId(Testing.TestData.DefaultCustomerId);

        await Task.CompletedTask;
    }

    [Test]
    public async Task InitiateOrder_Should_Trigger_OrderPlaced_Broadcast_And_Inbox_Record()
    {
        // Arrange
        var notifierMock = new Mock<IOrderRealtimeNotifier>(MockBehavior.Strict);
        OrderStatusBroadcastDto? capturedDto = null;
        Guid? targetOrderId = null;
        var matchingPlacedCount = 0;
        notifierMock
            .Setup(n => n.NotifyOrderPlaced(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<NotificationTarget>(), It.IsAny<CancellationToken>()))
            .Callback<OrderStatusBroadcastDto, NotificationTarget, CancellationToken>((dto, _, __) =>
            {
                if (targetOrderId.HasValue && dto.OrderId == targetOrderId.Value && dto.Status == "Placed")
                {
                    capturedDto = dto;
                    matchingPlacedCount++;
                }
            })
            .Returns(Task.CompletedTask);
        notifierMock
            .Setup(n => n.NotifyOrderPaymentSucceeded(It.IsAny<OrderStatusBroadcastDto>(),
                It.IsAny<NotificationTarget>(),
                It.IsAny<CancellationToken>()))
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
            .Returns(Task.CompletedTask);

        // Act: initiate order (emits OrderPlaced domain event enqueued to outbox)
        // Use COD so order starts in Placed status and emits OrderPlaced domain event.
        var cmd = InitiateOrderTestHelper.BuildValidCommand(paymentMethod: InitiateOrderTestHelper.PaymentMethods.CashOnDelivery);
        var initResponse = await SendAndUnwrapAsync(cmd); // InitiateOrderResponse
        targetOrderId = initResponse.OrderId.Value;

        using (var cleanupScope = CreateScope())
        {
            var cleanupDb = cleanupScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await cleanupDb.Database.ExecuteSqlRawAsync(
                "DELETE FROM \"OutboxMessages\" WHERE \"Content\"::text NOT LIKE {0};",
                $"%{targetOrderId}%");
        }

        ReplaceOrderDependencies(notifierMock);

        await DrainOutboxAsync();

        // Assert broadcast invoked once with expected dto
        notifierMock.Verify(
            n => n.NotifyOrderPlaced(
                It.Is<OrderStatusBroadcastDto>(dto => dto.OrderId == targetOrderId && dto.Status == "Placed"),
                It.IsAny<NotificationTarget>(),
                It.IsAny<CancellationToken>()), Times.Once);
        matchingPlacedCount.Should().Be(1);
        capturedDto.Should().NotBeNull();
        capturedDto!.OrderId.Should().Be(initResponse.OrderId.Value);
        capturedDto.RestaurantId.Should().Be(Testing.TestData.DefaultRestaurantId);
        capturedDto.Status.Should().Be("Placed");
        capturedDto.EventId.Should().NotBe(Guid.Empty);
        capturedDto.OccurredOnUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));

        // Assert inbox entry created and outbox processed
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var handlerName = typeof(OrderPlacedEventHandler).FullName!;
            var inboxEntries = await db.Set<InboxMessage>()
                .Where(x => x.Handler == handlerName && x.EventId == capturedDto.EventId)
                .ToListAsync();
            inboxEntries.Should().ContainSingle();

            var processedOutbox = await db.Set<OutboxMessage>()
                .Where(m => m.Type.Contains("OrderPlaced"))
                .ToListAsync();
            processedOutbox = processedOutbox
                .Where(m => m.Content.Contains(capturedDto.EventId.ToString()))
                .ToList();
            processedOutbox.Should().ContainSingle();
            processedOutbox.Should().OnlyContain(m => m.ProcessedOnUtc != null && m.Error == null);
        }
    }

    [Test]
    public async Task DrainingOutbox_Twice_Should_Not_Duplicate_Broadcast()
    {
        // Arrange
        var notifierMock = new Mock<IOrderRealtimeNotifier>(MockBehavior.Strict);
        int callCount = 0;
        Guid? targetOrderId = null;
        Guid? placedEventId = null;
        notifierMock
            .Setup(n => n.NotifyOrderPlaced(It.IsAny<OrderStatusBroadcastDto>(),
                It.IsAny<NotificationTarget>(),
                It.IsAny<CancellationToken>()))
            .Callback<OrderStatusBroadcastDto, NotificationTarget, CancellationToken>((dto, _, __) =>
            {
                if (targetOrderId.HasValue && dto.OrderId == targetOrderId.Value && dto.Status == "Placed")
                {
                    callCount++;
                    placedEventId ??= dto.EventId;
                }
            })
            .Returns(Task.CompletedTask);
        notifierMock
            .Setup(n => n.NotifyOrderPaymentSucceeded(It.IsAny<OrderStatusBroadcastDto>(),
                It.IsAny<NotificationTarget>(),
                It.IsAny<CancellationToken>()))
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
            .Returns(Task.CompletedTask);

        // Use COD so OrderPlaced event is emitted during creation
        var initResponse = await SendAndUnwrapAsync(InitiateOrderTestHelper.BuildValidCommand(paymentMethod: InitiateOrderTestHelper.PaymentMethods.CashOnDelivery));
        targetOrderId = initResponse.OrderId.Value;

        using (var cleanupScope = CreateScope())
        {
            var cleanupDb = cleanupScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await cleanupDb.Database.ExecuteSqlRawAsync(
                "DELETE FROM \"OutboxMessages\" WHERE \"Content\"::text NOT LIKE {0};",
                $"%{targetOrderId}%");
        }

        ReplaceOrderDependencies(notifierMock);

        await DrainOutboxAsync();
        await DrainOutboxAsync(); // second drain should be idempotent

        callCount.Should().Be(1);
        notifierMock.Verify(
            n => n.NotifyOrderPlaced(
                It.Is<OrderStatusBroadcastDto>(dto => dto.OrderId == targetOrderId && dto.Status == "Placed"),
                It.IsAny<NotificationTarget>(),
                It.IsAny<CancellationToken>()), Times.Once);

        // Verify single inbox record
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var handlerName = typeof(OrderPlacedEventHandler).FullName!;
        placedEventId.Should().NotBeNull();
        var inboxEntries = await db.Set<InboxMessage>()
            .Where(x => x.Handler == handlerName && x.EventId == placedEventId)
            .ToListAsync();
        inboxEntries.Should().ContainSingle();

        // Ensure outbox processed once
        var outboxMessages = await db.Set<OutboxMessage>()
            .Where(m => m.Type.Contains("OrderPlaced"))
            .ToListAsync();
        outboxMessages = outboxMessages
            .Where(m => m.Content.Contains(placedEventId!.Value.ToString()))
            .ToList();
        outboxMessages.Should().ContainSingle();
        outboxMessages.Should().OnlyContain(m => m.ProcessedOnUtc != null && m.Error == null);
    }

    private static void ReplaceOrderDependencies(Mock<IOrderRealtimeNotifier> notifierMock)
    {
        var paymentGatewayMock = InitiateOrderTestHelper.SetupSuccessfulPaymentGatewayMock();
        ReplaceServices(replacements =>
        {
            replacements[typeof(IPaymentGatewayService)] = paymentGatewayMock.Object;
            replacements[typeof(IOrderRealtimeNotifier)] = notifierMock.Object;
        });
    }
}
