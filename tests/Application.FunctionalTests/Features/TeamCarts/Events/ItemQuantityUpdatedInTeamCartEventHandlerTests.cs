using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.FunctionalTests.Authorization;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.UpdateTeamCartItemQuantity;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.Events;

public class ItemQuantityUpdatedInTeamCartEventHandlerTests : BaseTestFixture
{
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        await TeamCartRoleTestHelper.SetupTeamCartAuthorizationTestsAsync();
    }

    [Test]
    public async Task UpdateQuantity_Should_UpdateStore_And_Notify()
    {
        using (var scope = CreateScope())
        {
            var cleanupDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await cleanupDb.Database.ExecuteSqlRawAsync("DELETE FROM \"OutboxMessages\";");
        }

        // Arrange: Create team cart scenario with host using builder
        var scenario = await TeamCartTestBuilder.Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .BuildAsync();

        await DrainOutboxAsync(); // process TeamCartCreated -> create VM

        // Add item (as host)
        await scenario.ActAsHost();
        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 1))).IsSuccess.Should().BeTrue();
        await DrainOutboxAsync(); // process item added so VM has item

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

        // Mock notifier
        var notifierMock = new Mock<ITeamCartRealtimeNotifier>(MockBehavior.Strict);
        notifierMock
            .Setup(n => n.NotifyCartUpdated(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        ReplaceService<ITeamCartRealtimeNotifier>(notifierMock.Object);

        // Act: Update quantity (as host)
        (await SendAsync(new UpdateTeamCartItemQuantityCommand(scenario.TeamCartId, itemId, 3))).IsSuccess.Should().BeTrue();

        using (var scope = CreateScope())
        {
            var cleanupDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await cleanupDb.Database.ExecuteSqlRawAsync(
                "DELETE FROM \"OutboxMessages\" WHERE \"Content\"::text NOT LIKE {0};",
                $"%{scenario.TeamCartId}%");
        }

        await DrainOutboxAsync(); // process quantity updated

        // Assert: VM reflects updated quantity
        var store = GetService<ITeamCartStore>();
        var vm = await store.GetVmAsync(TeamCartId.Create(scenario.TeamCartId));
        vm.Should().NotBeNull();
        vm!.Items.Should().HaveCount(1);
        var item = vm.Items.Single();
        item.Quantity.Should().Be(3);
        item.LineTotal.Should().Be(item.BasePrice * 3);

        notifierMock.Verify(n => n.NotifyCartUpdated(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()), Times.Once);

        await DrainOutboxAsync();
        notifierMock.Verify(n => n.NotifyCartUpdated(It.IsAny<TeamCartId>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
