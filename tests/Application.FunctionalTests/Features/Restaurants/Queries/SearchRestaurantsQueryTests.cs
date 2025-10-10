using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.Restaurants.Queries.SearchRestaurants;
using YummyZoom.Domain.RestaurantAggregate;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Restaurants.Queries;

[TestFixture]
public class SearchRestaurantsQueryTests : BaseTestFixture
{
    private static SearchRestaurantsQuery Build(string? q = null, string? cuisine = null, int page = 1, int size = 10,
        double? lat = null, double? lng = null, double? radiusKm = null, string? sort = null)
        => new(q, cuisine, lat, lng, radiusKm, page, size, null, sort);

    [Test]
    public async Task TextFilter_FiltersByName_OrderedByNameThenId()
    {
        // Seed few more restaurants with controlled names
        var r1 = await CreateRestaurantAsync("Alpha Cafe");
        var r2 = await CreateRestaurantAsync("Beta Bistro");
        var r3 = await CreateRestaurantAsync("Alpine Diner");

        var result = await SendAsync(Build(q: "Al"));
        result.ShouldBeSuccessful();
        var names = result.Value.Items.Select(i => i.Name).ToList();
        names.Should().Contain(new[] { "Alpha Cafe", "Alpine Diner" });
        names.Should().NotContain("Beta Bistro");
        names.Where(n => n.StartsWith("Al")).Should().BeInAscendingOrder();
    }

    [Test]
    public async Task CuisineFilter_ReturnsOnlyTaggedRows()
    {
        // Tag two restaurants with Italian via raw SQL cuisine tags array
        var r1 = await CreateRestaurantAsync("Tag Test 1");
        var r2 = await CreateRestaurantAsync("Tag Test 2");

        await SetCuisineTagsAsync(r1, new[] { "Italian" });
        await SetCuisineTagsAsync(r2, new[] { "Vegan" });

        var result = await SendAsync(Build(cuisine: "Italian"));
        result.ShouldBeSuccessful();
        result.Value.Items.Should().OnlyContain(x => x.CuisineTags.Contains("Italian"));
    }

    [Test]
    public async Task Pagination_ReturnsConsistentPageMetadata()
    {
        // Ensure enough rows
        for (int i = 0; i < 15; i++)
            await CreateRestaurantAsync($"Paging {i:00}");

        var page1 = await SendAsync(Build(q: "Paging", page: 1, size: 5));
        var page2 = await SendAsync(Build(q: "Paging", page: 2, size: 5));

        page1.ShouldBeSuccessful();
        page2.ShouldBeSuccessful();

        page1.Value.Items.Should().HaveCount(5);
        page2.Value.Items.Should().HaveCount(5);
        page1.Value.PageNumber.Should().Be(1);
        page2.Value.PageNumber.Should().Be(2);
        page1.Value.TotalCount.Should().BeGreaterThanOrEqualTo(10);
    }

    [Test]
    public async Task Validation_InvalidPaging_ShouldThrow()
    {
        var act = async () => await SendAsync(Build(page: 0, size: 10));
        await act.Should().ThrowAsync<ValidationException>();

        var act2 = async () => await SendAsync(Build(page: 1, size: 0));
        await act2.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task Validation_InvalidGeoCombination_ShouldThrow()
    {
        var act = async () => await SendAsync(Build(lat: 10, lng: null, radiusKm: null));
        await act.Should().ThrowAsync<ValidationException>();

        var act2 = async () => await SendAsync(Build(lat: 10, lng: 10, radiusKm: 30));
        await act2.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task Distance_Sort_WithLatLng_ReturnsOrderedAndDistanceKm()
    {
        var rNear = await CreateRestaurantAsync("Geo Near");
        var rFar  = await CreateRestaurantAsync("Geo Far");

        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE ""Restaurants"" SET ""Geo_Latitude"" = {37.7749}, ""Geo_Longitude"" = {-122.4194} WHERE ""Id"" = {rNear};
                UPDATE ""Restaurants"" SET ""Geo_Latitude"" = {37.8044}, ""Geo_Longitude"" = {-122.2711} WHERE ""Id"" = {rFar};
            ");
        }

        var res = await SendAsync(Build(lat: 37.7749, lng: -122.4194, sort: "distance"));
        res.ShouldBeSuccessful();

        res.Value.Items.Should().NotBeEmpty();
        res.Value.Items.First().Name.Should().Be("Geo Near");
        res.Value.Items.First().DistanceKm.Should().BeApproximately(0m, 0.1m);
        res.Value.Items.All(i => i.DistanceKm.HasValue).Should().BeTrue();
    }

    [Test]
    public async Task Distance_Sort_WithoutGeo_FallsBack_NoDistanceKm()
    {
        await CreateRestaurantAsync("No Geo 1");
        await CreateRestaurantAsync("No Geo 2");

        var res = await SendAsync(Build(sort: "distance"));
        res.ShouldBeSuccessful();
        res.Value.Items.All(i => i.DistanceKm is null).Should().BeTrue();
    }

    [Test]
    public async Task Rating_Sort_OrdersByAverageRatingDesc()
    {
        var rLow  = await CreateRestaurantAsync("Low Rating");
        var rHigh = await CreateRestaurantAsync("High Rating");

        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""RestaurantReviewSummaries"" (""RestaurantId"", ""AverageRating"", ""TotalReviews"") VALUES ({rLow}, 3.2, 10)
                ON CONFLICT (""RestaurantId"") DO UPDATE SET ""AverageRating"" = 3.2, ""TotalReviews"" = 10;
                INSERT INTO ""RestaurantReviewSummaries"" (""RestaurantId"", ""AverageRating"", ""TotalReviews"") VALUES ({rHigh}, 4.8, 200)
                ON CONFLICT (""RestaurantId"") DO UPDATE SET ""AverageRating"" = 4.8, ""TotalReviews"" = 200;
            ");
        }

        var res = await SendAsync(Build(sort: "rating"));
        res.ShouldBeSuccessful();
        res.Value.Items.First().Name.Should().Be("High Rating");
        res.Value.Items.Select(i => i.AvgRating).Should().BeInDescendingOrder();
    }

    private static async Task<Guid> CreateRestaurantAsync(string name)
    {
        var address = Address.Create("1 St", "C", "S", "Z", "US").Value;
        var contact = ContactInfo.Create("+1-555-0123", "t@test.local").Value;
        var hours = BusinessHours.Create("09:00-17:00").Value;
        var create = Restaurant.Create(name, null, null, "desc", "Cuisine", address, contact, hours);
        create.ShouldBeSuccessful();
        var entity = create.Value;
        entity.Verify();
        entity.AcceptOrders();
        entity.ClearDomainEvents();
        await AddAsync(entity);
        return entity.Id.Value;
    }

    private static async Task SetCuisineTagsAsync(Guid restaurantId, string[] tags)
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        // Simplified: we only have CuisineType column; set it to the first tag (if any)
        var cuisine = tags.Length > 0 ? tags[0] : null;
        await db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE ""Restaurants"" 
            SET ""CuisineType"" = {cuisine}
            WHERE ""Id"" = {restaurantId}
        ");
    }
}
