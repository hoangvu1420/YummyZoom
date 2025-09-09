using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.FunctionalTests.Authorization;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.CommitToCodPayment;
using YummyZoom.Application.TeamCarts.Commands.LockTeamCartForPayment;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.Events;

public class TeamCartReadyForConfirmationEventHandlerTests : BaseTestFixture
{
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        await TeamCartRoleTestHelper.SetupTeamCartAuthorizationTestsAsync();
    }

    [Test]
    public async Task ReadyForConfirmation_Should_Notify()
    {
        // Arrange: Create team cart scenario with host using builder
        var scenario = await TeamCartTestBuilder.Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host")
            .BuildAsync();

        await DrainOutboxAsync(); // process TeamCartCreated -> create VM

        // Add item and lock cart (as host)
        await scenario.ActAsHost();
        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 1))).IsSuccess.Should().BeTrue();
        (await SendAsync(new LockTeamCartForPaymentCommand(scenario.TeamCartId))).IsSuccess.Should().BeTrue();

        var notifierMock = new Mock<ITeamCartRealtimeNotifier>(MockBehavior.Loose);
        notifierMock
            .Setup(n => n.NotifyReadyToConfirm(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notifierMock
            .Setup(n => n.NotifyCartUpdated(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notifierMock
            .Setup(n => n.NotifyPaymentEvent(It.IsAny<TeamCartId>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        ReplaceService<ITeamCartRealtimeNotifier>(notifierMock.Object);

        // Act: Commit COD to trigger ReadyToConfirm (single-member case, as host)
        (await SendAsync(new CommitToCodPaymentCommand(scenario.TeamCartId))).IsSuccess.Should().BeTrue();

        await DrainOutboxAsync();

        // Assert: Verify notifications
        notifierMock.Verify(n => n.NotifyReadyToConfirm(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()), Times.Once);
        notifierMock.Verify(n => n.NotifyCartUpdated(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce());
    }
}
