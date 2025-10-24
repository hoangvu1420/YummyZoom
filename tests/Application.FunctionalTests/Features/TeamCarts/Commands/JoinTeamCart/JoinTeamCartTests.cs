using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Infrastructure;
using YummyZoom.Application.TeamCarts.Commands.CreateTeamCart;
using YummyZoom.Application.TeamCarts.Commands.JoinTeamCart;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.Commands.JoinTeamCart;

public class JoinTeamCartTests : BaseTestFixture
{
    [Test]
    public async Task JoinTeamCart_HappyPath_ShouldAddMemberAndPersist()
    {
        // Host creates the cart
        var hostUserId = await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;

        var createCmd = new CreateTeamCartCommand(restaurantId, "Alice Host");
        var createResult = await SendAsync(createCmd);

        createResult.IsSuccess.Should().BeTrue();
        var teamCartId = createResult.Value.TeamCartId;
        var shareToken = createResult.Value.ShareToken;

        // Switch to a different user (guest)
        var guestUserId = await CreateUserAsync("guest@example.com", "Password123!");
        SetUserId(guestUserId);

        var joinCmd = new JoinTeamCartCommand(teamCartId, shareToken, "Bob Guest");
        var joinResult = await SendAsync(joinCmd);

        joinResult.IsSuccess.Should().BeTrue();

        // Verify persisted membership
        var cart = await Testing.FindTeamCartAsync(TeamCartId.Create(teamCartId));

        cart.Should().NotBeNull();
        cart!.Members.Should().Contain(m => m.UserId.Value == guestUserId);
    }

    [Test]
    public async Task JoinTeamCart_EmptyGuestName_ShouldFailValidation()
    {
        await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;

        var createCmd = new CreateTeamCartCommand(restaurantId, "Alice Host");
        var createResult = await SendAsync(createCmd);
        createResult.IsSuccess.Should().BeTrue();

        var teamCartId = createResult.Value.TeamCartId;
        var shareToken = createResult.Value.ShareToken;

        // Switch to a different user (guest)
        var guestUserId = await CreateUserAsync("guest2@example.com", "Password123!");
        SetUserId(guestUserId);

        var joinCmd = new JoinTeamCartCommand(teamCartId, shareToken, "");

        await FluentActions.Invoking(() => SendAsync(joinCmd))
            .Should().ThrowAsync<YummyZoom.Application.Common.Exceptions.ValidationException>();
    }
}

