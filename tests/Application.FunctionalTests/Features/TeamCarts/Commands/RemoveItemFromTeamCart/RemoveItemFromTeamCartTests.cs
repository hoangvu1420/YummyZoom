using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Authorization;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Infrastructure;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.RemoveItemFromTeamCart;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.Commands.RemoveItemFromTeamCart;

public class RemoveItemFromTeamCartTests : BaseTestFixture
{
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        await TeamCartRoleTestHelper.SetupTeamCartAuthorizationTestsAsync();
    }

    [Test]
    public async Task RemoveItem_HappyPath_Owner_RemovesSuccessfully()
    {
        // Arrange: Create team cart scenario with host
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host")
            .BuildAsync();

        // Add item as host
        await scenario.ActAsHost();
        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        var add = await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 1));
        add.IsSuccess.Should().BeTrue();

        // Find the itemId
        using var scope = TestInfrastructure.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var itemId = await db.TeamCarts
            .Where(c => c.Id == TeamCartId.Create(scenario.TeamCartId))
            .SelectMany(c => c.Items.Select(i => i.Id.Value))
            .FirstAsync();

        // Act: Remove item as host (owner of the item)
        var remove = await SendAsync(new RemoveItemFromTeamCartCommand(
            TeamCartId: scenario.TeamCartId,
            TeamCartItemId: itemId));

        // Assert
        remove.IsSuccess.Should().BeTrue();

        // Verify item no longer exists
        var remainingItemIds = await db.TeamCarts
            .Where(c => c.Id == TeamCartId.Create(scenario.TeamCartId))
            .SelectMany(c => c.Items.Select(i => i.Id.Value))
            .ToListAsync();
        remainingItemIds.Should().NotContain(itemId);
    }

    [Test]
    public async Task RemoveItem_NotOwner_ButMember_ShouldFail()
    {
        // Arrange: Create team cart scenario with host and guest
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host")
            .WithGuest("Guest User")
            .BuildAsync();

        // Add item as host
        await scenario.ActAsHost();
        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 1))).IsSuccess.Should().BeTrue();

        // Find the itemId added by host
        using var scope = TestInfrastructure.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var itemId = await db.TeamCarts
            .Where(c => c.Id == TeamCartId.Create(scenario.TeamCartId))
            .SelectMany(c => c.Items.Select(i => i.Id.Value))
            .FirstAsync();

        // Act: Try to remove host's item as guest (member but not owner of the item)
        await scenario.ActAsGuest("Guest User");
        var remove = await SendAsync(new RemoveItemFromTeamCartCommand(scenario.TeamCartId, itemId));

        // Assert: Should fail due to business logic (can only remove own items)
        remove.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task RemoveItem_CartLocked_ShouldFail()
    {
        // Arrange: Create team cart scenario with host
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host")
            .BuildAsync();

        // Add item and lock cart as host
        await scenario.ActAsHost();
        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 1))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(scenario.TeamCartId))).IsSuccess.Should().BeTrue();

        // Get the item id
        using var scope = TestInfrastructure.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var itemId = await db.TeamCarts
            .Where(c => c.Id == TeamCartId.Create(scenario.TeamCartId))
            .SelectMany(c => c.Items.Select(i => i.Id.Value))
            .FirstAsync();

        // Act: Try to remove item from locked cart as host (even though owner)
        var remove = await SendAsync(new RemoveItemFromTeamCartCommand(scenario.TeamCartId, itemId));

        // Assert: Should fail due to business logic (cannot modify locked cart)
        remove.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task RemoveItem_ItemNotFound_ShouldFail()
    {
        // Arrange: Create team cart scenario with host
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host")
            .BuildAsync();

        // Act: Try to remove non-existent item as host
        await scenario.ActAsHost();
        var missingItemId = Guid.NewGuid();
        var remove = await SendAsync(new RemoveItemFromTeamCartCommand(scenario.TeamCartId, missingItemId));

        // Assert: Should fail due to business logic (item not found)
        remove.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task RemoveItem_Guest_RemovesOwnItem_ShouldSucceed()
    {
        // Arrange: Create team cart scenario with host and guest
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host")
            .WithGuest("Guest User")
            .BuildAsync();

        // Add item as guest
        await scenario.ActAsGuest("Guest User");
        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        var add = await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 1));
        add.IsSuccess.Should().BeTrue();

        // Find the itemId added by guest
        using var scope = TestInfrastructure.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var itemId = await db.TeamCarts
            .Where(c => c.Id == TeamCartId.Create(scenario.TeamCartId))
            .SelectMany(c => c.Items.Select(i => i.Id.Value))
            .FirstAsync();

        // Act: Remove own item as guest
        var remove = await SendAsync(new RemoveItemFromTeamCartCommand(scenario.TeamCartId, itemId));

        // Assert: Should succeed (guest can remove their own items)
        remove.IsSuccess.Should().BeTrue();

        // Verify item no longer exists
        var remainingItemIds = await db.TeamCarts
            .Where(c => c.Id == TeamCartId.Create(scenario.TeamCartId))
            .SelectMany(c => c.Items.Select(i => i.Id.Value))
            .ToListAsync();
        remainingItemIds.Should().NotContain(itemId);
    }

    [Test]
    public async Task RemoveItem_NonMember_ShouldFail()
    {
        // Arrange: Create team cart scenario with host
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host")
            .BuildAsync();

        // Add item as host
        await scenario.ActAsHost();
        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 1))).IsSuccess.Should().BeTrue();

        // Find the itemId
        using var scope = TestInfrastructure.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var itemId = await db.TeamCarts
            .Where(c => c.Id == TeamCartId.Create(scenario.TeamCartId))
            .SelectMany(c => c.Items.Select(i => i.Id.Value))
            .FirstAsync();

        // Act: Try to remove item as non-member - authorization should fail at pipeline level
        await scenario.ActAsNonMember();

        // Assert: Should throw ForbiddenAccessException due to authorization policy
        await FluentActions.Invoking(() =>
                SendAsync(new RemoveItemFromTeamCartCommand(scenario.TeamCartId, itemId)))
            .Should().ThrowAsync<ForbiddenAccessException>();
    }
}

