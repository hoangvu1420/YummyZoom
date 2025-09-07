using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.CreateTeamCart;
using YummyZoom.Domain.TeamCartAggregate;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.Commands.LockTeamCartForPaymentCommand;

public class LockTeamCartForPaymentCommandTests : BaseTestFixture
{
    [Test]
    public async Task Lock_Should_Succeed_ForHost_WhenCartHasItems()
    {
        // Arrange host and create cart
        var hostUserId = await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host User"));
        create.IsSuccess.Should().BeTrue();

        // Add an item so cart is not empty
        var itemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        var addItem = await SendAsync(new AddItemToTeamCartCommand(create.Value.TeamCartId, itemId, 1));
        addItem.IsSuccess.Should().BeTrue();

        // Act
        var lockResult = await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(create.Value.TeamCartId));

        // Assert
        lockResult.IsSuccess.Should().BeTrue();

        // Verify persisted state changed to Locked
        var cart = await FindAsync<TeamCart>(TeamCartId.Create(create.Value.TeamCartId));
        cart.Should().NotBeNull();
        cart!.Status.Should().Be(TeamCartStatus.Locked);
    }

    [Test]
    public async Task Lock_Should_Fail_WhenCartIsEmpty()
    {
        // Arrange host and create cart (no items)
        var hostUserId = await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host User"));
        create.IsSuccess.Should().BeTrue();

        // Act
        var lockResult = await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(create.Value.TeamCartId));

        // Assert
        lockResult.IsFailure.Should().BeTrue();
        lockResult.Error.Code.Should().Be("TeamCart.CannotLockEmptyCart");

        var cart = await FindAsync<TeamCart>(TeamCartId.Create(create.Value.TeamCartId));
        cart.Should().NotBeNull();
        cart!.Status.Should().Be(TeamCartStatus.Open);
    }

    [Test]
    public async Task Lock_Should_Fail_ForNonHost()
    {
        // Arrange host creates cart and adds item
        var hostUserId = await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host User"));
        create.IsSuccess.Should().BeTrue();

        var itemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        var addItem = await SendAsync(new AddItemToTeamCartCommand(create.Value.TeamCartId, itemId, 1));
        addItem.IsSuccess.Should().BeTrue();

        // Switch to a different user (non-host)
        var otherUserId = await CreateUserAsync("not-host@example.com", "Password123!");
        SetUserId(otherUserId);

        // Act
        var lockResult = await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(create.Value.TeamCartId));

        // Assert
        lockResult.IsFailure.Should().BeTrue();
        lockResult.Error.Code.Should().Be("TeamCart.OnlyHostCanLockCart");

        var cart = await FindAsync<TeamCart>(TeamCartId.Create(create.Value.TeamCartId));
        cart.Should().NotBeNull();
        cart!.Status.Should().Be(TeamCartStatus.Open);
    }
}

