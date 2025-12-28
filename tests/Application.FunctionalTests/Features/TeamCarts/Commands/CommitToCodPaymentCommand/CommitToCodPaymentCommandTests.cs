using YummyZoom.Application.FunctionalTests.Authorization;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Domain.TeamCartAggregate;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.Commands.CommitToCodPaymentCommand;

public class CommitToCodPaymentCommandTests : BaseTestFixture
{
    [Test]
    public async Task Commit_Cod_Should_Succeed_ForMember_OnLockedCart()
    {
        // Arrange: Create team cart scenario, add item, and lock for payment
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .BuildAsync();

        await scenario.ActAsHost();
        var itemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, itemId, 2))).IsSuccess.Should().BeTrue();

        // Lock cart
        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(scenario.TeamCartId))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.FinalizePricing.FinalizePricingCommand(scenario.TeamCartId))).IsSuccess.Should().BeTrue();

        // Act: Commit COD payment as host (member)
        var result = await SendAsync(new Application.TeamCarts.Commands.CommitToCodPayment.CommitToCodPaymentCommand(scenario.TeamCartId));

        // Assert: Success and payment recorded
        result.IsSuccess.Should().BeTrue();

        var cart = await Testing.FindTeamCartAsync(TeamCartId.Create(scenario.TeamCartId));
        cart.Should().NotBeNull();
        cart!.Status.Should().BeOneOf(TeamCartStatus.Finalized, TeamCartStatus.ReadyToConfirm);
        cart.MemberPayments.Should().ContainSingle(p => p.UserId == Domain.UserAggregate.ValueObjects.UserId.Create(scenario.HostUserId) && p.Method == PaymentMethod.CashOnDelivery);
    }

    [Test]
    public async Task Commit_Cod_Should_Fail_OnOpenCart()
    {
        // Arrange: Create team cart scenario (cart remains open, not locked)
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .BuildAsync();

        // Act: Try to commit COD payment on open cart as host
        await scenario.ActAsHost();
        var itemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, itemId, 1))).IsSuccess.Should().BeTrue();
        var result = await SendAsync(new Application.TeamCarts.Commands.CommitToCodPayment.CommitToCodPaymentCommand(scenario.TeamCartId));

        // Assert: Should fail because cart is not locked
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TeamCart.CanOnlyPayOnFinalizedCart");
    }

    [Test]
    public async Task Commit_Cod_Should_Fail_ForNonMember()
    {
        // Arrange: Create locked cart scenario
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .BuildAsync();

        await scenario.ActAsHost();
        var itemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, itemId, 1))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(scenario.TeamCartId))).IsSuccess.Should().BeTrue();

        // Act & Assert: Switch to non-member and try to commit COD payment should throw ForbiddenAccessException
        var otherUserId = await CreateUserAsync("nonmember@example.com", "Password123!");
        SetUserId(otherUserId);

        await FluentActions.Invoking(() =>
                SendAsync(new Application.TeamCarts.Commands.CommitToCodPayment.CommitToCodPaymentCommand(scenario.TeamCartId)))
            .Should().ThrowAsync<YummyZoom.Application.Common.Exceptions.ForbiddenAccessException>();
    }
}

