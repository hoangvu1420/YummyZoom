using YummyZoom.Application.Admin.Commands.RebuildFullMenu;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Infrastructure.Data.Models;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Admin;

[TestFixture]
public class RebuildFullMenuCommandTests : BaseTestFixture
{
    [Test]
    public async Task Rebuild_PopulatesFullMenuView_AndIsIdempotent()
    {
        var restaurantId = Testing.TestData.DefaultRestaurantId;

        var cmd = new RebuildFullMenuCommand(restaurantId);
        var result = await SendAsync(cmd);
        result.ShouldBeSuccessful();

        var view = await FindAsync<FullMenuView>(restaurantId);
        view.Should().NotBeNull();
        view!.MenuJson.Should().NotBeNullOrWhiteSpace();
        view.LastRebuiltAt.Should().BeAfter(DateTimeOffset.UtcNow.AddMinutes(-5));

        // Idempotency: run again and expect same row updated with later timestamp
        await Task.Delay(10);
        var result2 = await SendAsync(cmd);
        result2.ShouldBeSuccessful();
        var view2 = await FindAsync<FullMenuView>(restaurantId);
        view2!.LastRebuiltAt.Should().BeOnOrAfter(view.LastRebuiltAt);

        // Minimal invariant check: document contains version field
        view2.MenuJson.Should().Contain("\"version\": 1");
    }
}


