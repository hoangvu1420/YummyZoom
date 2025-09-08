using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.CreateTeamCart;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.Commands.InitiateMemberOnlinePaymentCommand;

public class InitiateMemberOnlinePaymentCommandTests : BaseTestFixture
{
    [Test]
    public async Task Initiate_Should_Return_ClientSecret_ForMember_OnLockedCart()
    {
        var hostUserId = await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host User"));
        create.IsSuccess.Should().BeTrue();

        var itemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(create.Value.TeamCartId, itemId, 1))).IsSuccess.Should().BeTrue();

        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(create.Value.TeamCartId))).IsSuccess.Should().BeTrue();

        var result = await SendAsync(new Application.TeamCarts.Commands.InitiateMemberOnlinePayment.InitiateMemberOnlinePaymentCommand(create.Value.TeamCartId));

        result.IsSuccess.Should().BeTrue();
        result.Value.PaymentIntentId.Should().NotBeNullOrWhiteSpace();
        result.Value.ClientSecret.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Initiate_Should_Fail_OnOpenCart()
    {
        var hostUserId = await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host User"));
        create.IsSuccess.Should().BeTrue();

        var result = await SendAsync(new Application.TeamCarts.Commands.InitiateMemberOnlinePayment.InitiateMemberOnlinePaymentCommand(create.Value.TeamCartId));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TeamCart.CanOnlyPayOnLockedCart");
    }

    [Test]
    public async Task Initiate_Should_Fail_ForNonMember()
    {
        var hostUserId = await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var create = await SendAsync(new CreateTeamCartCommand(restaurantId, "Host User"));
        create.IsSuccess.Should().BeTrue();

        var itemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(create.Value.TeamCartId, itemId, 1))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(create.Value.TeamCartId))).IsSuccess.Should().BeTrue();

        var otherUserId = await CreateUserAsync("nonmember@example.com", "Password123!");
        SetUserId(otherUserId);

        var result = await SendAsync(new Application.TeamCarts.Commands.InitiateMemberOnlinePayment.InitiateMemberOnlinePaymentCommand(create.Value.TeamCartId));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TeamCart.UserNotMember");
    }
}


