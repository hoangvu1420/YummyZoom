using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.Orders.Broadcasting;
using YummyZoom.Web.Realtime;
using YummyZoom.Web.Realtime.Hubs;

namespace YummyZoom.Application.FunctionalTests.Web.SignalR;

[TestFixture]
public class SignalROrderRealtimeNotifierTests : BaseTestFixture
{
    private static bool MatchesDtoPayload(object?[]? args, OrderStatusBroadcastDto expected)
    {
        if (args is null || args.Length != 1) return false;
        if (args[0] is not OrderStatusBroadcastDto sent) return false;
        return sent.OrderId == expected.OrderId && sent.RestaurantId == expected.RestaurantId;
    }

    private record TestSetup(
        SignalROrderRealtimeNotifier Sut,
        Mock<IHubClients> RestaurantHubClients,
        Mock<IClientProxy> RestaurantClientProxy,
        Mock<IHubClients> CustomerHubClients,
        Mock<IClientProxy> CustomerClientProxy
    );

    private static TestSetup CreateSut(Guid restaurantId, Guid orderId)
    {
        // Restaurant hub setup
        var restaurantHubContext = new Mock<IHubContext<RestaurantOrdersHub>>(MockBehavior.Strict);
        var restaurantHubClients = new Mock<IHubClients>(MockBehavior.Strict);
        var restaurantClientProxy = new Mock<IClientProxy>(MockBehavior.Strict);

        var restaurantGroup = $"restaurant:{restaurantId}";
        restaurantHubContext.SetupGet(h => h.Clients).Returns(restaurantHubClients.Object);
        restaurantHubClients.Setup(c => c.Group(restaurantGroup)).Returns(restaurantClientProxy.Object);

        // Customer hub setup
        var customerHubContext = new Mock<IHubContext<CustomerOrdersHub>>(MockBehavior.Strict);
        var customerHubClients = new Mock<IHubClients>(MockBehavior.Strict);
        var customerClientProxy = new Mock<IClientProxy>(MockBehavior.Strict);

        var customerGroup = $"order:{orderId}";
        customerHubContext.SetupGet(h => h.Clients).Returns(customerHubClients.Object);
        customerHubClients.Setup(c => c.Group(customerGroup)).Returns(customerClientProxy.Object);

        var logger = new Mock<ILogger<SignalROrderRealtimeNotifier>>();

        var sut = new SignalROrderRealtimeNotifier(
            restaurantHubContext.Object,
            customerHubContext.Object,
            logger.Object);

        return new TestSetup(sut, restaurantHubClients, restaurantClientProxy, customerHubClients, customerClientProxy);
    }

    private static OrderStatusBroadcastDto MakeDto(Guid restaurantId, Guid orderId)
    {
        return new OrderStatusBroadcastDto(
            OrderId: orderId,
            OrderNumber: "TEST-ORDER",
            Status: "Placed",
            RestaurantId: restaurantId,
            LastUpdateTimestamp: DateTime.UtcNow,
            EstimatedDeliveryTime: null,
            ActualDeliveryTime: null,
            DeliveredAt: null,
            OccurredOnUtc: DateTime.UtcNow,
            EventId: Guid.NewGuid());
    }

