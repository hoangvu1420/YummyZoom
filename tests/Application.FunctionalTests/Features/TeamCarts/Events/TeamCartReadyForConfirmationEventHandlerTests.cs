
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.CommitToCodPayment;
using YummyZoom.Application.TeamCarts.Commands.CreateTeamCart;
using YummyZoom.Application.TeamCarts.Commands.LockTeamCartForPayment;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.Events;

public class TeamCartReadyForConfirmationEventHandlerTests : BaseTestFixture
{
    [Test]
    public async Task ReadyForConfirmation_Should_Notify()
    {
        await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;

        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host"));
        create.IsSuccess.Should().BeTrue();

        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(create.Value.TeamCartId, burgerId, 1))).IsSuccess.Should().BeTrue();
        (await SendAsync(new LockTeamCartForPaymentCommand(create.Value.TeamCartId))).IsSuccess.Should().BeTrue();

        var notifierMock = new Moq.Mock<ITeamCartRealtimeNotifier>(Moq.MockBehavior.Loose);
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

        // Commit COD to trigger ReadyToConfirm (single-member case)
        (await SendAsync(new CommitToCodPaymentCommand(create.Value.TeamCartId))).IsSuccess.Should().BeTrue();

        await DrainOutboxAsync();

        notifierMock.Verify(n => n.NotifyReadyToConfirm(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()), Times.Once);
        notifierMock.Verify(n => n.NotifyCartUpdated(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()), Moq.Times.AtLeastOnce());
    }
}
