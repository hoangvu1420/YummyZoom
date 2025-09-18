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
public class UniversalSearchDiacriticsTests : BaseTestFixture
{
    [Test]
    public async Task Search_Diacritics_ShouldMatchAccentedName_WithUnaccentFts()
    {
        var id = await CreateRestaurantAsync(
            name: "Bánh Mì 79",
            cuisine: "Vietnamese",
            description: "Authentic bánh mì",
            lat: null,
            lon: null);

        await DrainOutboxAsync();

        var res = await SendAsync(new UniversalSearchQuery(
            Term: "banh mi",
            Latitude: null,
            Longitude: null,
            OpenNow: null,
            Cuisines: null,
            PageNumber: 1,
            PageSize: 10));

        res.ShouldBeSuccessful();
        res.Value.Page.Items.Select(i => i.Id).Should().Contain(id);
        res.Value.Page.Items.Select(i => i.Name).Should().Contain(n => n.Contains("Bánh Mì", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public async Task Search_Diacritics_ShouldMatchAccentedDescriptionAndTags()
    {
        var id = await CreateRestaurantAsync(
            name: "Saigon Street",
            cuisine: "Vietnamese",
            description: "Best bánh mì and phở in town",
            lat: null,
            lon: null);

        await DrainOutboxAsync();

        // Add accented tag directly into the read model to ensure Tags participate via FTS (TsAll)
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE ""SearchIndexItems"" SET ""Tags"" = ARRAY['bánh mì','street food'] WHERE ""Id"" = {id};
                UPDATE ""SearchIndexItems"" SET ""UpdatedAt"" = now() WHERE ""Id"" = {id};
            ");
        }

        var res = await SendAsync(new UniversalSearchQuery(
            Term: "banh mi",
            Latitude: null,
            Longitude: null,
            OpenNow: null,
            Cuisines: null,
            PageNumber: 1,
            PageSize: 10));

        res.ShouldBeSuccessful();
        res.Value.Page.Items.Select(i => i.Id).Should().Contain(id);
    }

    [Test]
    public async Task ShortPrefix_ShouldBeAccentInsensitive_OnNamePrefix()
    {
        var id1 = await CreateRestaurantAsync(
            name: "Bánh House",
            cuisine: "Cafe",
            description: "Fresh pastries",
            lat: null,
            lon: null);
        var id2 = await CreateRestaurantAsync(
            name: "Cafe Z",
            cuisine: "Cafe",
            description: "Regular cafe",
            lat: null,
            lon: null);

        await DrainOutboxAsync();

        var res = await SendAsync(new UniversalSearchQuery(
            Term: "ba",
            Latitude: null,
            Longitude: null,
            OpenNow: null,
            Cuisines: null,
            PageNumber: 1,
            PageSize: 10));

        res.ShouldBeSuccessful();
        var names = res.Value.Page.Items.Select(i => i.Name).ToList();
        names.Should().Contain(n => n.Contains("Bánh", StringComparison.OrdinalIgnoreCase));
        names.Should().NotContain("Cafe Z");
    }

    private static async Task<Guid> CreateRestaurantAsync(string name, string cuisine, string? description, double? lat, double? lon)
    {
        var address = Address.Create("1 St", "C", "S", "Z", "US").Value;
        var contact = ContactInfo.Create("+1-555-0123", "t@test.local").Value;
        var hours = BusinessHours.Create("09:00-17:00").Value;
        var created = Restaurant.Create(name, null, null, description ?? "desc", cuisine, address, contact, hours);
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