    [Test]
    public async Task NotifyOrderPlaced_Restaurant_Target_Sends_To_Restaurant_Only()
    {
        // Arrange
        var restaurantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var setup = CreateSut(restaurantId, orderId);
        var dto = MakeDto(restaurantId, orderId);

        setup.RestaurantClientProxy
            .Setup(p => p.SendCoreAsync(
                "ReceiveOrderPlaced",
                It.Is<object?[]>(a => MatchesDtoPayload(a, dto)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        await setup.Sut.NotifyOrderPlaced(dto, NotificationTarget.Restaurant);

        // Assert
        setup.RestaurantHubClients.Verify(c => c.Group($"restaurant:{restaurantId}"), Times.Once);
        setup.RestaurantClientProxy.Verify();
        setup.CustomerHubClients.Verify(c => c.Group(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task NotifyOrderPlaced_Customer_Target_Sends_To_Customer_Only()
    {
        // Arrange
        var restaurantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var setup = CreateSut(restaurantId, orderId);
        var dto = MakeDto(restaurantId, orderId);

        setup.CustomerClientProxy
            .Setup(p => p.SendCoreAsync(
                "ReceiveOrderPlaced",
                It.Is<object?[]>(a => MatchesDtoPayload(a, dto)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        await setup.Sut.NotifyOrderPlaced(dto, NotificationTarget.Customer);

        // Assert
        setup.CustomerHubClients.Verify(c => c.Group($"order:{orderId}"), Times.Once);
        setup.CustomerClientProxy.Verify();
        setup.RestaurantHubClients.Verify(c => c.Group(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task NotifyOrderPlaced_Both_Target_Sends_To_Both_Hubs()
    {
        // Arrange
        var restaurantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var setup = CreateSut(restaurantId, orderId);
        var dto = MakeDto(restaurantId, orderId);

        setup.RestaurantClientProxy
            .Setup(p => p.SendCoreAsync(
                "ReceiveOrderPlaced",
                It.Is<object?[]>(a => MatchesDtoPayload(a, dto)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        setup.CustomerClientProxy
            .Setup(p => p.SendCoreAsync(
                "ReceiveOrderPlaced",
                It.Is<object?[]>(a => MatchesDtoPayload(a, dto)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        await setup.Sut.NotifyOrderPlaced(dto, NotificationTarget.Both);

        // Assert
        setup.RestaurantHubClients.Verify(c => c.Group($"restaurant:{restaurantId}"), Times.Once);
        setup.CustomerHubClients.Verify(c => c.Group($"order:{orderId}"), Times.Once);
        setup.RestaurantClientProxy.Verify();
        setup.CustomerClientProxy.Verify();
    }

    [Test]
    public async Task NotifyOrderPaymentSucceeded_Both_Target_Sends_To_Both_Hubs()
    {
        // Arrange
        var restaurantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var setup = CreateSut(restaurantId, orderId);
        var dto = MakeDto(restaurantId, orderId);

        setup.RestaurantClientProxy
            .Setup(p => p.SendCoreAsync(
                "ReceiveOrderPaymentSucceeded",
                It.Is<object?[]>(a => MatchesDtoPayload(a, dto)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        setup.CustomerClientProxy
            .Setup(p => p.SendCoreAsync(
                "ReceiveOrderPaymentSucceeded",
                It.Is<object?[]>(a => MatchesDtoPayload(a, dto)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        await setup.Sut.NotifyOrderPaymentSucceeded(dto, NotificationTarget.Both);

        // Assert
        setup.RestaurantHubClients.Verify(c => c.Group($"restaurant:{restaurantId}"), Times.Once);
        setup.CustomerHubClients.Verify(c => c.Group($"order:{orderId}"), Times.Once);
        setup.RestaurantClientProxy.Verify();
        setup.CustomerClientProxy.Verify();
    }

    [Test]
    public async Task NotifyOrderStatusChanged_Customer_Target_Sends_To_Customer_Only()
    {
        // Arrange
        var restaurantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var setup = CreateSut(restaurantId, orderId);
        var dto = MakeDto(restaurantId, orderId);

        setup.CustomerClientProxy
            .Setup(p => p.SendCoreAsync(
                "ReceiveOrderStatusChanged",
                It.Is<object?[]>(a => MatchesDtoPayload(a, dto)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        await setup.Sut.NotifyOrderStatusChanged(dto, NotificationTarget.Customer);

        // Assert
        setup.CustomerHubClients.Verify(c => c.Group($"order:{orderId}"), Times.Once);
        setup.CustomerClientProxy.Verify();
        setup.RestaurantHubClients.Verify(c => c.Group(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task NotifyOrderPaymentFailed_Customer_Target_Sends_To_Customer_Only()
    {
        // Arrange
        var restaurantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var setup = CreateSut(restaurantId, orderId);
        var dto = MakeDto(restaurantId, orderId);

        setup.CustomerClientProxy
            .Setup(p => p.SendCoreAsync(
                "ReceiveOrderPaymentFailed",
                It.Is<object?[]>(a => MatchesDtoPayload(a, dto)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        await setup.Sut.NotifyOrderPaymentFailed(dto, NotificationTarget.Customer);

        // Assert
        setup.CustomerHubClients.Verify(c => c.Group($"order:{orderId}"), Times.Once);
        setup.CustomerClientProxy.Verify();
        setup.RestaurantHubClients.Verify(c => c.Group(It.IsAny<string>()), Times.Never);
    }
}
