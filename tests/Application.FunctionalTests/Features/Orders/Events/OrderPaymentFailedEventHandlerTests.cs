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
/// Functional tests for <see cref="OrderPaymentFailedEventHandler"/> verifying:
/// 1. Payment failure broadcasts inform restaurant staff to ignore failed orders.
/// 2. Inbox idempotency prevents duplicate side-effects on repeated outbox draining.
/// </summary>
public class OrderPaymentFailedEventHandlerTests : BaseTestFixture
{
    [SetUp]
    public async Task SetUpUserAndPaymentGateway()
    {
        SetUserId(Testing.TestData.DefaultCustomerId);
        await Task.CompletedTask;
    }

    [Test]
    public async Task OnlinePayment_PaymentFailure_ShouldBroadcastFailureAndCreateInboxRecord()
    {
        // Arrange: build command with online payment (creates order in AwaitingPayment status)
        var notifierMock = new Mock<IOrderRealtimeNotifier>(MockBehavior.Strict);
        OrderStatusBroadcastDto? failureDto = null;
        OrderStatusBroadcastDto? cancelledStatusDto = null;
        Guid? targetOrderId = null;
        var failureMatchCount = 0;
        var statusMatchCount = 0;

        notifierMock.Setup(n => n.NotifyOrderPaymentFailed(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<NotificationTarget>(), It.IsAny<CancellationToken>()))
            .Callback<OrderStatusBroadcastDto, NotificationTarget, CancellationToken>((dto, _, __) =>
            {
                if (targetOrderId.HasValue && dto.OrderId == targetOrderId.Value)
                {
                    failureDto = dto;
                    failureMatchCount++;
                }
            })
            .Returns(Task.CompletedTask);
        notifierMock.Setup(n => n.NotifyOrderStatusChanged(It.IsAny<OrderStatusBroadcastDto>(),
                It.IsAny<NotificationTarget>(),
                It.IsAny<CancellationToken>()))
            .Callback<OrderStatusBroadcastDto, NotificationTarget, CancellationToken>((dto, _, __) =>
            {
                if (targetOrderId.HasValue && dto.OrderId == targetOrderId.Value && dto.Status == "Cancelled")
                {
                    cancelledStatusDto = dto;
                    statusMatchCount++;
                }
            })
            .Returns(Task.CompletedTask);
        notifierMock.Setup(n => n.NotifyOrderPaymentSucceeded(It.IsAny<OrderStatusBroadcastDto>(),
                It.IsAny<NotificationTarget>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notifierMock.Setup(n => n.NotifyOrderPlaced(It.IsAny<OrderStatusBroadcastDto>(),
                It.IsAny<NotificationTarget>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        ReplaceOrderDependencies(notifierMock);

        var cmd = InitiateOrderTestHelper.BuildValidCommand(paymentMethod: InitiateOrderTestHelper.PaymentMethods.CreditCard);
        var response = await SendAndUnwrapAsync(cmd);
        targetOrderId = response.OrderId.Value;

        using (var scope = CreateScope())
        {
            var cleanupDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await cleanupDb.Database.ExecuteSqlRawAsync(
                "DELETE FROM \"OutboxMessages\" WHERE \"Content\"::text NOT LIKE {0};",
                $"%{targetOrderId}%");
        }

        // Simulate payment failure (what the Stripe webhook would do)
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

            var order = await orderRepository.GetByIdAsync(response.OrderId, CancellationToken.None);
            order.Should().NotBeNull("Order should exist after creation");

            // Get the payment intent ID from the order's payment transaction
            var paymentTransaction = order!.PaymentTransactions.FirstOrDefault();
            paymentTransaction.Should().NotBeNull("Order should have a payment transaction");
            var paymentIntentId = paymentTransaction!.PaymentGatewayReferenceId;
            paymentIntentId.Should().NotBeNullOrEmpty("Payment transaction should have gateway reference ID");

            // Record payment failure (simulating Stripe webhook)
            var paymentResult = order.RecordPaymentFailure(paymentIntentId!);
            paymentResult.ShouldBeSuccessful();

            // Save the order changes
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

        // Assert: both notifications fired exactly once
        notifierMock.Verify(n => n.NotifyOrderPaymentFailed(
                It.Is<OrderStatusBroadcastDto>(dto => dto.OrderId == targetOrderId),
                It.IsAny<NotificationTarget>(),
                It.IsAny<CancellationToken>()), Times.Once);
        notifierMock.Verify(n => n.NotifyOrderStatusChanged(
                It.Is<OrderStatusBroadcastDto>(dto => dto.OrderId == targetOrderId && dto.Status == "Cancelled"),
                It.IsAny<NotificationTarget>(),
                It.IsAny<CancellationToken>()), Times.Once);
        failureMatchCount.Should().Be(1);
        statusMatchCount.Should().Be(1);

        failureDto.Should().NotBeNull();
        failureDto!.OrderId.Should().Be(response.OrderId.Value);
        failureDto.Status.Should().Be("Cancelled");

        cancelledStatusDto.Should().NotBeNull();
        cancelledStatusDto!.OrderId.Should().Be(response.OrderId.Value);
        cancelledStatusDto.Status.Should().Be("Cancelled");

        // Inbox assertions: entry for payment failure handler
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var failureHandlerName = typeof(OrderPaymentFailedEventHandler).FullName!;

            var failureInboxEntries = await db.Set<InboxMessage>()
                .Where(x => x.Handler == failureHandlerName && x.EventId == failureDto.EventId)
                .ToListAsync();
            failureInboxEntries.Should().ContainSingle();
        }

        // Outbox processed
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var processedOutbox = await db.Set<OutboxMessage>()
                .Where(m => m.Type.Contains(nameof(OrderPaymentFailed)))
                .ToListAsync();
            processedOutbox = processedOutbox
                .Where(m => m.Content.Contains(failureDto.EventId.ToString()))
                .ToList();
            processedOutbox.Should().ContainSingle();
            processedOutbox.Should().OnlyContain(m => m.ProcessedOnUtc != null && m.Error == null);
        }
    }

    [Test]
    public async Task DrainingOutbox_Twice_Should_Not_Duplicate_PaymentFailureBroadcast()
    {
        // Arrange
        var notifierMock = new Mock<IOrderRealtimeNotifier>(MockBehavior.Strict);
        int failureCallCount = 0;
        int statusCallCount = 0;
        Guid? targetOrderId = null;
        Guid? failureEventId = null;
        Guid? cancelledEventId = null;

        notifierMock.Setup(n => n.NotifyOrderPaymentFailed(It.IsAny<OrderStatusBroadcastDto>(),
                It.IsAny<NotificationTarget>(),
                It.IsAny<CancellationToken>()))
            .Callback<OrderStatusBroadcastDto, NotificationTarget, CancellationToken>((dto, _, __) =>
            {
                if (targetOrderId.HasValue && dto.OrderId == targetOrderId.Value)
                {
                    failureCallCount++;
                    failureEventId ??= dto.EventId;
                }
            })
            .Returns(Task.CompletedTask);
        notifierMock.Setup(n => n.NotifyOrderStatusChanged(It.IsAny<OrderStatusBroadcastDto>(),
                It.IsAny<NotificationTarget>(),
                It.IsAny<CancellationToken>()))
            .Callback<OrderStatusBroadcastDto, NotificationTarget, CancellationToken>((dto, _, __) =>
            {
                if (targetOrderId.HasValue && dto.OrderId == targetOrderId.Value && dto.Status == "Cancelled")
                {
                    statusCallCount++;
                    cancelledEventId ??= dto.EventId;
                }
            })
            .Returns(Task.CompletedTask);
        notifierMock.Setup(n => n.NotifyOrderPaymentSucceeded(It.IsAny<OrderStatusBroadcastDto>(),
                It.IsAny<NotificationTarget>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notifierMock.Setup(n => n.NotifyOrderPlaced(It.IsAny<OrderStatusBroadcastDto>(),
                It.IsAny<NotificationTarget>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        ReplaceOrderDependencies(notifierMock);

        var cmd = InitiateOrderTestHelper.BuildValidCommand(paymentMethod: InitiateOrderTestHelper.PaymentMethods.CreditCard);
        var response = await SendAndUnwrapAsync(cmd);
        targetOrderId = response.OrderId.Value;

        using (var scope = CreateScope())
        {
            var cleanupDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await cleanupDb.Database.ExecuteSqlRawAsync(
                "DELETE FROM \"OutboxMessages\" WHERE \"Content\"::text NOT LIKE {0};",
                $"%{targetOrderId}%");
        }

        // Simulate payment failure
        using (var scope = CreateScope())
        {
            var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var order = await orderRepository.GetByIdAsync(response.OrderId, CancellationToken.None);
            var paymentIntentId = order!.PaymentTransactions.First().PaymentGatewayReferenceId!;

            order.RecordPaymentFailure(paymentIntentId).ShouldBeSuccessful();
            await orderRepository.UpdateAsync(order, CancellationToken.None);
            await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().SaveChangesAsync();
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

        failureCallCount.Should().Be(1);
        notifierMock.Verify(n => n.NotifyOrderPaymentFailed(
                It.Is<OrderStatusBroadcastDto>(dto => dto.OrderId == targetOrderId),
                It.IsAny<NotificationTarget>(),
                It.IsAny<CancellationToken>()), Times.Once);
        statusCallCount.Should().Be(1);
        notifierMock.Verify(n => n.NotifyOrderStatusChanged(
                It.Is<OrderStatusBroadcastDto>(dto => dto.OrderId == targetOrderId && dto.Status == "Cancelled"),
                It.IsAny<NotificationTarget>(),
                It.IsAny<CancellationToken>()), Times.Once);

        // Verify single inbox record for payment failure handler
        using var finalScope = CreateScope();
        var db = finalScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var handlerName = typeof(OrderPaymentFailedEventHandler).FullName!;
        failureEventId.Should().NotBeNull();
        var inboxEntries = await db.Set<InboxMessage>()
            .Where(x => x.Handler == handlerName && x.EventId == failureEventId)
            .ToListAsync();
        inboxEntries.Should().ContainSingle();

        // Ensure outbox processed once
        var outboxMessages = await db.Set<OutboxMessage>()
            .Where(m => m.Type.Contains(nameof(OrderPaymentFailed)))
            .ToListAsync();
        outboxMessages = outboxMessages
            .Where(m => m.Content.Contains(failureEventId!.Value.ToString()))
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
