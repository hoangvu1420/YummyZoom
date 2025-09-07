using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.CreateTeamCart;
using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.TeamCartAggregate;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.Commands.ApplyTipToTeamCartCommand;

public class ApplyTipToTeamCartCommandTests : BaseTestFixture
{
    [Test]
    public async Task ApplyTip_Should_Succeed_ForHost_WhenCartLocked()
    {
        // Arrange: host creates cart, adds an item, locks for payment
        var hostUserId = await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;

        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host User"));
        create.IsSuccess.Should().BeTrue();

        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(create.Value.TeamCartId, burgerId, 1))).IsSuccess.Should().BeTrue();

        var lockResult = await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(create.Value.TeamCartId));
        lockResult.IsSuccess.Should().BeTrue();

        // Act: apply a tip
        var tipAmount = 10.50m;
        var apply = await SendAsync(new Application.TeamCarts.Commands.ApplyTipToTeamCart.ApplyTipToTeamCartCommand(create.Value.TeamCartId, tipAmount));

        // Assert: success and persisted TipAmount updated
        apply.IsSuccess.Should().BeTrue();

        var cart = await FindAsync<TeamCart>(TeamCartId.Create(create.Value.TeamCartId));
        cart.Should().NotBeNull();
        cart!.TipAmount.Amount.Should().Be(tipAmount);
        cart.TipAmount.Currency.Should().Be(Currencies.Default);
    }

    [Test]
    public async Task ApplyTip_Should_Fail_WhenCartOpen()
    {
        // Arrange: host creates cart (not locked)
        var hostUserId = await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host User"));
        create.IsSuccess.Should().BeTrue();

        // Act
        var apply = await SendAsync(new Application.TeamCarts.Commands.ApplyTipToTeamCart.ApplyTipToTeamCartCommand(create.Value.TeamCartId, 5m));

        // Assert: fails with correct error code
        apply.IsFailure.Should().BeTrue();
        apply.Error.Code.Should().Be("TeamCart.CanOnlyApplyFinancialsToLockedCart");
    }

    [Test]
    public async Task ApplyTip_Should_Fail_ForNonHost_WhenCartLocked()
    {
        // Arrange: host creates cart, adds an item and locks
        var hostUserId = await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host User"));
        create.IsSuccess.Should().BeTrue();

        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(create.Value.TeamCartId, burgerId, 1))).IsSuccess.Should().BeTrue();

        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(create.Value.TeamCartId)))
            .IsSuccess.Should().BeTrue();

        // Switch to another (non-host) user
        var otherUserId = await CreateUserAsync("not-host-applytip@example.com", "Password123!");
        SetUserId(otherUserId);

        // Act
        var apply = await SendAsync(new Application.TeamCarts.Commands.ApplyTipToTeamCart.ApplyTipToTeamCartCommand(create.Value.TeamCartId, 3m));

        // Assert
        apply.IsFailure.Should().BeTrue();
        apply.Error.Code.Should().Be("TeamCart.OnlyHostCanModifyFinancials");

        var cart = await FindAsync<TeamCart>(TeamCartId.Create(create.Value.TeamCartId));
        cart.Should().NotBeNull();
        cart!.TipAmount.Amount.Should().Be(0m);
    }

    [Test]
    public async Task ApplyTip_NegativeAmount_ShouldFailValidation()
    {
        // Arrange
        await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host User"));
        create.IsSuccess.Should().BeTrue();

        // Act + Assert: validator rejects negative tip
        await FluentActions.Invoking(() => SendAsync(new Application.TeamCarts.Commands.ApplyTipToTeamCart.ApplyTipToTeamCartCommand(create.Value.TeamCartId, -1m)))
            .Should().ThrowAsync<YummyZoom.Application.Common.Exceptions.ValidationException>();
    }
}
