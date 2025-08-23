using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Features.Orders.Commands.InitiateOrder;
using YummyZoom.Application.Orders.Broadcasting;
using YummyZoom.Application.Orders.EventHandlers;
using YummyZoom.Domain.OrderAggregate.Events;
using YummyZoom.Infrastructure.Data;
using YummyZoom.Infrastructure.Data.Models;
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
        var paymentGatewayMock = InitiateOrderTestHelper.SetupSuccessfulPaymentGatewayMock();
        ReplaceService<IPaymentGatewayService>(paymentGatewayMock.Object);
        await Task.CompletedTask;
    }

    [Test]
    public async Task OnlinePayment_PaymentFailure_ShouldBroadcastFailureAndCreateInboxRecord()
    {
        // Arrange: build command with online payment (creates order in AwaitingPayment status)
        var notifierMock = new Mock<IOrderRealtimeNotifier>(MockBehavior.Strict);
        OrderStatusBroadcastDto? failureDto = null;
        OrderStatusBroadcastDto? cancelledStatusDto = null;

        notifierMock.Setup(n => n.NotifyOrderPaymentFailed(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<CancellationToken>()))
            .Callback<OrderStatusBroadcastDto, CancellationToken>((dto, _) => failureDto = dto)
            .Returns(Task.CompletedTask);
        notifierMock.Setup(n => n.NotifyOrderStatusChanged(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<CancellationToken>()))
            .Callback<OrderStatusBroadcastDto, CancellationToken>((dto, _) => cancelledStatusDto = dto)
            .Returns(Task.CompletedTask);
        notifierMock.Setup(n => n.NotifyOrderPaymentSucceeded(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notifierMock.Setup(n => n.NotifyOrderPlaced(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ReplaceService<IOrderRealtimeNotifier>(notifierMock.Object);

        var cmd = InitiateOrderTestHelper.BuildValidCommand(paymentMethod: InitiateOrderTestHelper.PaymentMethods.CreditCard);
        var response = await SendAndUnwrapAsync(cmd);

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

        // Pre-drain: ensure outbox has both OrderPaymentFailed & OrderCancelled unprocessed
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var pending = await db.Set<OutboxMessage>()
                .Where(m => (m.Type.Contains(nameof(OrderPaymentFailed)) || m.Type.Contains(nameof(OrderCancelled))) && m.ProcessedOnUtc == null)
                .ToListAsync();
            pending.Should().NotBeEmpty();
            pending.Any(m => m.Type.Contains(nameof(OrderPaymentFailed))).Should().BeTrue();
            pending.Any(m => m.Type.Contains(nameof(OrderCancelled))).Should().BeTrue();
        }

        await DrainOutboxAsync();

        // Assert: both notifications fired exactly once
        notifierMock.Verify(n => n.NotifyOrderPaymentFailed(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<CancellationToken>()), Times.Once);
        notifierMock.Verify(n => n.NotifyOrderStatusChanged(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<CancellationToken>()), Times.Once);

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
                .Where(x => x.Handler == failureHandlerName)
                .ToListAsync();
            failureInboxEntries.Should().HaveCount(1);
        }

        // Outbox processed
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var processedOutbox = await db.Set<OutboxMessage>()
                .Where(m => m.Type.Contains(nameof(OrderPaymentFailed)))
                .ToListAsync();
            processedOutbox.Should().NotBeEmpty();
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

        notifierMock.Setup(n => n.NotifyOrderPaymentFailed(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<CancellationToken>()))
            .Callback(() => failureCallCount++)
            .Returns(Task.CompletedTask);
        notifierMock.Setup(n => n.NotifyOrderStatusChanged(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<CancellationToken>()))
            .Callback(() => statusCallCount++)
            .Returns(Task.CompletedTask);
        notifierMock.Setup(n => n.NotifyOrderPaymentSucceeded(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notifierMock.Setup(n => n.NotifyOrderPlaced(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ReplaceService<IOrderRealtimeNotifier>(notifierMock.Object);

        var cmd = InitiateOrderTestHelper.BuildValidCommand(paymentMethod: InitiateOrderTestHelper.PaymentMethods.CreditCard);
        var response = await SendAndUnwrapAsync(cmd);

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

        await DrainOutboxAsync();
        await DrainOutboxAsync(); // second drain should be idempotent

        failureCallCount.Should().Be(1);
        notifierMock.Verify(n => n.NotifyOrderPaymentFailed(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<CancellationToken>()), Times.Once);
        statusCallCount.Should().Be(1);
        notifierMock.Verify(n => n.NotifyOrderStatusChanged(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<CancellationToken>()), Times.Once);

        // Verify single inbox record for payment failure handler
        using var finalScope = CreateScope();
        var db = finalScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var handlerName = typeof(OrderPaymentFailedEventHandler).FullName!;
        var inboxEntries = await db.Set<InboxMessage>()
            .Where(x => x.Handler == handlerName)
            .ToListAsync();
        inboxEntries.Should().HaveCount(1);

        // Ensure outbox processed once
        var outboxMessages = await db.Set<OutboxMessage>()
            .Where(m => m.Type.Contains(nameof(OrderPaymentFailed)))
            .ToListAsync();
        outboxMessages.Should().NotBeEmpty();
        outboxMessages.Should().OnlyContain(m => m.ProcessedOnUtc != null && m.Error == null);
    }
}
