using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.Web.Realtime;
using YummyZoom.Web.Realtime.Hubs;

namespace YummyZoom.Web.ApiContractTests.Realtime;

public class SignalRTeamCartRealtimeNotifierTests
{
    [Test]
    public async Task NotifyCartUpdated_SendsSignalRMessage()
    {
        var cartId = TeamCartId.Create(Guid.NewGuid());

        // Mock SignalR hub context
        var clientProxy = new Mock<IClientProxy>(MockBehavior.Strict);
        clientProxy
            .Setup(p => p.SendCoreAsync("ReceiveCartUpdated", It.IsAny<object?[]>(), default))
            .Returns(Task.CompletedTask);

        var hubClients = new Mock<IHubClients>(MockBehavior.Loose);
        hubClients.Setup(c => c.Group(It.IsAny<string>())).Returns(clientProxy.Object);

        var hubContext = new Mock<IHubContext<TeamCartHub>>(MockBehavior.Loose);
        hubContext.SetupGet(h => h.Clients).Returns(hubClients.Object);

        var logger = new Mock<ILogger<SignalRTeamCartRealtimeNotifier>>();
        var notifier = new SignalRTeamCartRealtimeNotifier(hubContext.Object, logger.Object);

        await notifier.NotifyCartUpdated(cartId);

        // Verify SignalR message was sent (FCM push is now handled explicitly in event handlers)
        clientProxy.Verify(p => p.SendCoreAsync("ReceiveCartUpdated", It.IsAny<object?[]>(), default), Times.Once);
    }
}
