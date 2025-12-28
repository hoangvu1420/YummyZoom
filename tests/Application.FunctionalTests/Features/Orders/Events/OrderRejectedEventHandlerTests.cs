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
using YummyZoom.SharedKernel;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Orders.Events;

/// <summary>
/// Functional tests for <see cref="OrderRejectedEventHandler"/> verifying:
/// 1. Outbox -> handler execution broadcasts via realtime notifier abstraction.
/// 2. Inbox idempotency prevents duplicate side-effects on repeated outbox draining.
/// </summary>
public class OrderRejectedEventHandlerTests : BaseTestFixture
{
    [SetUp]
    public async Task SetUpUserAndPaymentGateway()
    {
        // Authenticate as default customer (authorization pipeline requires a user)
        SetUserId(Testing.TestData.DefaultCustomerId);

        await Task.CompletedTask;
    }

    [Test]
    public async Task RejectOrder_Should_Trigger_OrderRejected_Broadcast_And_Inbox_Record()
    {
        // Arrange
        var notifierMock = new Mock<IOrderRealtimeNotifier>(MockBehavior.Strict);
        OrderStatusBroadcastDto? capturedDto = null;
        Guid? targetOrderId = null;
        var matchingRejectedCount = 0;
        notifierMock
            .Setup(n => n.NotifyOrderPlaced(It.IsAny<OrderStatusBroadcastDto>(),
            It.IsAny<NotificationTarget>(),
            It.IsAny<CancellationToken>()))
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
            .Callback<OrderStatusBroadcastDto, NotificationTarget, CancellationToken>((dto, _, __) =>
            {
                if (targetOrderId.HasValue && dto.OrderId == targetOrderId.Value && dto.Status == "Rejected")
                {
                    capturedDto = dto;
                    matchingRejectedCount++;
                }
            })
            .Returns(Task.CompletedTask);

        // FCM mock to verify data push
        var fcmMock = new Mock<IFcmService>(MockBehavior.Strict);
        fcmMock.Setup(m => m.SendMulticastDataAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(Result.Success<List<string>>(new List<string>()));
        // Allow other methods if touched
        fcmMock.Setup(m => m.SendMulticastNotificationAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(Result.Success<List<string>>(new List<string>()));
        fcmMock.Setup(m => m.SendNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(Result.Success());
        fcmMock.Setup(m => m.SendDataMessageAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(Result.Success());

        ReplaceOrderDependencies(notifierMock, fcmMock);

        // Act: Create order first (using COD to get Placed status)
        var cmd = InitiateOrderTestHelper.BuildValidCommand(paymentMethod: InitiateOrderTestHelper.PaymentMethods.CashOnDelivery);
        var initResponse = await SendAndUnwrapAsync(cmd);
        targetOrderId = initResponse.OrderId.Value;

        using (var scope = CreateScope())
        {
            var cleanupDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await cleanupDb.Database.ExecuteSqlRawAsync(
                "DELETE FROM \"OutboxMessages\" WHERE \"Content\"::text NOT LIKE {0};",
                $"%{targetOrderId}%");
        }

        // Drain outbox to process OrderPlaced event
        await DrainOutboxAsync();

        // Reject the order (this will emit OrderRejected domain event)
        using (var scope = CreateScope())
        {
            var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var order = await orderRepository.GetByIdAsync(initResponse.OrderId, CancellationToken.None);
            order.Should().NotBeNull("Order should exist after creation");

            var rejectResult = order!.Reject();
            rejectResult.ShouldBeSuccessful();

            await orderRepository.UpdateAsync(order, CancellationToken.None);
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

        // Assert broadcast invoked once with expected dto
        notifierMock.Verify(
            n => n.NotifyOrderStatusChanged(
                It.Is<OrderStatusBroadcastDto>(dto => dto.OrderId == targetOrderId && dto.Status == "Rejected"),
                It.IsAny<NotificationTarget>(),
                It.IsAny<CancellationToken>()), Times.Once);
        matchingRejectedCount.Should().Be(1);
        capturedDto.Should().NotBeNull();
        capturedDto!.OrderId.Should().Be(initResponse.OrderId.Value);
        capturedDto.RestaurantId.Should().Be(Testing.TestData.DefaultRestaurantId);
        capturedDto.Status.Should().Be("Rejected");
        capturedDto.EventId.Should().NotBe(Guid.Empty);
        capturedDto.OccurredOnUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));

        // Assert inbox entry created and outbox processed
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var handlerName = typeof(OrderRejectedEventHandler).FullName!;
            var inboxEntries = await db.Set<InboxMessage>()
                .Where(x => x.Handler == handlerName && x.EventId == capturedDto.EventId)
                .ToListAsync();
            inboxEntries.Should().ContainSingle();

            var processedOutbox = await db.Set<OutboxMessage>()
                .Where(m => m.Type.Contains(nameof(OrderRejected)))
                .ToListAsync();
            processedOutbox = processedOutbox
                .Where(m => m.Content.Contains(capturedDto.EventId.ToString()))
                .ToList();
            processedOutbox.Should().ContainSingle();
            processedOutbox.Should().OnlyContain(m => m.ProcessedOnUtc != null && m.Error == null);
        }

        // Optionally verify FCM data push (only when tokens exist). Allow at most once.
        fcmMock.Verify(m => m.SendMulticastDataAsync(
                It.IsAny<IEnumerable<string>>(),
                It.Is<Dictionary<string, string>>(d => d.ContainsKey("orderId"))),
            Times.AtMostOnce);
    }

    [Test]
    public async Task DrainingOutbox_Twice_Should_Not_Duplicate_Broadcast()
    {
        // Arrange
        var notifierMock = new Mock<IOrderRealtimeNotifier>(MockBehavior.Strict);
        int callCount = 0;
        Guid? targetOrderId = null;
        Guid? rejectedEventId = null;
        notifierMock
            .Setup(n => n.NotifyOrderPlaced(It.IsAny<OrderStatusBroadcastDto>(),
                It.IsAny<NotificationTarget>(),
                It.IsAny<CancellationToken>()))
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
            .Callback<OrderStatusBroadcastDto, NotificationTarget, CancellationToken>((dto, _, __) =>
            {
                if (targetOrderId.HasValue && dto.OrderId == targetOrderId.Value && dto.Status == "Rejected")
                {
                    callCount++;
                    rejectedEventId ??= dto.EventId;
                }
            })
            .Returns(Task.CompletedTask);

        // Create placed order
        var initResponse = await SendAndUnwrapAsync(InitiateOrderTestHelper.BuildValidCommand(paymentMethod: InitiateOrderTestHelper.PaymentMethods.CashOnDelivery));
        targetOrderId = initResponse.OrderId.Value;

        using (var scope = CreateScope())
        {
            var cleanupDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await cleanupDb.Database.ExecuteSqlRawAsync(
                "DELETE FROM \"OutboxMessages\" WHERE \"Content\"::text NOT LIKE {0};",
                $"%{targetOrderId}%");
        }

        ReplaceOrderDependencies(notifierMock);

        await DrainOutboxAsync(); // Process OrderPlaced

        // Reject the order
        using (var scope = CreateScope())
        {
            var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var order = await orderRepository.GetByIdAsync(initResponse.OrderId, CancellationToken.None);
            var rejectResult = order!.Reject();
            rejectResult.ShouldBeSuccessful();

            await orderRepository.UpdateAsync(order, CancellationToken.None);
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        using (var scope = CreateScope())
        {
            var cleanupDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await cleanupDb.Database.ExecuteSqlRawAsync(
                "DELETE FROM \"OutboxMessages\" WHERE \"Content\"::text NOT LIKE {0};",
                $"%{targetOrderId}%");
        }

        await DrainOutboxAsync();
        await DrainOutboxAsync(); // second drain should be idempotent

        callCount.Should().Be(1);
        notifierMock.Verify(
            n => n.NotifyOrderStatusChanged(
                It.Is<OrderStatusBroadcastDto>(dto => dto.OrderId == targetOrderId && dto.Status == "Rejected"),
                It.IsAny<NotificationTarget>(),
                It.IsAny<CancellationToken>()), Times.Once);

        // Verify single inbox record
        using var finalScope = CreateScope();
        var db = finalScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var handlerName = typeof(OrderRejectedEventHandler).FullName!;
        rejectedEventId.Should().NotBeNull();
        var inboxEntries = await db.Set<InboxMessage>()
            .Where(x => x.Handler == handlerName && x.EventId == rejectedEventId)
            .ToListAsync();
        inboxEntries.Should().ContainSingle();

        // Ensure outbox processed once
        var outboxMessages = await db.Set<OutboxMessage>()
            .Where(m => m.Type.Contains(nameof(OrderRejected)))
            .ToListAsync();
        outboxMessages = outboxMessages
            .Where(m => m.Content.Contains(rejectedEventId!.Value.ToString()))
            .ToList();
        outboxMessages.Should().ContainSingle();
        outboxMessages.Should().OnlyContain(m => m.ProcessedOnUtc != null && m.Error == null);
    }

    private static void ReplaceOrderDependencies(Mock<IOrderRealtimeNotifier> notifierMock, Mock<IFcmService>? fcmMock = null)
    {
        var paymentGatewayMock = InitiateOrderTestHelper.SetupSuccessfulPaymentGatewayMock();
        ReplaceServices(replacements =>
        {
            replacements[typeof(IPaymentGatewayService)] = paymentGatewayMock.Object;
            replacements[typeof(IOrderRealtimeNotifier)] = notifierMock.Object;
            if (fcmMock != null)
            {
                replacements[typeof(IFcmService)] = fcmMock.Object;
            }
        });
    }
}
