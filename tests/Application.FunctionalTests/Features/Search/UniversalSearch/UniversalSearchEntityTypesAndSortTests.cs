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
public class UniversalSearchEntityTypesAndSortTests : BaseTestFixture
{
    [Test]
    public async Task EntityTypes_Filter_RestaurantOnly()
    {
        await CreateRestaurantAsync("US Types R1", "Cafe", null, null);
        await CreateRestaurantAsync("US Types R2", "Cafe", null, null);
        await DrainOutboxAsync();

        var res = await SendAsync(new UniversalSearchQuery(
            Term: null,
            Latitude: null,
            Longitude: null,
            OpenNow: null,
            Cuisines: null,
            Tags: null,
            PriceBands: null,
            EntityTypes: new[] { "restaurant" },
            Sort: null,
            IncludeFacets: false,
            PageNumber: 1,
            PageSize: 10));

        res.ShouldBeSuccessful();
        res.Value.Page.Items.Should().NotBeEmpty();
        res.Value.Page.Items.All(i => i.Type.Equals("restaurant", StringComparison.OrdinalIgnoreCase)).Should().BeTrue();
    }

    [Test]
    public async Task Sort_Rating_OrdersByAvgRatingDesc()
    {
        var r1 = await CreateRestaurantAsync("US Rating Low", "Cafe", null, null);
        var r2 = await CreateRestaurantAsync("US Rating High", "Cafe", null, null);
        await DrainOutboxAsync();

        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE ""SearchIndexItems"" SET ""AvgRating"" = {3.1}, ""ReviewCount"" = {5} WHERE ""Id"" = {r1};
                UPDATE ""SearchIndexItems"" SET ""AvgRating"" = {4.8}, ""ReviewCount"" = {200} WHERE ""Id"" = {r2};
            ");
        }

        var res = await SendAsync(new UniversalSearchQuery(
            Term: null,
            Latitude: null,
            Longitude: null,
            OpenNow: null,
            Cuisines: null,
            Tags: null,
            PriceBands: null,
            EntityTypes: new[] { "restaurant" },
            Sort: "rating",
            IncludeFacets: false,
            PageNumber: 1,
            PageSize: 10));

        res.ShouldBeSuccessful();
        res.Value.Page.Items.First().Name.Should().Be("US Rating High");
    }

    [Test]
    public async Task Sort_PriceBand_Ascending()
    {
        var r1 = await CreateRestaurantAsync("US PB High", "Cafe", null, null);
        var r2 = await CreateRestaurantAsync("US PB Low", "Cafe", null, null);
        await DrainOutboxAsync();

        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            short pbHigh = 4, pbLow = 1;
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE ""SearchIndexItems"" SET ""PriceBand"" = {pbHigh} WHERE ""Id"" = {r1};
                UPDATE ""SearchIndexItems"" SET ""PriceBand"" = {pbLow}  WHERE ""Id"" = {r2};
            ");
        }

        var res = await SendAsync(new UniversalSearchQuery(
            Term: null,
            Latitude: null,
            Longitude: null,
            OpenNow: null,
            Cuisines: null,
            Tags: null,
            PriceBands: null,
            EntityTypes: new[] { "restaurant" },
            Sort: "priceBand",
            IncludeFacets: false,
            PageNumber: 1,
            PageSize: 10));

        res.ShouldBeSuccessful();
        res.Value.Page.Items.First().Name.Should().Be("US PB Low");
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
