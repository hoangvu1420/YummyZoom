using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.Search.Queries.UniversalSearch;
using YummyZoom.Domain.RestaurantAggregate;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Search.UniversalSearch;

[TestFixture]
public class UniversalSearchOpenNowTests : BaseTestFixture
{
    [Test]
    public async Task OpenNow_ShouldReflectBusinessHours_Computation()
    {
        // Freeze time to noon UTC
        var fixedTime = new FixedTimeProvider(new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero));
        ReplaceService<TimeProvider>(fixedTime);

        // Create a restaurant with simple hours "09:00-17:00" and accept orders
        var restaurantId = await CreateRestaurantAsync("Open Test", "Cafe", null, null);
        await DrainOutboxAsync();

        // Query open-now; should include the restaurant at noon
        var res1 = await SendAsync(new UniversalSearchQuery(
            Term: null,
            Latitude: null,
            Longitude: null,
            OpenNow: true,
            Cuisines: null,
            PageNumber: 1,
            PageSize: 10));

        res1.ShouldBeSuccessful();
        res1.Value!.Page.Items.Select(i => i.Id).Should().Contain(restaurantId);

        // Move time outside business hours and trigger re-projection by touching business hours
        fixedTime.SetUtcNow(new DateTimeOffset(2025, 1, 1, 20, 0, 0, TimeSpan.Zero));

        using (var scope = CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IRestaurantRepository>();
            var agg = await repo.GetByIdAsync(RestaurantId.Create(restaurantId));
            agg!.UpdateBusinessHours("09:00-17:00");
            await UpdateAsync(agg);
        }

        await DrainOutboxAsync();

        var res2 = await SendAsync(new UniversalSearchQuery(
            Term: null,
            Latitude: null,
            Longitude: null,
            OpenNow: true,
            Cuisines: null,
            PageNumber: 1,
            PageSize: 10));

        res2.ShouldBeSuccessful();
        res2.Value!.Page.Items.Select(i => i.Id).Should().NotContain(restaurantId);
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

