using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Authorization;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.TeamCarts.Commands.CreateTeamCart;
using YummyZoom.Application.TeamCarts.Commands.JoinTeamCart;
using YummyZoom.Application.TeamCarts.Commands.SetMemberReady;
using YummyZoom.Domain.TeamCartAggregate.Errors;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.Commands.SetMemberReady;

public class SetMemberReadyTests : BaseTestFixture
{
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        await TeamCartRoleTestHelper.SetupTeamCartAuthorizationTestsAsync();
    }

    [Test]
    public async Task SetMemberReady_ShouldSucceed_WhenUserIsMember()
    {
        // Arrange: Create team cart scenario with host and guest
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host")
            .WithGuest("Member")
            .BuildAsync();

        await DrainOutboxAsync(); // Process events from team cart creation and member joining

        // Act as the guest member and set ready status
        await scenario.ActAsGuest("Member");
        var command = new SetMemberReadyCommand(scenario.TeamCartId, true);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        
        // Verify state in store
        var query = new YummyZoom.Application.TeamCarts.Queries.GetTeamCartRealTimeViewModel.GetTeamCartRealTimeViewModelQuery(scenario.TeamCartId);
        var queryResult = await SendAsync(query);
        var cart = queryResult.Value.TeamCart;
        var guestUserId = scenario.GetGuestUserId("Member");
        var member = cart.Members.First(m => m.UserId == guestUserId);
        member.IsReady.Should().BeTrue();
    }

    [Test]
    public async Task SetMemberReady_ShouldFail_WhenUserIsNotMember()
    {
        // Arrange: Create team cart scenario with host only
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Host")
            .BuildAsync();

        await DrainOutboxAsync(); // Process events from team cart creation

        // Act as a non-member user
        await scenario.ActAsNonMember();
        var command = new SetMemberReadyCommand(scenario.TeamCartId, true);

        // Act & Assert: Should throw ForbiddenAccessException due to authorization policy
        await FluentActions.Invoking(() =>
                SendAsync(command))
            .Should().ThrowAsync<ForbiddenAccessException>();
    }
}
