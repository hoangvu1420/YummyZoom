using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.CreateTeamCart;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.Events;

public class ItemAddedToTeamCartEventHandlerTests : BaseTestFixture
{
    [Test]
    public async Task AddItem_Should_AddItem_ToStore_And_Notify()
    {
        var userId = await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;

        // Create cart and process TeamCartCreated
        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host User"));
        create.IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();

        // Mock notifier for this test
        var notifierMock = new Mock<ITeamCartRealtimeNotifier>(MockBehavior.Strict);
        notifierMock
            .Setup(n => n.NotifyCartUpdated(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        ReplaceService<ITeamCartRealtimeNotifier>(notifierMock.Object);

        // Add item (no customizations) — use Classic Burger
        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        var add = await SendAsync(new AddItemToTeamCartCommand(create.Value.TeamCartId, burgerId, 1));
        add.IsSuccess.Should().BeTrue();

        // Process ItemAddedToTeamCart
        await DrainOutboxAsync();

        // Assert VM has the item
        var store = GetService<ITeamCartStore>();
        var vm = await store.GetVmAsync(TeamCartId.Create(create.Value.TeamCartId));
        vm.Should().NotBeNull();
        vm!.Items.Should().HaveCount(1);
        var item = vm.Items.Single();
        item.AddedByUserId.Should().Be(userId);
        item.Name.Should().Be(Testing.TestData.MenuItems.ClassicBurger);
        item.Quantity.Should().Be(1);
        item.BasePrice.Should().BeGreaterThan(0);
        item.LineTotal.Should().Be(item.BasePrice * item.Quantity);
        item.Customizations.Should().BeEmpty();

        notifierMock.Verify(n => n.NotifyCartUpdated(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()), Times.Once);

        // Idempotency: re-drain should not duplicate
        await DrainOutboxAsync();
        notifierMock.Verify(n => n.NotifyCartUpdated(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

