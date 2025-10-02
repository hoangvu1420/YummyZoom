using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.Coupons.Queries.GetCouponStats;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Features.Orders.Commands.InitiateOrder;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Domain.CouponAggregate;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.OrderAggregate;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Application.FunctionalTests.Features.Coupons.Queries;

using static Testing;

public class GetCouponStatsTests : BaseTestFixture
{
    [Test]
    public async Task GetCouponStats_ReturnsAggregateValues()
    {
        var (restaurantId, menuItemId) = await TestDataFactory.CreateSecondRestaurantWithMenuItemsAsync();
        var restaurant = RestaurantId.Create(restaurantId);
        var coupon = await CreateCouponAsync(restaurant, "STAT10");

        var userAEmail = "stats-user-a@yummyzoom.test";
        var userBEmail = "stats-user-b@yummyzoom.test";
        var password = "Password123!";

        var userAGuid = await RunAsUserAsync(userAEmail, password, Array.Empty<string>());

        var firstUsageTimestamp = await CreateOrderAsync(restaurantId, menuItemId, coupon.Code, userAGuid);
        var secondUsageTimestamp = await CreateOrderAsync(restaurantId, menuItemId, coupon.Code, userAGuid);

        var userBGuid = await RunAsUserAsync(userBEmail, password, Array.Empty<string>());
        var latestUsageTimestamp = await CreateOrderAsync(restaurantId, menuItemId, coupon.Code, userBGuid);

        await RunAsRestaurantStaffAsync("staff-stats@restaurant.com", restaurantId);

        var result = await SendAsync(new GetCouponStatsQuery(restaurantId, coupon.Id.Value));

        result.ShouldBeSuccessful();
        var dto = result.Value;
        dto.TotalUsage.Should().Be(3);
        dto.UniqueUsers.Should().Be(2);
        dto.LastUsedAtUtc.Should().NotBeNull();
        dto.LastUsedAtUtc!.Value.Should().BeOnOrAfter(secondUsageTimestamp);
        dto.LastUsedAtUtc.Value.Should().BeCloseTo(latestUsageTimestamp, TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task GetCouponStats_NotFound_ReturnsFailure()
    {
        await RunAsRestaurantStaffAsync("staff-stats-notfound@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var result = await SendAsync(new GetCouponStatsQuery(Testing.TestData.DefaultRestaurantId, Guid.NewGuid()));

        result.ShouldBeFailure("Coupon.Stats.NotFound");
    }

    [Test]
    public async Task GetCouponStats_WrongRestaurant_ThrowsForbidden()
    {
        var (restaurantId, _) = await TestDataFactory.CreateSecondRestaurantWithMenuItemsAsync();
        await RunAsRestaurantStaffAsync("staff-wrongscope@restaurant.com", restaurantId);

        await FluentActions.Invoking(() => SendAsync(new GetCouponStatsQuery(
            Testing.TestData.DefaultRestaurantId,
            Testing.TestData.DefaultCouponId)))
            .Should().ThrowAsync<ForbiddenAccessException>();
    }

    private static async Task<Coupon> CreateCouponAsync(RestaurantId restaurantId, string code)
    {
        var value = CouponValue.CreatePercentage(10).Value;
        var applies = AppliesTo.CreateForWholeOrder().Value;
        var now = DateTime.UtcNow;

        var coupon = Coupon.Create(
            restaurantId,
            code,
            "Stats coupon",
            value,
            applies,
            now.AddDays(-10),
            now.AddDays(10),
            minOrderAmount: null,
            totalUsageLimit: null,
            usageLimitPerUser: null,
            isEnabled: true).Value;
        coupon.ClearDomainEvents();
        await AddAsync(coupon);
        return coupon;
    }

    private static async Task<DateTime> CreateOrderAsync(
        Guid restaurantId,
        Guid menuItemId,
        string couponCode,
        Guid userId)
    {
        SetUserId(userId);

        var command = InitiateOrderTestHelper.BuildValidCommand(
            customerId: userId,
            restaurantId: restaurantId,
            menuItemIds: new List<Guid> { menuItemId },
            paymentMethod: InitiateOrderTestHelper.PaymentMethods.CashOnDelivery,
            couponCode: couponCode);

        var response = await SendAsync(command);
        response.IsSuccess.Should().BeTrue(response.Error?.ToString());

        var order = await FindAsync<Order>(OrderId.Create(response.Value.OrderId.Value));
        order.Should().NotBeNull();
        return order!.PlacementTimestamp;
    }
}
