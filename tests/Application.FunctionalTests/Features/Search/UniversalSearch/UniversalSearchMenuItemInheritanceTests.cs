using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.MenuItems.Commands.CreateMenuItem;
using YummyZoom.Application.Search.Queries.UniversalSearch;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Search.UniversalSearch;

[TestFixture]
public class UniversalSearchMenuItemInheritanceTests : BaseTestFixture
{
    [Test]
    public async Task MenuItem_ShouldInheritCuisine_AndRespectCuisineFilter()
    {
        // Run as staff for default restaurant to create menu item
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var cmd = new CreateMenuItemCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuCategoryId: Testing.TestData.GetMenuCategoryId("Main Dishes"),
            Name: "Inheritance Test Item",
            Description: "Test item for cuisine inheritance",
            Price: 9.99m,
            Currency: "USD");

        var create = await SendAsync(cmd);
        create.IsSuccess.Should().BeTrue();

        await DrainOutboxAsync();

        // Filter by the default restaurant cuisine (International Fusion from test data)
        var res = await SendAsync(new UniversalSearchQuery(
            Term: "Inheritance",
            Latitude: null,
            Longitude: null,
            OpenNow: null,
            Cuisines: new[] { "International Fusion" },
            PageNumber: 1,
            PageSize: 10));

        res.ShouldBeSuccessful();
        res.Value!.Page.Items.Select(i => i.Name).Should().Contain(n => n.Contains("Inheritance Test Item"));
    }

    [Test]
    public async Task MenuItem_ShouldInheritGeo_AndReportDistance()
    {
        // Create a menu item
        await RunAsRestaurantStaffAsync("staff2@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var cmd = new CreateMenuItemCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuCategoryId: Testing.TestData.GetMenuCategoryId("Main Dishes"),
            Name: "Geo Inheritance Item",
            Description: "Test item for geo inheritance",
            Price: 8.50m,
            Currency: "USD");

        var create = await SendAsync(cmd);
        create.IsSuccess.Should().BeTrue();

        await DrainOutboxAsync();

        var res = await SendAsync(new UniversalSearchQuery(
            Term: "Geo Inheritance",
            Latitude: 47.6100,
            Longitude: -122.3317,
            OpenNow: null,
            Cuisines: null,
            PageNumber: 1,
            PageSize: 10));

        res.ShouldBeSuccessful();
        var item = res.Value!.Page.Items.FirstOrDefault(i => i.Name.Contains("Geo Inheritance Item"));
        item.Should().NotBeNull();
        item!.DistanceKm.Should().NotBeNull();
    }
}

