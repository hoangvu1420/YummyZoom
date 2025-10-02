using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Infrastructure;
using YummyZoom.Application.TeamCarts.Commands.CreateTeamCart;
using YummyZoom.Infrastructure.Persistence.EfCore;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.Commands.CreateTeamCart;

public class CreateTeamCartTests : BaseTestFixture
{
    [Test]
    public async Task CreateTeamCart_HappyPath_ShouldCreateAndPersist()
    {
        var userId = await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;

        var cmd = new CreateTeamCartCommand(restaurantId, "Alice Host");
        var result = await SendAsync(cmd);

        result.IsSuccess.Should().BeTrue();
        result.Value.TeamCartId.Should().NotBeEmpty();
        result.Value.ShareToken.Should().NotBeNullOrEmpty();

        // Verify persisted
        using var scope = TestInfrastructure.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var createdId = result.Value.TeamCartId;
        var cart = await db.TeamCarts.FindAsync(Domain.TeamCartAggregate.ValueObjects.TeamCartId.Create(createdId));
        cart.Should().NotBeNull();
    }

    [Test]
    public async Task CreateTeamCart_EmptyHostName_ShouldFailValidation()
    {
        var userId = await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;

        var cmd = new CreateTeamCartCommand(restaurantId, "");

        await FluentActions.Invoking(() => SendAsync(cmd))
            .Should().ThrowAsync<YummyZoom.Application.Common.Exceptions.ValidationException>();
    }
}
