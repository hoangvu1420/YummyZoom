using FluentAssertions;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.Search.Queries.UniversalSearch;
using YummyZoom.Domain.RestaurantAggregate;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using static YummyZoom.Application.FunctionalTests.Testing;
using YummyZoom.Infrastructure.Data;

namespace YummyZoom.Application.FunctionalTests.Features.Search;

[TestFixture]
public class UniversalSearchTests : BaseTestFixture
{
    [Test]
    public async Task TextSearch_MatchesNameAndDescription_OrdersByScore()
    {
        await CreateRestaurantAsync("Alpha Cafe", "Cafe", null, null);
        await CreateRestaurantAsync("Alpine Diner", "Diner", null, null);
        await CreateRestaurantAsync("Beta Bistro", "Bistro", null, null);
        await DrainOutboxAsync();

        var q = new UniversalSearchQuery(
            Term: "Al",
            Latitude: null,
            Longitude: null,
            OpenNow: null,
            Cuisines: null,
            PageNumber: 1,
            PageSize: 10);

        var res = await SendAsync(q);
        res.ShouldBeSuccessful();

        var names = res.Value.Items.Select(i => i.Name).ToList();
        names.Should().Contain(new[] { "Alpha Cafe", "Alpine Diner" });
        names.Should().NotContain("Beta Bistro");
        res.Value.Items.Should().OnlyContain(i => i.Name.Contains("Al", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public async Task UpdatedAt_TieBreaksWhenScoresEqual()
    {
        var a = await CreateRestaurantAsync("Tie A", "Cafe", null, null);
        var b = await CreateRestaurantAsync("Tie B", "Cafe", null, null);
        await DrainOutboxAsync();

        // Make B more recent
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE ""SearchIndexItems"" SET ""UpdatedAt"" = now() - interval '10 minutes' WHERE ""Id"" = {a};
                UPDATE ""SearchIndexItems"" SET ""UpdatedAt"" = now() WHERE ""Id"" = {b};
            ");
        }

        var res = await SendAsync(new UniversalSearchQuery(
            Term: "Tie",
            Latitude: null,
            Longitude: null,
            OpenNow: null,
            Cuisines: null,
            PageNumber: 1,
            PageSize: 10));

        res.ShouldBeSuccessful();
        res.Value.Items.Select(i => i.Id).First().Should().Be(b);
    }

    [Test]
    public async Task NoResults_ReturnsEmptyList()
    {
        await CreateRestaurantAsync("Some Cafe", "Cafe", null, null);
        await DrainOutboxAsync();

        var res = await SendAsync(new UniversalSearchQuery(
            Term: "zzzz_unlikely_term",
            Latitude: null,
            Longitude: null,
            OpenNow: null,
            Cuisines: null,
            PageNumber: 1,
            PageSize: 10));

        res.ShouldBeSuccessful();
        res.Value.Items.Should().BeEmpty();
        res.Value.TotalCount.Should().Be(0);
    }

    [Test]
    public async Task SpecialCharacters_TermIsHandledSafely()
    {
        await CreateRestaurantAsync("Sushi (O'Clock) + Bar", "Japanese", null, null);
        await DrainOutboxAsync();

        var res = await SendAsync(new UniversalSearchQuery(
            Term: "(O'Clock) + Bar",
            Latitude: null,
            Longitude: null,
            OpenNow: null,
            Cuisines: null,
            PageNumber: 1,
            PageSize: 10));

        res.ShouldBeSuccessful();
        res.Value.Items.Select(i => i.Name).Should().Contain(n => n.Contains("Sushi", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public async Task Distance_NullWhenNoUserCoordinates()
    {
        await CreateRestaurantAsync("Geo Test", "Cafe", 47.6101, -122.3317);
        await DrainOutboxAsync();

        var res = await SendAsync(new UniversalSearchQuery(
            Term: "Geo",
            Latitude: null,
            Longitude: null,
            OpenNow: null,
            Cuisines: null,
            PageNumber: 1,
            PageSize: 10));

        res.ShouldBeSuccessful();
        res.Value.Items.Should().NotBeEmpty();
        res.Value.Items.Should().OnlyContain(i => i.DistanceKm == null);
    }

    [Test]
    public async Task OpenNowFilter_IncludesOnlyOpenAndAccepting()
    {
        var r1 = await CreateRestaurantAsync("Open Accepting", "Cafe", null, null);
        var r2 = await CreateRestaurantAsync("Open Not Accepting", "Cafe", null, null);
        await DrainOutboxAsync();

        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            // Force flags in read model to simulate current open state
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE ""SearchIndexItems"" SET ""IsOpenNow"" = TRUE, ""IsAcceptingOrders"" = TRUE WHERE ""Id"" = {r1};
                UPDATE ""SearchIndexItems"" SET ""IsOpenNow"" = TRUE, ""IsAcceptingOrders"" = FALSE WHERE ""Id"" = {r2};
            ");
        }

        var res = await SendAsync(new UniversalSearchQuery(
            Term: null,
            Latitude: null,
            Longitude: null,
            OpenNow: true,
            Cuisines: null,
            PageNumber: 1,
            PageSize: 10));

        res.ShouldBeSuccessful();
        var ids = res.Value.Items.Select(i => i.Id).ToHashSet();
        ids.Should().Contain(r1);
        ids.Should().NotContain(r2);
    }

    [Test]
    public async Task ShortQuery_BackoffReturnsPrefixMatches()
    {
        await CreateRestaurantAsync("Alpha Cafe", "Cafe", null, null);
        await CreateRestaurantAsync("Alpine Diner", "Diner", null, null);
        await CreateRestaurantAsync("Beta Bistro", "Bistro", null, null);
        await DrainOutboxAsync();

        var res = await SendAsync(new UniversalSearchQuery(
            Term: "A",
            Latitude: null,
            Longitude: null,
            OpenNow: null,
            Cuisines: null,
            PageNumber: 1,
            PageSize: 10));

        res.ShouldBeSuccessful();
        var names = res.Value.Items.Select(i => i.Name).ToList();
        names.Should().Contain(new[] { "Alpha Cafe", "Alpine Diner" });
        names.Should().NotContain("Beta Bistro");
    }

    [Test]
    public async Task Distance_CloserItemsScoreHigher_WhenLatLonProvided()
    {
        await CreateRestaurantAsync("Near Place", "Cafe", 47.6101, -122.3317);
        await CreateRestaurantAsync("Far Place", "Cafe", 37.7749, -122.4194);
        await DrainOutboxAsync();

        var res = await SendAsync(new UniversalSearchQuery(
            Term: null,
            Latitude: 47.6100,
            Longitude: -122.3317,
            OpenNow: null,
            Cuisines: null,
            PageNumber: 1,
            PageSize: 10));

        res.ShouldBeSuccessful();
        res.Value.Items.Should().NotBeEmpty();

        // First should be the near one, distances should be non-null and ordered
        var distances = res.Value.Items
            .Where(i => i.DistanceKm.HasValue)
            .Select(i => i.DistanceKm.GetValueOrDefault())
            .ToList();
        distances.Should().NotBeEmpty();
        distances.Should().BeInAscendingOrder();
        res.Value.Items.First().Name.Should().Be("Near Place");
    }

    [Test]
    public async Task CuisineFilter_IncludesOnlyRequested()
    {
        await CreateRestaurantAsync("Tag Test 1", "Italian", null, null);
        await CreateRestaurantAsync("Tag Test 2", "Vegan", null, null);
        await DrainOutboxAsync();

        var res = await SendAsync(new UniversalSearchQuery(
            Term: null,
            Latitude: null,
            Longitude: null,
            OpenNow: null,
            Cuisines: new[] { "Italian" },
            PageNumber: 1,
            PageSize: 10));

        res.ShouldBeSuccessful();
        res.Value.Items.Should().OnlyContain(x => string.Equals(x.Cuisine, "Italian", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public async Task Pagination_ReturnsConsistentPageMetadata()
    {
        for (int i = 0; i < 12; i++)
            await CreateRestaurantAsync($"Paging {i:00}", "Cafe", null, null);
        await DrainOutboxAsync();

        var page1 = await SendAsync(new UniversalSearchQuery(
            Term: "Paging",
            Latitude: null,
            Longitude: null,
            OpenNow: null,
            Cuisines: null,
            PageNumber: 1,
            PageSize: 5));

        var page2 = await SendAsync(new UniversalSearchQuery(
            Term: "Paging",
            Latitude: null,
            Longitude: null,
            OpenNow: null,
            Cuisines: null,
            PageNumber: 2,
            PageSize: 5));

        page1.ShouldBeSuccessful();
        page2.ShouldBeSuccessful();
        page1.Value.Items.Should().HaveCount(5);
        page2.Value.Items.Should().HaveCount(5);
        page1.Value.PageNumber.Should().Be(1);
        page2.Value.PageNumber.Should().Be(2);
        page1.Value.TotalCount.Should().BeGreaterThanOrEqualTo(10);
    }

    [Test]
    public async Task Pagination_NoOverlapBetweenPages_StableOrdering()
    {
        for (int i = 0; i < 15; i++)
            await CreateRestaurantAsync($"PageStable {i:00}", "Cafe", null, null);
        await DrainOutboxAsync();

        var page1 = await SendAsync(new UniversalSearchQuery(
            Term: "PageStable",
            Latitude: null,
            Longitude: null,
            OpenNow: null,
            Cuisines: null,
            PageNumber: 1,
            PageSize: 5));

        var page2 = await SendAsync(new UniversalSearchQuery(
            Term: "PageStable",
            Latitude: null,
            Longitude: null,
            OpenNow: null,
            Cuisines: null,
            PageNumber: 2,
            PageSize: 5));

        page1.ShouldBeSuccessful();
        page2.ShouldBeSuccessful();
        var ids1 = page1.Value.Items.Select(i => i.Id).ToHashSet();
        var ids2 = page2.Value.Items.Select(i => i.Id).ToHashSet();
        ids1.Intersect(ids2).Should().BeEmpty();
    }

    [Test]
    public async Task SoftDeleted_ExcludedFromResults()
    {
        var id = await CreateRestaurantAsync("To Be Deleted", "Cafe", null, null);
        await DrainOutboxAsync();

        // Pragmatic: directly mark soft-deleted in read model to validate exclusion behavior
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE ""SearchIndexItems"" SET ""SoftDeleted"" = TRUE WHERE ""Id"" = {id}");
        }

        var res = await SendAsync(new UniversalSearchQuery(
            Term: "Deleted",
            Latitude: null,
            Longitude: null,
            OpenNow: null,
            Cuisines: null,
            PageNumber: 1,
            PageSize: 10));

        res.ShouldBeSuccessful();
        res.Value.Items.Should().NotContain(i => i.Id == id);
    }

    [Test]
    public async Task Validation_FailsForInvalidInputs()
    {
        var invalidPageNumber = () => SendAsync(new UniversalSearchQuery(null, null, null, null, null, 0, 10));
        await invalidPageNumber.Should().ThrowAsync<ValidationException>();

        var invalidPageSize = () => SendAsync(new UniversalSearchQuery(null, null, null, null, null, 1, 0));
        await invalidPageSize.Should().ThrowAsync<ValidationException>();

        var invalidLat = () => SendAsync(new UniversalSearchQuery(null, 100, 0, null, null, 1, 10));
        await invalidLat.Should().ThrowAsync<ValidationException>();

        var invalidLon = () => SendAsync(new UniversalSearchQuery(null, 0, 200, null, null, 1, 10));
        await invalidLon.Should().ThrowAsync<ValidationException>();
    }

    private static async Task<Guid> CreateRestaurantAsync(string name, string cuisine, double? lat, double? lon)
    {
        var address = Address.Create("1 St", "C", "S", "Z", "US").Value;
        var contact = ContactInfo.Create("+1-555-0123", "t@test.local").Value;
        var hours = BusinessHours.Create("9-5").Value;
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
