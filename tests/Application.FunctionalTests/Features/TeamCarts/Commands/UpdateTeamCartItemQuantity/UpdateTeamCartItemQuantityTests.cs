using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Infrastructure;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.CreateTeamCart;
using YummyZoom.Application.TeamCarts.Commands.UpdateTeamCartItemQuantity;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.Commands.UpdateTeamCartItemQuantity;

public class UpdateTeamCartItemQuantityTests : BaseTestFixture
{
    [Test]
    public async Task UpdateQuantity_HappyPath_Owner_UpdatesSuccessfully()
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

        // Update quantity
        var update = await SendAsync(new UpdateTeamCartItemQuantityCommand(
            TeamCartId: cartId,
            TeamCartItemId: itemId,
            NewQuantity: 3));
        update.IsSuccess.Should().BeTrue();

        // Verify persisted via projection
        var quantities = await db.TeamCarts
            .Where(c => c.Id == TeamCartId.Create(cartId))
            .SelectMany(c => c.Items.Select(i => new { i.Id.Value, i.Quantity }))
            .ToListAsync();
        quantities.Should().Contain(x => x.Value == itemId && x.Quantity == 3);
    }

    [Test]
    public async Task UpdateQuantity_InvalidQuantity_ShouldFailValidation()
    {
        await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;

        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host"));
        var cartId = create.Value.TeamCartId;
        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        await SendAsync(new AddItemToTeamCartCommand(cartId, burgerId, 1));

        using var scope = TestInfrastructure.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var itemId = await db.TeamCarts
            .Where(c => c.Id == TeamCartId.Create(cartId))
            .SelectMany(c => c.Items.Select(i => i.Id.Value))
            .FirstAsync();

        var cmd = new UpdateTeamCartItemQuantityCommand(cartId, itemId, 0);
        await FluentActions.Invoking(() => SendAsync(cmd))
            .Should().ThrowAsync<YummyZoom.Application.Common.Exceptions.ValidationException>();
    }

    [Test]
    public async Task UpdateQuantity_NotOwner_ShouldFail()
    {
        // User A creates cart and adds item
        await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host"));
        var cartId = create.Value.TeamCartId;
        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(cartId, burgerId, 1))).IsSuccess.Should().BeTrue();

        // Locate itemId
        using var scope = TestInfrastructure.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var itemId = await db.TeamCarts
            .Where(c => c.Id == TeamCartId.Create(cartId))
            .SelectMany(c => c.Items.Select(i => i.Id.Value))
            .FirstAsync();

        // Switch to another user (not owner)
        var otherUserId = await CreateUserAsync("other-user@example.com", "Password123!");
        SetUserId(otherUserId);

        var update = await SendAsync(new UpdateTeamCartItemQuantityCommand(cartId, itemId, 2));
        update.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task UpdateQuantity_CartLocked_ShouldFail()
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

        var update = await SendAsync(new UpdateTeamCartItemQuantityCommand(cartId, itemId, 2));
        update.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task UpdateQuantity_ItemNotFound_ShouldFail()
    {
        await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host"));
        var cartId = create.Value.TeamCartId;

        var missingItemId = Guid.NewGuid();
        var update = await SendAsync(new UpdateTeamCartItemQuantityCommand(cartId, missingItemId, 2));
        update.IsFailure.Should().BeTrue();
    }
}
