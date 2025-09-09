using YummyZoom.Application.FunctionalTests.Authorization;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Domain.TeamCartAggregate;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.Commands.LockTeamCartForPaymentCommand;

public class LockTeamCartForPaymentCommandTests : BaseTestFixture
{
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        await TeamCartRoleTestHelper.SetupTeamCartAuthorizationTestsAsync();
    }

    [Test]
    public async Task Lock_Should_Succeed_ForHost_WhenCartHasItems()
    {
        // Arrange: Create team cart scenario with host
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .BuildAsync();

        // Add an item as host so cart is not empty
        await scenario.ActAsHost();
        var itemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        var addItem = await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, itemId, 1));
        addItem.IsSuccess.Should().BeTrue();

        // Act: Lock cart as host
        var lockResult = await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(scenario.TeamCartId));

        // Assert
        lockResult.IsSuccess.Should().BeTrue();

        // Verify persisted state changed to Locked
        var cart = await FindAsync<TeamCart>(TeamCartId.Create(scenario.TeamCartId));
        cart.Should().NotBeNull();
        cart!.Status.Should().Be(TeamCartStatus.Locked);
    }

    [Test]
    public async Task Lock_Should_Fail_WhenCartIsEmpty()
    {
        // Arrange: Create team cart scenario with host (no items)
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .BuildAsync();

        // Act: Try to lock empty cart as host
        await scenario.ActAsHost();
        var lockResult = await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(scenario.TeamCartId));

        // Assert
        lockResult.IsFailure.Should().BeTrue();
        lockResult.Error.Code.Should().Be("TeamCart.CannotLockEmptyCart");

        var cart = await FindAsync<TeamCart>(TeamCartId.Create(scenario.TeamCartId));
        cart.Should().NotBeNull();
        cart!.Status.Should().Be(TeamCartStatus.Open);
    }

    [Test]
    public async Task Lock_Should_Fail_ForNonHost()
    {
        // Arrange: Create team cart scenario with host and guest
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .WithGuest("Guest User")
            .BuildAsync();

        // Add item as host so cart is not empty
        await scenario.ActAsHost();
        var itemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        var addItem = await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, itemId, 1));
        addItem.IsSuccess.Should().BeTrue();

        // Act: Try to lock cart as guest (non-host) - authorization should fail at pipeline level
        await scenario.ActAsGuest("Guest User");
        
        // Assert: Should throw ForbiddenAccessException due to authorization policy
        await FluentActions.Invoking(() => 
                SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(scenario.TeamCartId)))
            .Should().ThrowAsync<ForbiddenAccessException>();

        // Verify cart status remains Open
        var cart = await FindAsync<TeamCart>(TeamCartId.Create(scenario.TeamCartId));
        cart.Should().NotBeNull();
        cart!.Status.Should().Be(TeamCartStatus.Open);
    }

    [Test]
    public async Task Lock_Should_Fail_ForNonMember()
    {
        // Arrange: Create team cart scenario with host
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .BuildAsync();

        // Add item as host so cart is not empty
        await scenario.ActAsHost();
        var itemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        var addItem = await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, itemId, 1));
        addItem.IsSuccess.Should().BeTrue();

        // Act: Try to lock cart as non-member - authorization should fail at pipeline level
        await scenario.ActAsNonMember();
        
        // Assert: Should throw ForbiddenAccessException due to authorization policy
        await FluentActions.Invoking(() => 
                SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(scenario.TeamCartId)))
            .Should().ThrowAsync<ForbiddenAccessException>();

        // Verify cart status remains Open
        var cart = await FindAsync<TeamCart>(TeamCartId.Create(scenario.TeamCartId));
        cart.Should().NotBeNull();
        cart!.Status.Should().Be(TeamCartStatus.Open);
    }
}

