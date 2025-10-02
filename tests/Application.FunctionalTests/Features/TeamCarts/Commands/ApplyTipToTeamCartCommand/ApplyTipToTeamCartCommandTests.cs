using YummyZoom.Application.FunctionalTests.Authorization;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
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
        // Arrange: Create team cart scenario, add item, and lock for payment
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .BuildAsync();

        await scenario.ActAsHost();
        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 1))).IsSuccess.Should().BeTrue();

        var lockResult = await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(scenario.TeamCartId));
        lockResult.IsSuccess.Should().BeTrue();

        // Act: Apply tip as host
        var tipAmount = 10.50m;
        var apply = await SendAsync(new Application.TeamCarts.Commands.ApplyTipToTeamCart.ApplyTipToTeamCartCommand(scenario.TeamCartId, tipAmount));

        // Assert: Success and persisted TipAmount updated
        apply.IsSuccess.Should().BeTrue();

        var cart = await FindAsync<TeamCart>(TeamCartId.Create(scenario.TeamCartId));
        cart.Should().NotBeNull();
        cart!.TipAmount.Amount.Should().Be(tipAmount);
        cart.TipAmount.Currency.Should().Be(Currencies.Default);
    }

    [Test]
    public async Task ApplyTip_Should_Fail_WhenCartOpen()
    {
        // Arrange: Create team cart scenario (cart remains open, not locked)
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .BuildAsync();

        // Act: Try to apply tip to open cart as host
        await scenario.ActAsHost();
        var apply = await SendAsync(new Application.TeamCarts.Commands.ApplyTipToTeamCart.ApplyTipToTeamCartCommand(scenario.TeamCartId, 5m));

        // Assert: Should fail because cart is not locked
        apply.IsFailure.Should().BeTrue();
        apply.Error.Code.Should().Be("TeamCart.CanOnlyApplyFinancialsToLockedCart");
    }

    [Test]
    public async Task ApplyTip_Should_Fail_ForNonHost_WhenCartLocked()
    {
        // Arrange: Create locked cart scenario
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .BuildAsync();

        await scenario.ActAsHost();
        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, burgerId, 1))).IsSuccess.Should().BeTrue();

        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(scenario.TeamCartId)))
            .IsSuccess.Should().BeTrue();

        // Act & Assert: Switch to non-host and try to apply tip should throw ForbiddenAccessException
        var otherUserId = await CreateUserAsync("not-host-applytip@example.com", "Password123!");
        SetUserId(otherUserId);

        await FluentActions.Invoking(() =>
                SendAsync(new Application.TeamCarts.Commands.ApplyTipToTeamCart.ApplyTipToTeamCartCommand(scenario.TeamCartId, 3m)))
            .Should().ThrowAsync<YummyZoom.Application.Common.Exceptions.ForbiddenAccessException>();
    }

    [Test]
    public async Task ApplyTip_NegativeAmount_ShouldFailValidation()
    {
        // Arrange: Create team cart scenario
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .BuildAsync();

        // Act & Assert: Validator should reject negative tip amount
        await scenario.ActAsHost();
        await FluentActions.Invoking(() => SendAsync(new Application.TeamCarts.Commands.ApplyTipToTeamCart.ApplyTipToTeamCartCommand(scenario.TeamCartId, -1m)))
            .Should().ThrowAsync<YummyZoom.Application.Common.Exceptions.ValidationException>();
    }
}
