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

public class OrderPaymentSucceededEventHandlerTests : BaseTestFixture
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
    public async Task OnlinePayment_OrderInitiated_PaymentSuccessBroadcastAndInbox()
    {
        // Arrange: build command with online payment (CreditCard default)
        var notifierMock = new Mock<IOrderRealtimeNotifier>(MockBehavior.Strict);
        OrderStatusBroadcastDto? paymentDto = null; OrderStatusBroadcastDto? placedDto = null;
        notifierMock.Setup(n => n.NotifyOrderPaymentSucceeded(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<NotificationTarget>(), It.IsAny<CancellationToken>()))
            .Callback<OrderStatusBroadcastDto, NotificationTarget, CancellationToken>((dto, _, __) => paymentDto = dto)
            .Returns(Task.CompletedTask);
        notifierMock.Setup(n => n.NotifyOrderPlaced(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<NotificationTarget>(), It.IsAny<CancellationToken>()))
            .Callback<OrderStatusBroadcastDto, NotificationTarget, CancellationToken>((dto, _, __) => placedDto = dto)
            .Returns(Task.CompletedTask);
        notifierMock.Setup(n => n.NotifyOrderPaymentFailed(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<NotificationTarget>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notifierMock.Setup(n => n.NotifyOrderStatusChanged(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<NotificationTarget>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        ReplaceService<IOrderRealtimeNotifier>(notifierMock.Object);

        var cmd = InitiateOrderTestHelper.BuildValidCommand(paymentMethod: InitiateOrderTestHelper.PaymentMethods.CreditCard);
        var response = await SendAndUnwrapAsync(cmd);

        // Simulate payment confirmation (what the Stripe webhook would do)
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

            // Record payment success (simulating Stripe webhook)
            var paymentResult = order.RecordPaymentSuccess(paymentIntentId!);
            paymentResult.ShouldBeSuccessful();

            // Save the order changes
            await orderRepository.UpdateAsync(order, CancellationToken.None);
            await db.SaveChangesAsync(CancellationToken.None);
        }

        // Pre-drain: ensure outbox has both OrderPaymentSucceeded & OrderPlaced unprocessed
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var pending = await db.Set<OutboxMessage>()
                .Where(m => (m.Type.Contains(nameof(OrderPaymentSucceeded)) || m.Type.Contains(nameof(OrderPlaced))) && m.ProcessedOnUtc == null)
                .ToListAsync();
            pending.Should().NotBeEmpty();
            pending.Any(m => m.Type.Contains(nameof(OrderPaymentSucceeded))).Should().BeTrue();
            pending.Any(m => m.Type.Contains(nameof(OrderPlaced))).Should().BeTrue();
        }

        await DrainOutboxAsync();

        // Assert: both payment succeeded and placed notifications fired exactly once
        notifierMock.Verify(n => n.NotifyOrderPaymentSucceeded(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<NotificationTarget>(), It.IsAny<CancellationToken>()), Times.Once);
        notifierMock.Verify(n => n.NotifyOrderPlaced(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<NotificationTarget>(), It.IsAny<CancellationToken>()), Times.Once);
        paymentDto.Should().NotBeNull(); placedDto.Should().NotBeNull();
        paymentDto!.OrderId.Should().Be(response.OrderId.Value);
        placedDto!.OrderId.Should().Be(response.OrderId.Value);
        paymentDto.Status.Should().Be("Placed");
        placedDto.Status.Should().Be("Placed");
        paymentDto.EventId.Should().NotBe(placedDto.EventId); // distinct events

        // Inbox assertions: entries for both handlers
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var paymentHandler = typeof(OrderPaymentSucceededEventHandler).FullName!;
            var placedHandler = typeof(OrderPlacedEventHandler).FullName!;
            var inbox = await db.Set<InboxMessage>().Where(x => x.Handler == paymentHandler || x.Handler == placedHandler).ToListAsync();
            inbox.Any(x => x.Handler == paymentHandler).Should().BeTrue();
            inbox.Any(x => x.Handler == placedHandler).Should().BeTrue();
        }
    }

    [Test]
    public async Task DrainingTwice_Should_Not_Duplicate_PaymentSucceeded_Broadcast()
    {
        var notifierMock = new Mock<IOrderRealtimeNotifier>(MockBehavior.Strict);
        int paymentCalls = 0; int placedCalls = 0;
        notifierMock.Setup(n => n.NotifyOrderPaymentSucceeded(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<NotificationTarget>(), It.IsAny<CancellationToken>()))
            .Callback(() => paymentCalls++)
            .Returns(Task.CompletedTask);
        notifierMock.Setup(n => n.NotifyOrderPlaced(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<NotificationTarget>(), It.IsAny<CancellationToken>()))
            .Callback(() => placedCalls++)
            .Returns(Task.CompletedTask);
        notifierMock.Setup(n => n.NotifyOrderPaymentFailed(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<NotificationTarget>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notifierMock.Setup(n => n.NotifyOrderStatusChanged(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<NotificationTarget>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        ReplaceService<IOrderRealtimeNotifier>(notifierMock.Object);

        var response = await SendAndUnwrapAsync(InitiateOrderTestHelper.BuildValidCommand(paymentMethod: InitiateOrderTestHelper.PaymentMethods.CreditCard));

        // Simulate payment confirmation (what the Stripe webhook would do)
        using (var paymentScope = CreateScope())
        {
            var orderRepository = paymentScope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var order = await orderRepository.GetByIdAsync(response.OrderId, CancellationToken.None);
            var paymentTransaction = order!.PaymentTransactions.FirstOrDefault();
            var paymentIntentId = paymentTransaction!.PaymentGatewayReferenceId;
            var paymentResult = order.RecordPaymentSuccess(paymentIntentId!);
            paymentResult.ShouldBeSuccessful();
            await orderRepository.UpdateAsync(order, CancellationToken.None);
            var dbContext = paymentScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        await DrainOutboxAsync();
        await DrainOutboxAsync();

        paymentCalls.Should().Be(1);
        placedCalls.Should().Be(1);
        notifierMock.Verify(n => n.NotifyOrderPaymentSucceeded(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<NotificationTarget>(), It.IsAny<CancellationToken>()), Times.Once);
        notifierMock.Verify(n => n.NotifyOrderPlaced(It.IsAny<OrderStatusBroadcastDto>(), It.IsAny<NotificationTarget>(), It.IsAny<CancellationToken>()), Times.Once);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var paymentHandler = typeof(OrderPaymentSucceededEventHandler).FullName!;
        var inboxPayment = await db.Set<InboxMessage>().Where(x => x.Handler == paymentHandler).ToListAsync();
        inboxPayment.Should().HaveCount(1);
    }
}
