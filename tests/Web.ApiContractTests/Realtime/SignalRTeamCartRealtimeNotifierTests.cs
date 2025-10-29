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
    public async Task NotifyCartUpdated_EmitsFcmPush()
    {
        var cartId = TeamCartId.Create(Guid.NewGuid());

        // Mock SignalR hub context to no-op
        var clientProxy = new Mock<IClientProxy>(MockBehavior.Loose);
        clientProxy
            .Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), default))
            .Returns(Task.CompletedTask);

        var hubClients = new Mock<IHubClients>(MockBehavior.Loose);
        hubClients.Setup(c => c.Group(It.IsAny<string>())).Returns(clientProxy.Object);

        var hubContext = new Mock<IHubContext<TeamCartHub>>(MockBehavior.Loose);
        hubContext.SetupGet(h => h.Clients).Returns(hubClients.Object);

        // Build a minimal scoped provider that returns our push mock
        var push = new Mock<ITeamCartPushNotifier>(MockBehavior.Strict);
        push.Setup(p => p.PushTeamCartDataAsync(cartId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        var services = new ServiceCollection();
        services.AddScoped<ITeamCartPushNotifier>(_ => push.Object);
        var provider = services.BuildServiceProvider();

        var logger = new Mock<ILogger<SignalRTeamCartRealtimeNotifier>>();
        var notifier = new SignalRTeamCartRealtimeNotifier(hubContext.Object, provider, logger.Object);

        await notifier.NotifyCartUpdated(cartId);

        push.Verify(p => p.PushTeamCartDataAsync(cartId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
