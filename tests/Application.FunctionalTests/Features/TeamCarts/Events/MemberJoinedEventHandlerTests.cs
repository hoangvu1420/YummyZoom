using FluentAssertions;
using Moq;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.TeamCarts.Commands.CreateTeamCart;
using YummyZoom.Application.TeamCarts.Commands.JoinTeamCart;
using YummyZoom.Application.TeamCarts.Models;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.Events;

public class MemberJoinedEventHandlerTests : BaseTestFixture
{
    [Test]
    public async Task JoinTeamCart_Should_AddMember_ToStore_And_Notify()
    {
        // Host creates cart and initial VM is created by TeamCartCreated handler
        var hostUserId = await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host User"));
        create.IsSuccess.Should().BeTrue();

        // Drain outbox to process TeamCartCreated using default notifier (not under test)
        await DrainOutboxAsync();

        // Prepare a mock notifier only for the MemberJoined step
        var notifierMock = new Mock<ITeamCartRealtimeNotifier>(MockBehavior.Strict);
        notifierMock
            .Setup(n => n.NotifyCartUpdated(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        ReplaceService<ITeamCartRealtimeNotifier>(notifierMock.Object);

        // Guest joins
        var guestUserId = await CreateUserAsync("guest-memberjoined@example.com", "Password123!");
        SetUserId(guestUserId);
        var join = await SendAsync(new JoinTeamCartCommand(create.Value.TeamCartId, create.Value.ShareToken, "Bob Guest"));
        join.IsSuccess.Should().BeTrue();

        // Drain outbox to process MemberJoined
        await DrainOutboxAsync();

        // Assert VM updated in Redis store
        var store = GetService<ITeamCartStore>();
        var vm = await store.GetVmAsync(TeamCartId.Create(create.Value.TeamCartId));
        vm.Should().NotBeNull();
        vm!.Members.Should().Contain(m => m.UserId == guestUserId && m.Name == "Bob Guest" && m.Role == "Guest");

        // Assert notifier called exactly once for the join update
        notifierMock.Verify(n => n.NotifyCartUpdated(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()), Times.Once);

        // Idempotent re-drain should not duplicate
        await DrainOutboxAsync();
        notifierMock.Verify(n => n.NotifyCartUpdated(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

