using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Authorization;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Infrastructure;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.UpdateTeamCartItemQuantity;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.Commands.UpdateTeamCartItemQuantity;

public class UpdateTeamCartItemQuantityTests : BaseTestFixture
{
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        await TeamCartRoleTestHelper.SetupTeamCartAuthorizationTestsAsync();
    }

    [Test]
    public async Task UpdateQuantity_HappyPath_Owner_UpdatesSuccessfully()
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

        // Act: Update quantity as host (owner of the item)
        var update = await SendAsync(new UpdateTeamCartItemQuantityCommand(
            TeamCartId: scenario.TeamCartId,
            TeamCartItemId: itemId,
            NewQuantity: 3));

        // Assert
        update.IsSuccess.Should().BeTrue();

        // Verify persisted via projection
        var quantities = await db.TeamCarts
            .Where(c => c.Id == TeamCartId.Create(scenario.TeamCartId))
            .SelectMany(c => c.Items.Select(i => new { i.Id.Value, i.Quantity }))
            .ToListAsync();
        quantities.Should().Contain(x => x.Value == itemId && x.Quantity == 3);
    }

    [Test]
    public async Task UpdateQuantity_InvalidQuantity_ShouldFailValidation()
    {
        // Arrange: Create team cart scenario with host
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host")
            .BuildAsync();

        // Add item as host
        await scenario.ActAsHost();
        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 1));

        // Find the itemId
        using var scope = TestInfrastructure.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var itemId = await db.TeamCarts
            .Where(c => c.Id == TeamCartId.Create(scenario.TeamCartId))
            .SelectMany(c => c.Items.Select(i => i.Id.Value))
            .FirstAsync();

        // Act & Assert: Try to update with invalid quantity (0) should fail validation
        var cmd = new UpdateTeamCartItemQuantityCommand(scenario.TeamCartId, itemId, 0);
        await FluentActions.Invoking(() => SendAsync(cmd))
            .Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task UpdateQuantity_NotOwner_ButMember_ShouldFail()
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

        // Act: Try to update host's item as guest (member but not owner of the item)
        await scenario.ActAsGuest("Guest User");
        var update = await SendAsync(new UpdateTeamCartItemQuantityCommand(scenario.TeamCartId, itemId, 2));

        // Assert: Should fail due to business logic (can only update own items)
        update.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task UpdateQuantity_CartLocked_ShouldFail()
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

        // Act: Try to update quantity in locked cart as host (even though owner)
        var update = await SendAsync(new UpdateTeamCartItemQuantityCommand(scenario.TeamCartId, itemId, 2));

        // Assert: Should fail due to business logic (cannot modify locked cart)
        update.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task UpdateQuantity_ItemNotFound_ShouldFail()
    {
        // Arrange: Create team cart scenario with host
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host")
            .BuildAsync();

        // Act: Try to update non-existent item as host
        await scenario.ActAsHost();
        var missingItemId = Guid.NewGuid();
        var update = await SendAsync(new UpdateTeamCartItemQuantityCommand(scenario.TeamCartId, missingItemId, 2));

        // Assert: Should fail due to business logic (item not found)
        update.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task UpdateQuantity_Guest_UpdatesOwnItem_ShouldSucceed()
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

        // Act: Update own item quantity as guest
        var update = await SendAsync(new UpdateTeamCartItemQuantityCommand(scenario.TeamCartId, itemId, 5));

        // Assert: Should succeed (guest can update their own items)
        update.IsSuccess.Should().BeTrue();

        // Verify persisted quantity
        var quantities = await db.TeamCarts
            .Where(c => c.Id == TeamCartId.Create(scenario.TeamCartId))
            .SelectMany(c => c.Items.Select(i => new { i.Id.Value, i.Quantity }))
            .ToListAsync();
        quantities.Should().Contain(x => x.Value == itemId && x.Quantity == 5);
    }

    [Test]
    public async Task UpdateQuantity_NonMember_ShouldFail()
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

        // Act: Try to update item as non-member - authorization should fail at pipeline level
        await scenario.ActAsNonMember();

        // Assert: Should throw ForbiddenAccessException due to authorization policy
        await FluentActions.Invoking(() =>
                SendAsync(new UpdateTeamCartItemQuantityCommand(scenario.TeamCartId, itemId, 2)))
            .Should().ThrowAsync<ForbiddenAccessException>();
    }
}
