using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.CreateTeamCart;
using YummyZoom.Application.TeamCarts.Commands.RemoveItemFromTeamCart;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Infrastructure.Data;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.Events;

public class ItemRemovedFromTeamCartEventHandlerTests : BaseTestFixture
{
    [Test]
    public async Task RemoveItem_Should_RemoveFromStore_And_Notify()
    {
        await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host User"));
        create.IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();

        // Add item and process add event so VM contains it
        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(create.Value.TeamCartId, burgerId, 1))).IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();

        // Get the item id
        Guid itemId;
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            itemId = await db.TeamCarts
                .Where(c => c.Id == TeamCartId.Create(create.Value.TeamCartId))
                .SelectMany(c => c.Items.Select(i => i.Id.Value))
                .FirstAsync();
        }

        var notifierMock = new Mock<ITeamCartRealtimeNotifier>(MockBehavior.Strict);
        notifierMock
            .Setup(n => n.NotifyCartUpdated(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        ReplaceService<ITeamCartRealtimeNotifier>(notifierMock.Object);

        // Remove the item
        (await SendAsync(new RemoveItemFromTeamCartCommand(create.Value.TeamCartId, itemId))).IsSuccess.Should().BeTrue();

        await DrainOutboxAsync();

        var store = GetService<ITeamCartStore>();
        var vm = await store.GetVmAsync(TeamCartId.Create(create.Value.TeamCartId));
        vm.Should().NotBeNull();
        vm!.Items.Should().BeEmpty();

        notifierMock.Verify(n => n.NotifyCartUpdated(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()), Times.Once);

        await DrainOutboxAsync();
        notifierMock.Verify(n => n.NotifyCartUpdated(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

