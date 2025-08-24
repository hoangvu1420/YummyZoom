using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
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

    private static (SignalROrderRealtimeNotifier sut, Mock<IHubClients> hubClients, Mock<IClientProxy> clientProxy) CreateSut(Guid restaurantId)
    {
        var hubContext = new Mock<IHubContext<RestaurantOrdersHub>>(MockBehavior.Strict);
        var hubClients = new Mock<IHubClients>(MockBehavior.Strict);
        var clientProxy = new Mock<IClientProxy>(MockBehavior.Strict);
        var logger = new Mock<ILogger<SignalROrderRealtimeNotifier>>();

        var expectedGroup = $"restaurant:{restaurantId}";
        hubContext.SetupGet(h => h.Clients).Returns(hubClients.Object);
        hubClients.Setup(c => c.Group(expectedGroup)).Returns(clientProxy.Object);

        var sut = new SignalROrderRealtimeNotifier(hubContext.Object, logger.Object);
        return (sut, hubClients, clientProxy);
    }

    private static OrderStatusBroadcastDto MakeDto(Guid restaurantId)
    {
        return new OrderStatusBroadcastDto(
            OrderId: Guid.NewGuid(),
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
    public async Task NotifyOrderPlaced_sends_expected_hub_call()
    {
        var restaurantId = Guid.NewGuid();
        var (sut, hubClients, clientProxy) = CreateSut(restaurantId);
        var dto = MakeDto(restaurantId);

        clientProxy
            .Setup(p => p.SendCoreAsync(
                "ReceiveOrderPlaced",
                It.Is<object?[]>(a => MatchesDtoPayload(a, dto)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        await sut.NotifyOrderPlaced(dto, default);

        hubClients.Verify(c => c.Group($"restaurant:{restaurantId}"), Times.Once);
        clientProxy.Verify();
    }

    [Test]
    public async Task NotifyOrderPaymentSucceeded_sends_expected_hub_call()
    {
        var restaurantId = Guid.NewGuid();
        var (sut, hubClients, clientProxy) = CreateSut(restaurantId);
        var dto = MakeDto(restaurantId);

        clientProxy
            .Setup(p => p.SendCoreAsync(
                "ReceiveOrderPaymentSucceeded",
                It.Is<object?[]>(a => MatchesDtoPayload(a, dto)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        await sut.NotifyOrderPaymentSucceeded(dto, default);

        hubClients.Verify(c => c.Group($"restaurant:{restaurantId}"), Times.Once);
        clientProxy.Verify();
    }

    [Test]
    public async Task NotifyOrderPaymentFailed_sends_expected_hub_call()
    {
        var restaurantId = Guid.NewGuid();
        var (sut, hubClients, clientProxy) = CreateSut(restaurantId);
        var dto = MakeDto(restaurantId);

        clientProxy
            .Setup(p => p.SendCoreAsync(
                "ReceiveOrderPaymentFailed",
                It.Is<object?[]>(a => MatchesDtoPayload(a, dto)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        await sut.NotifyOrderPaymentFailed(dto, default);

        hubClients.Verify(c => c.Group($"restaurant:{restaurantId}"), Times.Once);
        clientProxy.Verify();
    }

    [Test]
    public async Task NotifyOrderStatusChanged_sends_expected_hub_call()
    {
        var restaurantId = Guid.NewGuid();
        var (sut, hubClients, clientProxy) = CreateSut(restaurantId);
        var dto = MakeDto(restaurantId);

        clientProxy
            .Setup(p => p.SendCoreAsync(
                "ReceiveOrderStatusChanged",
                It.Is<object?[]>(a => MatchesDtoPayload(a, dto)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        await sut.NotifyOrderStatusChanged(dto, default);

        hubClients.Verify(c => c.Group($"restaurant:{restaurantId}"), Times.Once);
        clientProxy.Verify();
    }
}
