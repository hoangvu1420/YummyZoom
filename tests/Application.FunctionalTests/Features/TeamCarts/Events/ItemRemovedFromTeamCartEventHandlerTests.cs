using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.FunctionalTests.Authorization;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.RemoveItemFromTeamCart;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.Events;

public class ItemRemovedFromTeamCartEventHandlerTests : BaseTestFixture
{
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        await TeamCartRoleTestHelper.SetupTeamCartAuthorizationTestsAsync();
    }

    [Test]
    public async Task RemoveItem_Should_RemoveFromStore_And_Notify()
    {
        // Arrange: Create team cart scenario with host using builder
        var scenario = await TeamCartTestBuilder.Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .BuildAsync();

        await DrainOutboxAsync(); // process TeamCartCreated -> create VM

        // Add item and process add event so VM contains it (as host)
        await scenario.ActAsHost();
        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 1))).IsSuccess.Should().BeTrue();
        await DrainOutboxAsync();

        // Get the item id
        Guid itemId;
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            itemId = await db.TeamCarts
                .Where(c => c.Id == TeamCartId.Create(scenario.TeamCartId))
                .SelectMany(c => c.Items.Select(i => i.Id.Value))
                .FirstAsync();
        }

        var notifierMock = new Mock<ITeamCartRealtimeNotifier>(MockBehavior.Strict);
        notifierMock
            .Setup(n => n.NotifyCartUpdated(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        ReplaceService<ITeamCartRealtimeNotifier>(notifierMock.Object);

        // Act: Remove the item (as host)
        (await SendAsync(new RemoveItemFromTeamCartCommand(scenario.TeamCartId, itemId))).IsSuccess.Should().BeTrue();

        await DrainOutboxAsync();

        // Assert: VM should have no items
        var store = GetService<ITeamCartStore>();
        var vm = await store.GetVmAsync(TeamCartId.Create(scenario.TeamCartId));
        vm.Should().NotBeNull();
        vm!.Items.Should().BeEmpty();

        notifierMock.Verify(n => n.NotifyCartUpdated(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()), Times.Once);

        await DrainOutboxAsync();
        notifierMock.Verify(n => n.NotifyCartUpdated(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

