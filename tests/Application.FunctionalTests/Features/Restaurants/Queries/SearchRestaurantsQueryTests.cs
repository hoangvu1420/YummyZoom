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
        double? lat = null, double? lng = null, double? radiusKm = null)
        => new(q, cuisine, lat, lng, radiusKm, page, size);

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

    private static async Task<Guid> CreateRestaurantAsync(string name)
    {
        var address = Address.Create("1 St", "C", "S", "Z", "US").Value;
        var contact = ContactInfo.Create("+1-555-0123", "t@test.local").Value;
        var hours = BusinessHours.Create("9-5").Value;
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


