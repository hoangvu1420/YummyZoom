using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.FunctionalTests.Common;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Web.SignalR;

[TestFixture]
public class SignalRDiWiringTests : BaseTestFixture
{
    [Test]
    public void Resolves_SignalR_Notifier_From_DI()
    {
        using var scope = CreateScope();
        var notifier = scope.ServiceProvider.GetRequiredService<IOrderRealtimeNotifier>();

        notifier.Should().BeOfType<YummyZoom.Web.Realtime.SignalROrderRealtimeNotifier>();
    }
}
