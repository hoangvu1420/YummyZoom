using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.Restaurants.Queries.SearchRestaurants;
using YummyZoom.Domain.RestaurantAggregate;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Restaurants.Queries;

[TestFixture]
public class SearchRestaurantsBboxTests : BaseTestFixture
{
    [Test]
    public async Task Bbox_Filter_IncludesAndExcludes_ByLatLon()
    {
        var inside = await CreateRestaurantAsync("Inside", 37.775, -122.419);
        var outside = await CreateRestaurantAsync("Outside", 37.40, -122.00);
        await DrainOutboxAsync();

        var res = await SendAsync(new SearchRestaurantsQuery(null, null, null, null, null, 1, 20, null, null, "-122.52,37.70,-122.35,37.82"));
        res.ShouldBeSuccessful();
        res.Value.Items.Select(i => i.RestaurantId).Should().Contain(inside);
        res.Value.Items.Select(i => i.RestaurantId).Should().NotContain(outside);
    }

    private static async Task<Guid> CreateRestaurantAsync(string name, double lat, double lon)
    {
        var address = Address.Create("1 St", "C", "S", "Z", "US").Value;
        var contact = ContactInfo.Create("+1-555-0123", "t@test.local").Value;
        var hours = BusinessHours.Create("09:00-17:00").Value;
        var created = Restaurant.Create(name, null, null, "desc", "Cuisine", address, contact, hours);
        var entity = created.Value;
        entity.ChangeGeoCoordinates(lat, lon);
        entity.Verify();
        entity.AcceptOrders();
        await AddAsync(entity);
        return entity.Id.Value;
    }
}

