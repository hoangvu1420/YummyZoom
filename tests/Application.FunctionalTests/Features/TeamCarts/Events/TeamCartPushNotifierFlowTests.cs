using Moq;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.FunctionalTests.Authorization;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.JoinTeamCart;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.Events;

public class TeamCartPushNotifierFlowTests : BaseTestFixture
{
    private sealed class TestTeamCartRealtimeNotifier : ITeamCartRealtimeNotifier
    {
        private readonly ITeamCartPushNotifier _push;
        public TestTeamCartRealtimeNotifier(ITeamCartPushNotifier push) => _push = push;
        private Task FanOut(TeamCartId id, CancellationToken ct) => _push.PushTeamCartDataAsync(id, 1, TeamCartNotificationTarget.All, null, NotificationDeliveryType.Hybrid, ct); // Use dummy version for tests
        public Task NotifyCartUpdated(TeamCartId cartId, CancellationToken cancellationToken = default) => FanOut(cartId, cancellationToken);
        public Task NotifyLocked(TeamCartId cartId, CancellationToken cancellationToken = default) => FanOut(cartId, cancellationToken);
        public Task NotifyPaymentEvent(TeamCartId cartId, Guid userId, string status, CancellationToken cancellationToken = default) => FanOut(cartId, cancellationToken);
        public Task NotifyReadyToConfirm(TeamCartId cartId, CancellationToken cancellationToken = default) => FanOut(cartId, cancellationToken);
        public Task NotifyConverted(TeamCartId cartId, Guid orderId, CancellationToken cancellationToken = default) => FanOut(cartId, cancellationToken);
        public Task NotifyAllMembersReady(TeamCartId cartId, CancellationToken cancellationToken = default) => FanOut(cartId, cancellationToken);
        public Task NotifyExpired(TeamCartId cartId, CancellationToken cancellationToken = default) => FanOut(cartId, cancellationToken);
    }

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        await TeamCartRoleTestHelper.SetupTeamCartAuthorizationTestsAsync();
    }

    [Test]
    public async Task MemberJoined_Should_Emit_Push()
    {
        // Arrange minimal scenario: host only
        var scenario = await TeamCartTestBuilder.Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host")
            .BuildAsync();

        await DrainOutboxAsync(); // process TeamCartCreated -> VM created

        // Replace notifier with test wrapper that calls push; mock push to capture calls
        var pushMock = new Moq.Mock<ITeamCartPushNotifier>(MockBehavior.Strict);
        pushMock
            .Setup(p => p.PushTeamCartDataAsync(It.IsAny<TeamCartId>(), It.IsAny<long>(), It.IsAny<TeamCartNotificationTarget>(), It.IsAny<TeamCartNotificationContext>(), It.IsAny<NotificationDeliveryType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(YummyZoom.SharedKernel.Result.Success());
        ReplaceService<ITeamCartPushNotifier>(pushMock.Object);
        ReplaceService<ITeamCartRealtimeNotifier, TestTeamCartRealtimeNotifier>();

        // Act: a guest joins
        var guestUserId = await CreateUserAsync("guest@example.com", "Password123!");
        SetUserId(guestUserId);
        (await SendAsync(new JoinTeamCartCommand(scenario.TeamCartId, scenario.ShareToken, "Guest"))).IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();

        // Assert push was invoked
        pushMock.Verify(p => p.PushTeamCartDataAsync(It.Is<TeamCartId>(id => id.Value == scenario.TeamCartId), It.IsAny<long>(), It.IsAny<TeamCartNotificationTarget>(), It.IsAny<TeamCartNotificationContext>(), It.IsAny<NotificationDeliveryType>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Test]
    public async Task ItemAdded_Should_Emit_Push()
    {
        var scenario = await TeamCartTestBuilder.Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host")
            .BuildAsync();

        await DrainOutboxAsync();

        var pushMock = new Moq.Mock<ITeamCartPushNotifier>(MockBehavior.Strict);
        pushMock
            .Setup(p => p.PushTeamCartDataAsync(It.IsAny<TeamCartId>(), It.IsAny<long>(), It.IsAny<TeamCartNotificationTarget>(), It.IsAny<TeamCartNotificationContext>(), It.IsAny<NotificationDeliveryType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(YummyZoom.SharedKernel.Result.Success());
        ReplaceService<ITeamCartPushNotifier>(pushMock.Object);
        ReplaceService<ITeamCartRealtimeNotifier, TestTeamCartRealtimeNotifier>();

        // Act: host adds item
        await scenario.ActAsHost();
        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 1, null))).IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();

        // Assert push
        pushMock.Verify(p => p.PushTeamCartDataAsync(It.Is<TeamCartId>(id => id.Value == scenario.TeamCartId), It.IsAny<long>(), It.IsAny<TeamCartNotificationTarget>(), It.IsAny<TeamCartNotificationContext>(), It.IsAny<NotificationDeliveryType>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
