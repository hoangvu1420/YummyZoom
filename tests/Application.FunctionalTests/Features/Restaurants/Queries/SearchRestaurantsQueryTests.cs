using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.Restaurants.Queries.SearchRestaurants;
using YummyZoom.Domain.CouponAggregate;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Application.Restaurants.Queries.Common;
using YummyZoom.Domain.MenuEntity;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.TagEntity;
using YummyZoom.Domain.TagEntity.Enums;
using YummyZoom.Domain.TagEntity.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore;
using YummyZoom.SharedKernel;
using Money = YummyZoom.Domain.Common.ValueObjects.Money;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Restaurants.Queries;

[TestFixture]
public class SearchRestaurantsQueryTests : BaseTestFixture
{
    private static SearchRestaurantsQuery Build(
        string? q = null,
        string? cuisine = null,
        int page = 1,
        int size = 10,
        double? lat = null,
        double? lng = null,
        double? radiusKm = null,
        double? minRating = null,
        string? sort = null,
        bool? discountedOnly = null,
        bool includeFacets = false)
        => new(q, cuisine, lat, lng, radiusKm, page, size, minRating, sort, null, null, null, discountedOnly, includeFacets);

    private static PaginatedList<RestaurantSearchResultDto> ExtractPage(Result<SearchRestaurantsResult> result)
    {
        result.ShouldBeSuccessful();
        return result.Value.Should().BeOfType<RestaurantSearchPageResult>().Subject.Page;
    }

    private static RestaurantSearchWithFacetsDto ExtractFaceted(Result<SearchRestaurantsResult> result)
    {
        result.ShouldBeSuccessful();
        return result.Value.Should().BeOfType<RestaurantSearchWithFacetsDto>().Subject;
    }

    [Test]
    public async Task TextFilter_FiltersByName_OrderedByNameThenId()
    {
        // Seed few more restaurants with controlled names
        var r1 = await CreateRestaurantAsync("Alpha Cafe");
        var r2 = await CreateRestaurantAsync("Beta Bistro");
        var r3 = await CreateRestaurantAsync("Alpine Diner");

        var result = await SendAsync(Build(q: "Al"));
        var page = ExtractPage(result);
        var names = page.Items.Select(i => i.Name).ToList();
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
        var page = ExtractPage(result);
        page.Items.Should().OnlyContain(x => x.CuisineTags.Contains("Italian"));
    }

    [Test]
    public async Task Pagination_ReturnsConsistentPageMetadata()
    {
        // Ensure enough rows
        for (int i = 0; i < 15; i++)
            await CreateRestaurantAsync($"Paging {i:00}");

        var page1Result = await SendAsync(Build(q: "Paging", page: 1, size: 5));
        var page2Result = await SendAsync(Build(q: "Paging", page: 2, size: 5));

        var page1 = ExtractPage(page1Result);
        var page2 = ExtractPage(page2Result);

        page1.Items.Should().HaveCount(5);
        page2.Items.Should().HaveCount(5);
        page1.PageNumber.Should().Be(1);
        page2.PageNumber.Should().Be(2);
        page1.TotalCount.Should().BeGreaterThanOrEqualTo(10);
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
        var page = ExtractPage(res);

        page.Items.Should().NotBeEmpty();
        page.Items.First().Name.Should().Be("Geo Near");
        page.Items.First().DistanceKm.Should().BeApproximately(0m, 0.1m);
        page.Items.All(i => i.DistanceKm.HasValue).Should().BeTrue();
    }

    [Test]
    public async Task Distance_Sort_WithoutGeo_FallsBack_NoDistanceKm()
    {
        await CreateRestaurantAsync("No Geo 1");
        await CreateRestaurantAsync("No Geo 2");

        var res = await SendAsync(Build(sort: "distance"));
        var page = ExtractPage(res);
        page.Items.All(i => i.DistanceKm is null).Should().BeTrue();
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
        var page = ExtractPage(res);
        page.Items.First().Name.Should().Be("High Rating");
        page.Items.Select(i => i.AvgRating).Should().BeInDescendingOrder();
    }

