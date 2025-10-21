using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.Restaurants.Queries.GetRestaurantAggregatedDetails;
using YummyZoom.Infrastructure.Persistence.EfCore;
using YummyZoom.Domain.RestaurantAggregate;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Restaurants.Queries;

[TestFixture]
public class GetRestaurantAggregatedDetailsQueryTests : BaseTestFixture
{
    [Test]
    public async Task ReturnsCombinedDetailsAndFreshnessMetadata()
    {
        var restaurantGuid = Testing.TestData.DefaultRestaurantId;
        var restaurantId = RestaurantId.Create(restaurantGuid);
        var infoLastModified = DateTimeOffset.UtcNow.AddHours(-1);
        var menuLastRebuiltAt = infoLastModified.AddMinutes(15);
        var summaryUpdatedAtUtc = DateTime.UtcNow.AddMinutes(-10);
        const string menuJson = "{\"menus\":[{\"id\":\"menu-1\",\"name\":\"Dinner\",\"categories\":[{\"id\":\"cat-1\",\"name\":\"Starters\"}]}]}";

        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var restaurant = await db.Restaurants.SingleAsync(r => r.Id == restaurantId);
            restaurant.Verify();
            restaurant.LastModified = infoLastModified;

            await db.SaveChangesAsync();

            await db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE ""Restaurants""
                SET ""LastModified"" = {infoLastModified.UtcDateTime}, ""IsAcceptingOrders"" = TRUE
                WHERE ""Id"" = {restaurantGuid};
            ");

            await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""FullMenuViews"" (""RestaurantId"", ""MenuJson"", ""LastRebuiltAt"")
                VALUES ({restaurantGuid}, {menuJson}::jsonb, {menuLastRebuiltAt.UtcDateTime})
                ON CONFLICT (""RestaurantId"")
                DO UPDATE SET ""MenuJson"" = {menuJson}::jsonb, ""LastRebuiltAt"" = {menuLastRebuiltAt.UtcDateTime};
            ");

            await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""RestaurantReviewSummaries"" (
                    ""RestaurantId"",
                    ""AverageRating"",
                    ""TotalReviews"",
                    ""Ratings1"",
                    ""Ratings2"",
                    ""Ratings3"",
                    ""Ratings4"",
                    ""Ratings5"",
                    ""TotalWithText"",
                    ""LastReviewAtUtc"",
                    ""UpdatedAtUtc"")
                VALUES (
                    {restaurantGuid},
                    {4.7},
                    {125},
                    {3},
                    {5},
                    {10},
                    {25},
                    {82},
                    {60},
                    {summaryUpdatedAtUtc.AddMinutes(-5)},
                    {summaryUpdatedAtUtc})
                ON CONFLICT (""RestaurantId"")
                DO UPDATE SET
                    ""AverageRating"" = {4.7},
                    ""TotalReviews"" = {125},
                    ""Ratings1"" = {3},
                    ""Ratings2"" = {5},
                    ""Ratings3"" = {10},
                    ""Ratings4"" = {25},
                    ""Ratings5"" = {82},
                    ""TotalWithText"" = {60},
                    ""LastReviewAtUtc"" = {summaryUpdatedAtUtc.AddMinutes(-5)},
                    ""UpdatedAtUtc"" = {summaryUpdatedAtUtc};
            ");
        }

        var result = await SendAsync(new GetRestaurantAggregatedDetailsQuery(restaurantGuid));
        result.IsSuccess.Should().BeTrue();
        var aggregated = result.Value;

        aggregated.Info.LastModified.Should().BeCloseTo(infoLastModified, TimeSpan.FromSeconds(1));
        using var expectedMenu = JsonDocument.Parse(menuJson);
        using var actualMenu = JsonDocument.Parse(aggregated.Menu.MenuJson);
        JsonElement.DeepEquals(actualMenu.RootElement, expectedMenu.RootElement).Should().BeTrue();
        aggregated.Menu.LastRebuiltAt.Should().BeCloseTo(menuLastRebuiltAt, TimeSpan.FromMilliseconds(1));
        aggregated.ReviewSummary.TotalReviews.Should().Be(125);
        aggregated.LastChangedUtc.Should().BeCloseTo(new DateTimeOffset(summaryUpdatedAtUtc.ToUniversalTime()), TimeSpan.FromMilliseconds(1));
    }

    [Test]
    public async Task ReturnsFallbacksWhenMenuAndSummaryMissing()
    {
        var restaurantGuid = Testing.TestData.DefaultRestaurantId;
        var restaurantId = RestaurantId.Create(restaurantGuid);
        var infoLastModified = DateTimeOffset.UtcNow.AddHours(-2);

        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var restaurant = await db.Restaurants.SingleAsync(r => r.Id == restaurantId);
            restaurant.Verify();
            restaurant.LastModified = infoLastModified;
            await db.SaveChangesAsync();

            await db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE ""Restaurants""
                SET ""LastModified"" = {infoLastModified.UtcDateTime}
                WHERE ""Id"" = {restaurantGuid};
            ");

            await db.Database.ExecuteSqlInterpolatedAsync($@"DELETE FROM ""FullMenuViews"" WHERE ""RestaurantId"" = {restaurantGuid};");
            await db.Database.ExecuteSqlInterpolatedAsync($@"DELETE FROM ""RestaurantReviewSummaries"" WHERE ""RestaurantId"" = {restaurantGuid};");
        }

        var result = await SendAsync(new GetRestaurantAggregatedDetailsQuery(restaurantGuid));
        result.IsSuccess.Should().BeTrue();
        var aggregated = result.Value;

        aggregated.Menu.MenuJson.Should().Be("{}");
        aggregated.Menu.LastRebuiltAt.Should().BeCloseTo(infoLastModified, TimeSpan.FromSeconds(1));
        aggregated.ReviewSummary.TotalReviews.Should().Be(0);
        aggregated.LastChangedUtc.Should().BeCloseTo(infoLastModified, TimeSpan.FromSeconds(1));
    }

    [Test]
    public async Task PersonalizedRequests_ShouldReturnDistinctRestaurants()
    {
        var firstId = await SeedVerifiedRestaurantAsync("Cache Bypass One", "CuisineA");
        var secondId = await SeedVerifiedRestaurantAsync("Cache Bypass Two", "CuisineB");

        var first = await SendAsync(new GetRestaurantAggregatedDetailsQuery(firstId, Lat: 47.6100, Lng: -122.3317));
        var second = await SendAsync(new GetRestaurantAggregatedDetailsQuery(secondId, Lat: 48.5200, Lng: -122.4000));

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();

        first.Value.Info.RestaurantId.Should().Be(firstId);
        second.Value.Info.RestaurantId.Should().Be(secondId);
        first.Value.Info.Name.Should().Be("Cache Bypass One");
        second.Value.Info.Name.Should().Be("Cache Bypass Two");
    }

    private static async Task<Guid> SeedVerifiedRestaurantAsync(string name, string cuisine)
    {
        var address = Address.Create("123 Road", "City", "ST", "00000", "US").Value;
        var contact = ContactInfo.Create("+1-555-0000", "seed@yummyzoom.test").Value;
        var hours = BusinessHours.Create("09:00-17:00").Value;
        var created = Restaurant.Create(name, null, null, "Desc", cuisine, address, contact, hours);
        var entity = created.Value;
        entity.Verify();
        entity.AcceptOrders();

        await AddAsync(entity);
        return entity.Id.Value;
    }
}
