using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.Coupons.Queries.FastCheck;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Coupons.Queries.FastCheck;

[TestFixture]
public class FastCouponCheckTests : BaseTestFixture
{
    [SetUp]
    public void SetUpUser()
    {
        // Authenticate as default customer (fast-check requires authenticated user)
        SetUserId(Testing.TestData.DefaultCustomerId);
    }

    [Test]
    public async Task FastCheck_WithValidCart_ReturnsBestDealAndCandidates()
    {
        // Arrange: build two-line cart from seeded menu
        var burgerId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger); // $15.99
        var wingsId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.BuffaloWings);   // $12.99

        var burger = await FindAsync<MenuItem>(MenuItemId.Create(burgerId));
        var wings = await FindAsync<MenuItem>(MenuItemId.Create(wingsId));

        burger.Should().NotBeNull();
        wings.Should().NotBeNull();

        var items = new List<FastCouponCheckItemDto>
        {
            new(burger!.Id.Value, burger.MenuCategoryId.Value, 2, burger.BasePrice.Amount),
            new(wings!.Id.Value, wings.MenuCategoryId.Value, 1, wings.BasePrice.Amount)
        };

        // Create a coupon for the restaurant
        await CreateActiveCouponAsync(Testing.TestData.DefaultRestaurantId, "TEST15", 15m);

        // Process outbox events to ensure coupon is persisted
        await DrainOutboxAsync();

        // Refresh the materialized view to include the new coupon
        await RefreshMaterializedViewAsync();

        var q = new FastCouponCheckQuery(Testing.TestData.DefaultRestaurantId, items);

        // Act
        var result = await SendAsync(q);

        // Assert
        result.IsSuccess.Should().BeTrue(result.Error?.ToString());
        var resp = result.Value;
        resp.Suggestions.Should().NotBeEmpty();
        resp.BestDeal.Should().NotBeNull();

        // Expected subtotal = (15.99 * 2) + (12.99 * 1) = 44.97
        const decimal expectedSubtotal = 44.97m;
        // Default coupon in test data is 15% off whole order; allow small rounding tolerance
        var expectedSavings = Math.Round(expectedSubtotal * 0.15m, 2);
        resp.BestDeal!.Savings.Should().BeApproximately(expectedSavings, 0.01m);
    }

    [Test]
    public async Task FastCheck_WithOtherRestaurant_ShouldReturnNoCandidates()
    {
        // Arrange: create second restaurant and an item belonging to it
        var (restaurant2Id, menuItemId) = await TestData.TestDataFactory.CreateSecondRestaurantWithMenuItemsAsync();
        var menuItem = await FindAsync<MenuItem>(MenuItemId.Create(menuItemId));
        menuItem.Should().NotBeNull();

        var items = new List<FastCouponCheckItemDto>
        {
            new(menuItem!.Id.Value, menuItem.MenuCategoryId.Value, 1, menuItem.BasePrice.Amount)
        };

        var q = new FastCouponCheckQuery(restaurant2Id, items);

        // Act
        var result = await SendAsync(q);

        // Assert
        result.IsSuccess.Should().BeTrue(result.Error?.ToString());
        var resp = result.Value;
        // Default coupons belong to the default restaurant only; expect no suggestions here
        resp.Suggestions.Should().BeEmpty();
        resp.BestDeal.Should().BeNull();
    }

    private static async Task CreateActiveCouponAsync(Guid restaurantId, string code, decimal percentageValue)
    {
        var restaurantIdVo = YummyZoom.Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(restaurantId);
        var valueResult = YummyZoom.Domain.CouponAggregate.ValueObjects.CouponValue.CreatePercentage(percentageValue);
        var appliesToResult = YummyZoom.Domain.CouponAggregate.ValueObjects.AppliesTo.CreateForWholeOrder();

        var couponResult = YummyZoom.Domain.CouponAggregate.Coupon.Create(
            restaurantIdVo,
            code,
            $"{percentageValue}% off entire order",
            valueResult.Value,
            appliesToResult.Value,
            validityStartDate: DateTime.UtcNow.AddDays(-1),
            validityEndDate: DateTime.UtcNow.AddDays(30),
            minOrderAmount: null,
            totalUsageLimit: null,
            usageLimitPerUser: null,
            isEnabled: true);

        couponResult.ShouldBeSuccessful();
        var coupon = couponResult.Value;
        coupon.ClearDomainEvents();
        await AddAsync(coupon);
    }

    private static async Task RefreshMaterializedViewAsync()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<YummyZoom.Infrastructure.Persistence.EfCore.ApplicationDbContext>();
        await db.Database.ExecuteSqlRawAsync("REFRESH MATERIALIZED VIEW active_coupons_view;");
    }
}

