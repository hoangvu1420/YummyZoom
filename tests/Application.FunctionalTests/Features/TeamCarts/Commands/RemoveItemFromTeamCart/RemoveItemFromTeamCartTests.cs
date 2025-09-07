using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Infrastructure;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.CreateTeamCart;
using YummyZoom.Application.TeamCarts.Commands.RemoveItemFromTeamCart;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Infrastructure.Data;

using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.Commands.RemoveItemFromTeamCart;

public class RemoveItemFromTeamCartTests : BaseTestFixture
{
    [Test]
    public async Task RemoveItem_HappyPath_Owner_RemovesSuccessfully()
    {
        var userId = await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;

        // Create cart
        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host"));
        create.IsSuccess.Should().BeTrue();
        var cartId = create.Value.TeamCartId;

        // Add item
        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        var add = await SendAsync(new AddItemToTeamCartCommand(cartId, burgerId, 1));
        add.IsSuccess.Should().BeTrue();

        // Find the itemId
        using var scope = TestInfrastructure.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var itemId = await db.TeamCarts
            .Where(c => c.Id == TeamCartId.Create(cartId))
            .SelectMany(c => c.Items.Select(i => i.Id.Value))
            .FirstAsync();

        // Remove item
        var remove = await SendAsync(new RemoveItemFromTeamCartCommand(
            TeamCartId: cartId,
            TeamCartItemId: itemId));
        remove.IsSuccess.Should().BeTrue();

        // Verify item no longer exists
        var remainingItemIds = await db.TeamCarts
            .Where(c => c.Id == TeamCartId.Create(cartId))
            .SelectMany(c => c.Items.Select(i => i.Id.Value))
            .ToListAsync();
        remainingItemIds.Should().NotContain(itemId);
    }

    [Test]
    public async Task RemoveItem_NotOwner_ShouldFail()
    {
        await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host"));
        var cartId = create.Value.TeamCartId;
        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(cartId, burgerId, 1))).IsSuccess.Should().BeTrue();

        using var scope = TestInfrastructure.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var itemId = await db.TeamCarts
            .Where(c => c.Id == TeamCartId.Create(cartId))
            .SelectMany(c => c.Items.Select(i => i.Id.Value))
            .FirstAsync();

        // Switch to another user
        var otherUserId = await CreateUserAsync("remove-notowner@example.com", "Password123!");
        SetUserId(otherUserId);

        var remove = await SendAsync(new RemoveItemFromTeamCartCommand(cartId, itemId));
        remove.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task RemoveItem_CartLocked_ShouldFail()
    {
        await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host"));
        var cartId = create.Value.TeamCartId;
        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(cartId, burgerId, 1))).IsSuccess.Should().BeTrue();

        // Lock the cart directly via EF and domain method
        using (var scope = TestInfrastructure.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var cart = await db.TeamCarts.Include(c => c.Items).FirstAsync(c => c.Id == TeamCartId.Create(cartId));
            cart.LockForPayment(cart.HostUserId);
            await db.SaveChangesAsync();
        }

        // Get the item id again
        using var scope2 = TestInfrastructure.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var itemId = await db2.TeamCarts
            .Where(c => c.Id == TeamCartId.Create(cartId))
            .SelectMany(c => c.Items.Select(i => i.Id.Value))
            .FirstAsync();

        var remove = await SendAsync(new RemoveItemFromTeamCartCommand(cartId, itemId));
        remove.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task RemoveItem_ItemNotFound_ShouldFail()
    {
        await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host"));
        var cartId = create.Value.TeamCartId;

        var missingItemId = Guid.NewGuid();
        var remove = await SendAsync(new RemoveItemFromTeamCartCommand(cartId, missingItemId));
        remove.IsFailure.Should().BeTrue();
    }
}

