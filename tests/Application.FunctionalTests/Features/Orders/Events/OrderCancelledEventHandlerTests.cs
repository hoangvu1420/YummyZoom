using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Features.Orders.Commands.InitiateOrder;
using YummyZoom.Application.Orders.Broadcasting;
using YummyZoom.Application.Orders.EventHandlers;
using YummyZoom.Domain.OrderAggregate.Events;
using YummyZoom.Infrastructure.Persistence.EfCore;
using YummyZoom.Infrastructure.Persistence.EfCore.Models;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Orders.Events;

/// <summary>
/// Functional tests for <see cref="OrderCancelledEventHandler"/> verifying broadcast and idempotency.
/// </summary>
public class OrderCancelledEventHandlerTests : BaseTestFixture
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
    public async Task CancelOrder_Should_Trigger_OrderCancelled_Broadcast_And_Inbox_Record()
    {
        // Arrange
        var notifierMock = new Mock<IOrderRealtimeNotifier>(MockBehavior.Strict);
        OrderStatusBroadcastDto? capturedDto = null;
        Guid? targetOrderId = null;
        var matchingCancelledCount = 0;
        notifierMock.Setup(n => n.NotifyOrderPlaced(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<NotificationTarget>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notifierMock.Setup(n => n.NotifyOrderPaymentSucceeded(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<NotificationTarget>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notifierMock.Setup(n => n.NotifyOrderPaymentFailed(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<NotificationTarget>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notifierMock.Setup(n => n.NotifyOrderStatusChanged(It.IsAny<OrderStatusBroadcastDto>(),
                It.IsAny<NotificationTarget>(),
                It.IsAny<CancellationToken>()))
            .Callback<OrderStatusBroadcastDto, NotificationTarget, CancellationToken>((dto, _, __) =>
            {
                if (targetOrderId.HasValue && dto.OrderId == targetOrderId.Value && dto.Status == "Cancelled")
                {
                    capturedDto = dto;
                    matchingCancelledCount++;
                }
            })
            .Returns(Task.CompletedTask);
        ReplaceService<IOrderRealtimeNotifier>(notifierMock.Object);

        // Create placed order (COD) and drain OrderPlaced
        var init = await SendAndUnwrapAsync(InitiateOrderTestHelper.BuildValidCommand(paymentMethod: InitiateOrderTestHelper.PaymentMethods.CashOnDelivery));
        targetOrderId = init.OrderId.Value;

        using (var scope = CreateScope())
        {
            var cleanupDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await cleanupDb.Database.ExecuteSqlRawAsync(
                "DELETE FROM \"OutboxMessages\" WHERE \"Content\"::text NOT LIKE {0};",
                $"%{targetOrderId}%");
        }

        await DrainOutboxAsync();

        // Cancel the order (staff or customer can cancel from Placed depending on policy; here use staff helper to avoid auth pitfalls)
        using (var scope = CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var order = await repo.GetByIdAsync(init.OrderId, CancellationToken.None);
            order.Should().NotBeNull();

            var cancelResult = order!.Cancel();
            cancelResult.ShouldBeSuccessful();

            await repo.UpdateAsync(order, CancellationToken.None);
            await db.SaveChangesAsync(CancellationToken.None);
        }

        using (var scope = CreateScope())
        {
            var cleanupDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await cleanupDb.Database.ExecuteSqlRawAsync(
                "DELETE FROM \"OutboxMessages\" WHERE \"Content\"::text NOT LIKE {0};",
                $"%{targetOrderId}%");
        }

        await DrainOutboxAsync();

        // Assert broadcast
        notifierMock.Verify(n => n.NotifyOrderStatusChanged(
                It.Is<OrderStatusBroadcastDto>(dto => dto.OrderId == targetOrderId && dto.Status == "Cancelled"),
                It.IsAny<NotificationTarget>(),
                It.IsAny<CancellationToken>()), Times.Once);
        matchingCancelledCount.Should().Be(1);
        capturedDto.Should().NotBeNull();
        capturedDto!.OrderId.Should().Be(init.OrderId.Value);
        capturedDto.Status.Should().Be("Cancelled");
        capturedDto.EventId.Should().NotBe(Guid.Empty);

        // Inbox and outbox
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var handlerName = typeof(OrderCancelledEventHandler).FullName!;
            var inboxEntries = await db.Set<InboxMessage>()
                .Where(x => x.Handler == handlerName && x.EventId == capturedDto.EventId)
                .ToListAsync();
            inboxEntries.Should().ContainSingle();

            var processed = await db.Set<OutboxMessage>()
                .Where(m => m.Type.Contains(nameof(OrderCancelled)))
                .ToListAsync();
            processed = processed
                .Where(m => m.Content.Contains(capturedDto.EventId.ToString()))
                .ToList();
            processed.Should().ContainSingle();
            processed.Should().OnlyContain(m => m.ProcessedOnUtc != null && m.Error == null);
        }
    }

    [Test]
    public async Task DrainingOutbox_Twice_Should_Not_Duplicate_Broadcast()
    {
        var notifierMock = new Mock<IOrderRealtimeNotifier>(MockBehavior.Strict);
        int callCount = 0;
        Guid? targetOrderId = null;
        Guid? cancelledEventId = null;
        notifierMock.Setup(n => n.NotifyOrderPlaced(It.IsAny<OrderStatusBroadcastDto>(),
                It.IsAny<NotificationTarget>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notifierMock.Setup(n => n.NotifyOrderPaymentSucceeded(It.IsAny<OrderStatusBroadcastDto>(),
                It.IsAny<NotificationTarget>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notifierMock.Setup(n => n.NotifyOrderPaymentFailed(It.IsAny<OrderStatusBroadcastDto>(),
                It.IsAny<NotificationTarget>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notifierMock.Setup(n => n.NotifyOrderStatusChanged(It.IsAny<OrderStatusBroadcastDto>(),
                It.IsAny<NotificationTarget>(),
                It.IsAny<CancellationToken>()))
            .Callback<OrderStatusBroadcastDto, NotificationTarget, CancellationToken>((dto, _, __) =>
            {
                if (targetOrderId.HasValue && dto.OrderId == targetOrderId.Value && dto.Status == "Cancelled")
                {
                    callCount++;
                    cancelledEventId ??= dto.EventId;
                }
            })
            .Returns(Task.CompletedTask);
        ReplaceService<IOrderRealtimeNotifier>(notifierMock.Object);

        var init = await SendAndUnwrapAsync(InitiateOrderTestHelper.BuildValidCommand(paymentMethod: InitiateOrderTestHelper.PaymentMethods.CashOnDelivery));
        targetOrderId = init.OrderId.Value;

        using (var scope = CreateScope())
        {
            var cleanupDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await cleanupDb.Database.ExecuteSqlRawAsync(
                "DELETE FROM \"OutboxMessages\" WHERE \"Content\"::text NOT LIKE {0};",
                $"%{targetOrderId}%");
        }

        await DrainOutboxAsync();

        using (var scope = CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var order = await repo.GetByIdAsync(init.OrderId, CancellationToken.None);
            var cancelResult = order!.Cancel();
            cancelResult.ShouldBeSuccessful();
            await repo.UpdateAsync(order, CancellationToken.None);
            await db.SaveChangesAsync(CancellationToken.None);
        }

        using (var scope = CreateScope())
        {
            var cleanupDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await cleanupDb.Database.ExecuteSqlRawAsync(
                "DELETE FROM \"OutboxMessages\" WHERE \"Content\"::text NOT LIKE {0};",
                $"%{targetOrderId}%");
        }

        await DrainOutboxAsync();
        await DrainOutboxAsync();

        callCount.Should().Be(1);
        notifierMock.Verify(n => n.NotifyOrderStatusChanged(
                It.Is<OrderStatusBroadcastDto>(dto => dto.OrderId == targetOrderId && dto.Status == "Cancelled"),
                It.IsAny<NotificationTarget>(),
                It.IsAny<CancellationToken>()), Times.Once);

        using var finalScope = CreateScope();
        var dbFinal = finalScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var handlerNameFinal = typeof(OrderCancelledEventHandler).FullName!;
        cancelledEventId.Should().NotBeNull();
        var inbox = await dbFinal.Set<InboxMessage>()
            .Where(x => x.Handler == handlerNameFinal && x.EventId == cancelledEventId)
            .ToListAsync();
        inbox.Should().ContainSingle();

        var outbox = await dbFinal.Set<OutboxMessage>()
            .Where(m => m.Type.Contains(nameof(OrderCancelled)))
            .ToListAsync();
        outbox = outbox
            .Where(m => m.Content.Contains(cancelledEventId!.Value.ToString()))
            .ToList();
        outbox.Should().ContainSingle();
        outbox.Should().OnlyContain(m => m.ProcessedOnUtc != null && m.Error == null);
    }
}
