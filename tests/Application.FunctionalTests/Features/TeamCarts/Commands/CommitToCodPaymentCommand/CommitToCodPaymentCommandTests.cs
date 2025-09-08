using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.CommitToCodPayment;
using YummyZoom.Application.TeamCarts.Commands.CreateTeamCart;
using YummyZoom.Application.TeamCarts.Commands.LockTeamCartForPayment;
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
        // Arrange host and create cart
        var hostUserId = await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host User"));
        create.IsSuccess.Should().BeTrue();

        // Add item by host
        var itemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(create.Value.TeamCartId, itemId, 2))).IsSuccess.Should().BeTrue();

        // Lock cart
        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(create.Value.TeamCartId))).IsSuccess.Should().BeTrue();

        // Act
        var result = await SendAsync(new Application.TeamCarts.Commands.CommitToCodPayment.CommitToCodPaymentCommand(create.Value.TeamCartId));

        // Assert
        result.IsSuccess.Should().BeTrue();

        var cart = await FindAsync<TeamCart>(TeamCartId.Create(create.Value.TeamCartId));
        cart.Should().NotBeNull();
        cart!.Status.Should().BeOneOf(TeamCartStatus.Locked, TeamCartStatus.ReadyToConfirm);
        cart.MemberPayments.Should().ContainSingle(p => p.UserId == Domain.UserAggregate.ValueObjects.UserId.Create(hostUserId) && p.Method == PaymentMethod.CashOnDelivery);
    }

    [Test]
    public async Task Commit_Cod_Should_Fail_OnOpenCart()
    {
        var hostUserId = await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host User"));
        create.IsSuccess.Should().BeTrue();

        var result = await SendAsync(new Application.TeamCarts.Commands.CommitToCodPayment.CommitToCodPaymentCommand(create.Value.TeamCartId));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TeamCart.CanOnlyPayOnLockedCart");
    }

    [Test]
    public async Task Commit_Cod_Should_Fail_ForNonMember()
    {
        var hostUserId = await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host User"));
        create.IsSuccess.Should().BeTrue();

        var itemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(create.Value.TeamCartId, itemId, 1))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(create.Value.TeamCartId))).IsSuccess.Should().BeTrue();

        // Switch to a different user (non-member)
        var otherUserId = await CreateUserAsync("nonmember@example.com", "Password123!");
        SetUserId(otherUserId);

        var result = await SendAsync(new Application.TeamCarts.Commands.CommitToCodPayment.CommitToCodPaymentCommand(create.Value.TeamCartId));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TeamCart.UserNotMember");
    }
}


