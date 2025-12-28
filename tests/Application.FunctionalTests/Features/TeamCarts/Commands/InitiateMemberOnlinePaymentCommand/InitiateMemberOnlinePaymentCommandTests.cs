using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Authorization;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.Commands.InitiateMemberOnlinePaymentCommand;

public class InitiateMemberOnlinePaymentCommandTests : BaseTestFixture
{
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        await TeamCartRoleTestHelper.SetupTeamCartAuthorizationTestsAsync();
    }

    [Test]
    public async Task Initiate_Should_Return_ClientSecret_ForMember_OnLockedCart()
    {
        // Arrange: Create team cart scenario with host
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .BuildAsync();

        // Add item as host (who is a member)
        await scenario.ActAsHost();
        var itemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, itemId, 1))).IsSuccess.Should().BeTrue();

        // Lock cart as host
        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(scenario.TeamCartId))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.FinalizePricing.FinalizePricingCommand(scenario.TeamCartId))).IsSuccess.Should().BeTrue();

        // Act: Initiate payment as host (who is a member)
        var result = await SendAsync(new Application.TeamCarts.Commands.InitiateMemberOnlinePayment.InitiateMemberOnlinePaymentCommand(scenario.TeamCartId));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PaymentIntentId.Should().NotBeNullOrWhiteSpace();
        result.Value.ClientSecret.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Initiate_Should_Fail_OnOpenCart()
    {
        // Arrange: Create team cart scenario with host
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .BuildAsync();

        // Act: Try to initiate payment as host on open cart (not locked)
        await scenario.ActAsHost();
        var result = await SendAsync(new Application.TeamCarts.Commands.InitiateMemberOnlinePayment.InitiateMemberOnlinePaymentCommand(scenario.TeamCartId));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TeamCart.CanOnlyPayOnFinalizedCart");
    }

    [Test]
    public async Task Initiate_Should_Fail_ForNonMember()
    {
        // Arrange: Create team cart scenario with host
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .BuildAsync();

        // Add item and lock cart as host
        await scenario.ActAsHost();
        var itemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, itemId, 1))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(scenario.TeamCartId))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.FinalizePricing.FinalizePricingCommand(scenario.TeamCartId))).IsSuccess.Should().BeTrue();

        // Act: Try to initiate payment as non-member (authorization should fail at pipeline level)
        await scenario.ActAsNonMember();

        // Assert: Should throw ForbiddenAccessException due to authorization policy
        await FluentActions.Invoking(() =>
                SendAsync(new Application.TeamCarts.Commands.InitiateMemberOnlinePayment.InitiateMemberOnlinePaymentCommand(scenario.TeamCartId)))
            .Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task Initiate_Should_Return_ClientSecret_ForGuestMember_OnLockedCart()
    {
        // Arrange: Create team cart scenario with host and guest
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host User")
            .WithGuest("Guest User")
            .BuildAsync();

        // Add items as both members
        await scenario.ActAsHost();
        var itemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, itemId, 1))).IsSuccess.Should().BeTrue();

        await scenario.ActAsGuest("Guest User");
        (await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, itemId, 1))).IsSuccess.Should().BeTrue();

        // Lock cart as host
        await scenario.ActAsHost();
        (await SendAsync(new Application.TeamCarts.Commands.LockTeamCartForPayment.LockTeamCartForPaymentCommand(scenario.TeamCartId))).IsSuccess.Should().BeTrue();
        (await SendAsync(new Application.TeamCarts.Commands.FinalizePricing.FinalizePricingCommand(scenario.TeamCartId))).IsSuccess.Should().BeTrue();

        // Act: Initiate payment as guest (who is a member)
        await scenario.ActAsGuest("Guest User");
        var result = await SendAsync(new Application.TeamCarts.Commands.InitiateMemberOnlinePayment.InitiateMemberOnlinePaymentCommand(scenario.TeamCartId));

        // Assert: Guest member should successfully initiate payment
        result.IsSuccess.Should().BeTrue();
        result.Value.PaymentIntentId.Should().NotBeNullOrWhiteSpace();
        result.Value.ClientSecret.Should().NotBeNullOrWhiteSpace();
    }
}

