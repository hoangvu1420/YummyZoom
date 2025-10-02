using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.Search.Queries.UniversalSearch;
using YummyZoom.Domain.RestaurantAggregate;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Search.UniversalSearch;

[TestFixture]
public class UniversalSearchBadgesAndReasonTests : BaseTestFixture
{
    [Test]
    public async Task OpenNowBadge_IsReturned_WhenOpenAndAccepting()
    {
        var id = await CreateRestaurantAsync("Badge Open", "Cafe", null, null);
        await DrainOutboxAsync();

        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE ""SearchIndexItems"" SET ""IsOpenNow"" = TRUE, ""IsAcceptingOrders"" = TRUE WHERE ""Id"" = {id};
            ");
        }

        var res = await SendAsync(new UniversalSearchQuery(
            Term: "Badge Open",
            Latitude: null,
            Longitude: null,
            OpenNow: null,
            Cuisines: null,
            PageNumber: 1,
            PageSize: 10));

        res.ShouldBeSuccessful();
        var item = res.Value.Page.Items.Should().ContainSingle(i => i.Id == id).Subject;
        item.Badges.Should().Contain(b => b.Code == "open_now" && b.Label == "Open now");
        item.Reason.Should().NotBeNull();
        item.Reason!.Should().Contain("Open now");
    }

    [Test]
    public async Task NearYouBadge_IsReturned_WhenWithinThreshold()
    {
        // Restaurant ~10-20 meters away from the query point
        var id = await CreateRestaurantAsync("Badge Near", "Cafe", 47.61010, -122.33170);
        await DrainOutboxAsync();

        var res = await SendAsync(new UniversalSearchQuery(
            Term: "Badge Near",
            Latitude: 47.61000,
            Longitude: -122.33170,
            OpenNow: null,
            Cuisines: null,
            PageNumber: 1,
            PageSize: 10));

        res.ShouldBeSuccessful();
        var item = res.Value.Page.Items.Should().ContainSingle(i => i.Id == id).Subject;
        item.DistanceKm.Should().NotBeNull();
        item.Badges.Should().Contain(b => b.Code == "near_you" && b.Label.Contains("away"));
        item.Reason.Should().NotBeNull();
        item.Reason!.Should().Contain("away");
    }

    [Test]
    public async Task RatingBadge_IsReturned_WhenAboveThreshold()
    {
        var id = await CreateRestaurantAsync("Badge Rating", "Cafe", null, null);
        await DrainOutboxAsync();

        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE ""SearchIndexItems"" SET ""AvgRating"" = {4.6}, ""ReviewCount"" = {120} WHERE ""Id"" = {id};
            ");
        }

        var res = await SendAsync(new UniversalSearchQuery(
            Term: "Badge Rating",
            Latitude: null,
            Longitude: null,
            OpenNow: null,
            Cuisines: null,
            PageNumber: 1,
            PageSize: 10));

        res.ShouldBeSuccessful();
        var item = res.Value.Page.Items.Should().ContainSingle(i => i.Id == id).Subject;
        item.Badges.Should().Contain(b => b.Code == "rating" && b.Label == "⭐ 4.6 (120)");
        item.Reason.Should().NotBeNull();
        item.Reason!.Should().Contain("⭐ 4.6 (120)");
    }

    [Test]
    public async Task NoBadges_ProducesGenericReason()
    {
        var id = await CreateRestaurantAsync("Badge None", "Cafe", null, null);
        await DrainOutboxAsync();

        // Ensure no open/accepting flags to avoid badges
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE ""SearchIndexItems"" 
                SET ""IsOpenNow"" = FALSE, ""IsAcceptingOrders"" = FALSE, ""AvgRating"" = NULL, ""ReviewCount"" = 0
                WHERE ""Id"" = {id};
            ");
        }

        var res = await SendAsync(new UniversalSearchQuery(
            Term: "Badge None",
            Latitude: null,
            Longitude: null,
            OpenNow: null,
            Cuisines: null,
            PageNumber: 1,
            PageSize: 10));

        res.ShouldBeSuccessful();
        var item = res.Value.Page.Items.Should().ContainSingle(i => i.Id == id).Subject;
        item.Badges.Should().BeEmpty();
        item.Reason.Should().Be("Relevant match");
    }

    private static async Task<Guid> CreateRestaurantAsync(string name, string cuisine, double? lat, double? lon)
    {
        var address = Address.Create("1 St", "C", "S", "Z", "US").Value;
        var contact = ContactInfo.Create("+1-555-0123", "t@test.local").Value;
        var hours = BusinessHours.Create("09:00-17:00").Value;
        var created = Restaurant.Create(name, null, null, "desc", cuisine, address, contact, hours);
        var entity = created.Value;

        if (lat.HasValue && lon.HasValue)
        {
            entity.ChangeGeoCoordinates(lat.Value, lon.Value);
        }

        entity.Verify();
        entity.AcceptOrders();

        await AddAsync(entity);
        return entity.Id.Value;
    }
}
