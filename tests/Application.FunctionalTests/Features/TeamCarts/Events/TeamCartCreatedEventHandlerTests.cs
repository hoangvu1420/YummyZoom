using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.FunctionalTests.Authorization;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.Events;

/// <summary>
/// Functional tests for TeamCartCreatedEventHandler verifying:
/// 1) Outbox -> handler creates VM in ITeamCartStore and
/// 2) Notifies via ITeamCartRealtimeNotifier once (idempotent on re-drain).
/// </summary>
public class TeamCartCreatedEventHandlerTests : BaseTestFixture
{
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        await TeamCartRoleTestHelper.SetupTeamCartAuthorizationTestsAsync();
    }

    [Test]
    public async Task CreateTeamCart_Should_CreateVm_And_NotifyRealtime()
    {
        // Arrange: Create team cart scenario with host using builder
        var scenario = await TeamCartTestBuilder.Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Alice Host")
            .BuildAsync();

        // Use real Redis-backed store from test infrastructure; only notifier is mocked
        var notifierMock = new Mock<ITeamCartRealtimeNotifier>(MockBehavior.Strict);
        notifierMock
            .Setup(n => n.NotifyCartUpdated(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        ReplaceService<ITeamCartRealtimeNotifier>(notifierMock.Object);

        // Act: Drain outbox to process TeamCartCreated
        await DrainOutboxAsync();

        // Assert: VM actually created in Redis store and notifier called once
        var store = GetService<ITeamCartStore>();
        var vm = await store.GetVmAsync(TeamCartId.Create(scenario.TeamCartId));
        vm.Should().NotBeNull();
        vm!.CartId.Value.Should().Be(scenario.TeamCartId);
        vm.RestaurantId.Should().Be(Testing.TestData.DefaultRestaurantId);
        vm.Status.ToString().Should().Be("Open");
        vm.Version.Should().BeGreaterThanOrEqualTo(1);
        notifierMock.Verify(n => n.NotifyCartUpdated(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()), Times.Once);

        // Idempotency: re-drain should not duplicate
        await DrainOutboxAsync();
        notifierMock.Verify(n => n.NotifyCartUpdated(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
