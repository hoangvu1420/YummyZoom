using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.FunctionalTests.Authorization;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.LockTeamCartForPayment;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.Events;

public class TeamCartLockedForPaymentEventHandlerTests : BaseTestFixture
{
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        await TeamCartRoleTestHelper.SetupTeamCartAuthorizationTestsAsync();
    }

    [Test]
    public async Task LockCart_Should_SetVmLocked_And_Notify()
    {
        // Arrange: Create team cart scenario with host using builder
        var scenario = await TeamCartTestBuilder.Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .BuildAsync();

        // Create the team cart and process creation event
        await DrainOutboxAsync();

        // Add at least one item to allow locking (as host)
        await scenario.ActAsHost();
        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 1))).IsSuccess.Should().BeTrue();
        await DrainOutboxAsync(); // ensure ItemAdded event processed before we start verifying notifications

        // Mock notifier for expectations
        var notifierMock = new Mock<ITeamCartRealtimeNotifier>(MockBehavior.Strict);
        notifierMock
            .Setup(n => n.NotifyLocked(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notifierMock
            .Setup(n => n.NotifyCartUpdated(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        ReplaceService<ITeamCartRealtimeNotifier>(notifierMock.Object);

        // Act: Lock the cart (domain raises TeamCartLockedForPayment, as host)
        (await SendAsync(new LockTeamCartForPaymentCommand(scenario.TeamCartId))).IsSuccess.Should().BeTrue();

        await DrainOutboxAsync();

        // Assert: VM status is Locked
        var store = GetService<ITeamCartStore>();
        var vm = await store.GetVmAsync(TeamCartId.Create(scenario.TeamCartId));
        vm.Should().NotBeNull();
        vm!.Status.Should().Be(TeamCartStatus.Locked);

        // Assert notifications
        // Expect 1 NotifyLocked and 2 NotifyCartUpdated (one from TeamCartLockedForPayment, one from TeamCartQuoteUpdated)
        notifierMock.Verify(n => n.NotifyLocked(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()), Times.Once);
        notifierMock.Verify(n => n.NotifyCartUpdated(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