    [Test]
    public async Task Popularity_Sort_OrdersByReviewCountDesc()
    {
        var rLow  = await CreateRestaurantAsync("Few Reviews");
        var rHigh = await CreateRestaurantAsync("Many Reviews");

        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""RestaurantReviewSummaries"" (""RestaurantId"", ""AverageRating"", ""TotalReviews"") VALUES ({rLow}, 4.9, 3)
                ON CONFLICT (""RestaurantId"") DO UPDATE SET ""AverageRating"" = 4.9, ""TotalReviews"" = 3;
                INSERT INTO ""RestaurantReviewSummaries"" (""RestaurantId"", ""AverageRating"", ""TotalReviews"") VALUES ({rHigh}, 4.2, 250)
                ON CONFLICT (""RestaurantId"") DO UPDATE SET ""AverageRating"" = 4.2, ""TotalReviews"" = 250;
            ");
        }

        var res = await SendAsync(Build(sort: "popularity"));
        var page = ExtractPage(res);
        page.Items.First().Name.Should().Be("Many Reviews");
        page.Items.Select(i => i.RatingCount ?? 0).Should().BeInDescendingOrder();
    }

    [Test]
    public async Task DiscountedOnlyTrue_ReturnsOnlyRestaurantsWithActiveCoupons()
    {
        var discounted = await CreateRestaurantAsync("Discounted Diner");
        var regular = await CreateRestaurantAsync("Regular Eatery");

        await CreateActiveCouponAsync(discounted);

        var res = await SendAsync(Build(discountedOnly: true));
        var page = ExtractPage(res);

        var ids = page.Items.Select(i => i.RestaurantId).ToList();
        ids.Should().Contain(discounted);
        ids.Should().NotContain(regular);
    }

    [Test]
    public async Task DiscountedOnly_WithMinRating_FiltersOutLowRatedDiscounts()
    {
        var highRated = await CreateRestaurantAsync("High Rated Discount");
        var lowRated = await CreateRestaurantAsync("Low Rated Discount");

        await CreateActiveCouponAsync(highRated);
        await CreateActiveCouponAsync(lowRated);

        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""RestaurantReviewSummaries"" (""RestaurantId"", ""AverageRating"", ""TotalReviews"") VALUES ({highRated}, 4.5, 50)
                ON CONFLICT (""RestaurantId"") DO UPDATE SET ""AverageRating"" = 4.5, ""TotalReviews"" = 50;
                INSERT INTO ""RestaurantReviewSummaries"" (""RestaurantId"", ""AverageRating"", ""TotalReviews"") VALUES ({lowRated}, 3.0, 20)
                ON CONFLICT (""RestaurantId"") DO UPDATE SET ""AverageRating"" = 3.0, ""TotalReviews"" = 20;
            ");
        }

        var res = await SendAsync(Build(minRating: 4.0, discountedOnly: true));
        var page = ExtractPage(res);

        var ids = page.Items.Select(i => i.RestaurantId).ToList();
        ids.Should().Contain(highRated);
        ids.Should().NotContain(lowRated);
    }

    [Test]
    public async Task IncludeFacetsTrue_ReturnsFacetBucketsWithOpenNowCount()
    {
        var seed = await SeedFacetDataAsync("FacetA");

        var result = await SendAsync(Build(q: "FacetA", includeFacets: true));
        var faceted = ExtractFaceted(result);

        faceted.Page.Items.Should().OnlyContain(i => i.Name.Contains("FacetA", StringComparison.OrdinalIgnoreCase));

        faceted.Facets.Cuisines.Should().ContainSingle(f => f.Value == "italian" && f.Count == 2);
        faceted.Facets.Cuisines.Should().Contain(f => f.Value == "vegan" && f.Count == 1);

        faceted.Facets.Tags.Should().ContainSingle(f => f.Value == seed.TagName.ToLowerInvariant() && f.Count == 2);

        faceted.Facets.PriceBands.Should().BeEmpty();

        faceted.Facets.OpenNowCount.Should().Be(2);
    }

    [Test]
    public async Task IncludeFacets_RespectsExistingFilters()
    {
        var seed = await SeedFacetDataAsync("FacetB");

        var result = await SendAsync(Build(q: "FacetB", cuisine: "Italian", includeFacets: true));
        var faceted = ExtractFaceted(result);

        faceted.Page.Items.Should().OnlyContain(i => i.CuisineTags.Contains("Italian"));
        faceted.Facets.Cuisines.Should().ContainSingle(f => f.Value == "italian" && f.Count == 2);
        faceted.Facets.Cuisines.Should().HaveCount(1);

        faceted.Facets.Tags.Should().ContainSingle(f => f.Value == seed.TagName.ToLowerInvariant() && f.Count == 2);
        faceted.Facets.OpenNowCount.Should().Be(2);
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

    private sealed record FacetSeedResult(string TagName, Guid[] TaggedRestaurantIds);

    private static async Task<FacetSeedResult> SeedFacetDataAsync(string prefix)
    {
        var tagName = $"{prefix}-GlutenFree";
        var tagId = await CreateTagAsync(tagName);

        var italianOne = await CreateRestaurantAsync($"{prefix} Italian 1");
        var italianTwo = await CreateRestaurantAsync($"{prefix} Italian 2");
        var vegan = await CreateRestaurantAsync($"{prefix} Vegan");

        await SetCuisineTagsAsync(italianOne, new[] { "Italian" });
        await SetCuisineTagsAsync(italianTwo, new[] { "Italian" });
        await SetCuisineTagsAsync(vegan, new[] { "Vegan" });

        await CreateMenuItemWithTagAsync(italianOne, tagId);
        await CreateMenuItemWithTagAsync(italianTwo, tagId);

        await SetAcceptingOrdersAsync(vegan, accepting: false);

        await DrainOutboxAsync();

        return new FacetSeedResult(tagName, new[] { italianOne, italianTwo });
    }

    private static async Task<Guid> CreateTagAsync(string name, TagCategory category = TagCategory.Dietary)
    {
        var result = Tag.Create(name, category);
        result.ShouldBeSuccessful();
        var tag = result.Value;
        tag.ClearDomainEvents();
        await AddAsync(tag);
        return tag.Id.Value;
    }

    private static async Task CreateMenuItemWithTagAsync(Guid restaurantId, Guid tagId)
    {
        var restaurantIdVo = RestaurantId.Create(restaurantId);

        var menuResult = Menu.Create(restaurantIdVo, $"Menu {Guid.NewGuid():N}".Substring(0, 12), "Facet menu");
        menuResult.ShouldBeSuccessful();
        var menu = menuResult.Value;
        menu.ClearDomainEvents();
        await AddAsync(menu);

        var categoryResult = MenuCategory.Create(menu.Id, "Facets", 1);
        categoryResult.ShouldBeSuccessful();
        var category = categoryResult.Value;
        category.ClearDomainEvents();
        await AddAsync(category);

        var menuItemResult = MenuItem.Create(
            restaurantIdVo,
            category.Id,
            $"Facet Dish {Guid.NewGuid():N}".Substring(0, 12),
            "Facet test dish",
            new Money(12.5m, "USD"),
            dietaryTagIds: new List<TagId> { TagId.Create(tagId) });

        menuItemResult.ShouldBeSuccessful();
        var menuItem = menuItemResult.Value;
        menuItem.ClearDomainEvents();
        await AddAsync(menuItem);
    }

    private static async Task SetAcceptingOrdersAsync(Guid restaurantId, bool accepting)
    {
        var restaurantIdVo = RestaurantId.Create(restaurantId);
        var restaurant = await FindAsync<Restaurant>(restaurantIdVo);
        restaurant.Should().NotBeNull();
        if (accepting)
        {
            restaurant!.AcceptOrders();
        }
        else
        {
            restaurant!.DeclineOrders();
        }

        restaurant.ClearDomainEvents();
        await UpdateAsync(restaurant);
    }

    private static async Task CreateActiveCouponAsync(Guid restaurantId, bool isEnabled = true, DateTime? start = null, DateTime? end = null)
    {
        var restaurantIdVo = RestaurantId.Create(restaurantId);

        var appliesToResult = AppliesTo.CreateForWholeOrder();
        appliesToResult.ShouldBeSuccessful();

        var valueResult = CouponValue.CreatePercentage(15);
        valueResult.ShouldBeSuccessful();

        var couponResult = Coupon.Create(
            restaurantIdVo,
            $"DISC{Guid.NewGuid():N}"[..8].ToUpperInvariant(),
            "Functional test coupon",
            valueResult.Value,
            appliesToResult.Value,
            start ?? DateTime.UtcNow.AddDays(-1),
            end ?? DateTime.UtcNow.AddDays(1),
            minOrderAmount: null,
            totalUsageLimit: null,
            usageLimitPerUser: null,
            isEnabled: isEnabled);

        couponResult.ShouldBeSuccessful();
        var coupon = couponResult.Value;

        if (!isEnabled)
        {
            coupon.Disable();
        }

        coupon.ClearDomainEvents();
        await AddAsync(coupon);
    }
}
