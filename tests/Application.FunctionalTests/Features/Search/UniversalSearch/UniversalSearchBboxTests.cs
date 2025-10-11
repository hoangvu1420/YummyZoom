using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.Search.Queries.UniversalSearch;
using YummyZoom.Domain.RestaurantAggregate;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Search.UniversalSearch;

[TestFixture]
public class UniversalSearchBboxTests : BaseTestFixture
{
    [Test]
    public async Task Bbox_Filter_IncludesAndExcludes()
    {
        var r1 = await CreateRestaurantAsync("BBox In 1", "Cafe", 37.775, -122.419);
        var r2 = await CreateRestaurantAsync("BBox Out", "Cafe", 37.40, -122.00);
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
            Bbox: "-122.52,37.70,-122.35,37.82",
            Sort: null,
            IncludeFacets: false,
            PageNumber: 1,
            PageSize: 20));

        res.ShouldBeSuccessful();
        var names = res.Value.Page.Items.Select(i => i.Name).ToList();
        names.Should().Contain(n => n.Contains("BBox In"));
        names.Should().NotContain(n => n.Contains("BBox Out"));
    }

    private static async Task<Guid> CreateRestaurantAsync(string name, string cuisine, double? lat, double? lon)
    {
        var address = Address.Create("1 St", "C", "S", "Z", "US").Value;
        var contact = ContactInfo.Create("+1-555-0123", "t@test.local").Value;
        var hours = BusinessHours.Create("09:00-17:00").Value;
        var created = Restaurant.Create(name, null, null, "desc", cuisine, address, contact, hours);
        var entity = created.Value;
        if (lat.HasValue && lon.HasValue) entity.ChangeGeoCoordinates(lat.Value, lon.Value);
        entity.Verify();
        entity.AcceptOrders();
        await AddAsync(entity);
        return entity.Id.Value;
    }
}

