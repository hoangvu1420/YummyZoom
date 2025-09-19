using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.Coupons.Queries.GetCouponDetails;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CouponAggregate;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Application.FunctionalTests.Features.Coupons.Queries;

using static Testing;

public class GetCouponDetailsTests : BaseTestFixture
{
    [Test]
    public async Task GetCouponDetails_ReturnsDetailedInfo()
    {
        var (restaurantId, menuItemId) = await TestDataFactory.CreateSecondRestaurantWithMenuItemsAsync();
        await RunAsRestaurantStaffAsync("staff-details@restaurant.com", restaurantId);

        var restaurant = RestaurantId.Create(restaurantId);
        var menuItem = await FindAsync<MenuItem>(MenuItemId.Create(menuItemId));
        menuItem.Should().NotBeNull();

        var value = CouponValue.CreateFixedAmount(new Money(5, "USD")).Value;
        var applies = AppliesTo.CreateForSpecificItems(new List<MenuItemId> { MenuItemId.Create(menuItemId) }).Value;

        var now = DateTime.UtcNow;
        var coupon = Coupon.Create(
            restaurant,
            code: "DETAILS5",
            description: "Detailed coupon",
            value,
            applies,
            validityStartDate: now.AddDays(-1),
            validityEndDate: now.AddDays(6),
            minOrderAmount: new Money(25, "USD"),
            totalUsageLimit: 100,
            usageLimitPerUser: 3,
            isEnabled: true).Value;
        coupon.ClearDomainEvents();
        await AddAsync(coupon);

        var result = await SendAsync(new GetCouponDetailsQuery(restaurantId, coupon.Id.Value));

        result.ShouldBeSuccessful();
        var dto = result.Value;
        dto.CouponId.Should().Be(coupon.Id.Value);
        dto.Code.Should().Be("DETAILS5");
        dto.ValueType.Should().Be(CouponType.FixedAmount);
        dto.FixedAmount.Should().Be(5);
        dto.Scope.Should().Be(CouponScope.SpecificItems);
        dto.ItemIds.Should().Contain(menuItemId);
        dto.CategoryIds.Should().BeEmpty();
        dto.MinOrderAmount.Should().Be(25);
        dto.MinOrderCurrency.Should().Be("USD");
        dto.TotalUsageLimit.Should().Be(100);
        dto.UsageLimitPerUser.Should().Be(3);
        dto.IsEnabled.Should().BeTrue();
    }

    [Test]
    public async Task GetCouponDetails_NotFound_ReturnsFailure()
    {
        await RunAsRestaurantStaffAsync("staff-notfound@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var query = new GetCouponDetailsQuery(Testing.TestData.DefaultRestaurantId, Guid.NewGuid());
        var result = await SendAsync(query);

        result.ShouldBeFailure("Coupon.Details.NotFound");
    }

    [Test]
    public async Task GetCouponDetails_WrongRestaurant_ThrowsForbidden()
    {
        var (restaurantId, _) = await TestDataFactory.CreateSecondRestaurantWithMenuItemsAsync();
        await RunAsRestaurantStaffAsync("staff-owner@restaurant.com", restaurantId);

        var query = new GetCouponDetailsQuery(Testing.TestData.DefaultRestaurantId, Testing.TestData.DefaultCouponId);

        await FluentActions.Invoking(() => SendAsync(query))
            .Should().ThrowAsync<ForbiddenAccessException>();
    }
}

