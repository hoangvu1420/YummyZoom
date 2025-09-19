using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.Coupons.Queries.ListCouponsByRestaurant;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Domain.CouponAggregate;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Application.FunctionalTests.Features.Coupons.Queries;

using static Testing;

public class ListCouponsByRestaurantTests : BaseTestFixture
{
    [Test]
    public async Task ListCoupons_ReturnsPagedData_AndExcludesDeleted()
    {
        var (restaurantGuid, _) = await TestDataFactory.CreateSecondRestaurantWithMenuItemsAsync();
        await RunAsRestaurantStaffAsync("staff-list@restaurant.com", restaurantGuid);

        var restaurantId = RestaurantId.Create(restaurantGuid);
        var now = DateTime.UtcNow;

        var value = CouponValue.CreatePercentage(10).Value;
        var applies = AppliesTo.CreateForWholeOrder().Value;

        var activeCoupon = Coupon.Create(
            restaurantId,
            code: "LIST_ACTIVE10",
            description: "Active coupon",
            value,
            applies,
            validityStartDate: now.AddDays(-5),
            validityEndDate: now.AddDays(5),
            minOrderAmount: null,
            totalUsageLimit: null,
            usageLimitPerUser: null,
            isEnabled: true).Value;
        activeCoupon.ClearDomainEvents();
        await AddAsync(activeCoupon);

        var disabledCoupon = Coupon.Create(
            restaurantId,
            code: "LIST_DISABLE10",
            description: "Disabled coupon",
            value,
            applies,
            validityStartDate: now.AddDays(-2),
            validityEndDate: now.AddDays(10),
            minOrderAmount: null,
            totalUsageLimit: null,
            usageLimitPerUser: null,
            isEnabled: true).Value;
        disabledCoupon.ClearDomainEvents();
        disabledCoupon.Disable();
        disabledCoupon.ClearDomainEvents();
        await AddAsync(disabledCoupon);

        var deletedCoupon = Coupon.Create(
            restaurantId,
            code: "LIST_DELETED10",
            description: "Deleted coupon",
            value,
            applies,
            validityStartDate: now.AddDays(-1),
            validityEndDate: now.AddDays(3),
            minOrderAmount: null,
            totalUsageLimit: null,
            usageLimitPerUser: null,
            isEnabled: true).Value;
        deletedCoupon.ClearDomainEvents();
        await AddAsync(deletedCoupon);
        deletedCoupon.MarkAsDeleted(DateTimeOffset.UtcNow);
        deletedCoupon.ClearDomainEvents();
        await UpdateAsync(deletedCoupon);

        var result = await SendAsync(new ListCouponsByRestaurantQuery(
            RestaurantId: restaurantGuid,
            PageNumber: 1,
            PageSize: 10));

        result.ShouldBeSuccessful();
        var page = result.Value;
        page.TotalCount.Should().BeGreaterOrEqualTo(2);
        page.Items.Should().OnlyContain(c => c.Code != "LIST_DELETED10");
        page.Items.Select(c => c.Code).Should().BeEquivalentTo(new[] { "LIST_ACTIVE10", "LIST_DISABLE10" });
    }

    [Test]
    public async Task ListCoupons_FilterByEnabled_ReturnsOnlyMatching()
    {
        var (restaurantGuid, _) = await TestDataFactory.CreateSecondRestaurantWithMenuItemsAsync();
        await RunAsRestaurantStaffAsync("staff-enabled@restaurant.com", restaurantGuid);

        var restaurantId = RestaurantId.Create(restaurantGuid);
        var now = DateTime.UtcNow;
        var value = CouponValue.CreatePercentage(15).Value;
        var applies = AppliesTo.CreateForWholeOrder().Value;

        var enabledCoupon = Coupon.Create(
            restaurantId,
            code: "LIST_ENABLED15",
            description: "Enabled coupon",
            value,
            applies,
            validityStartDate: now.AddDays(-1),
            validityEndDate: now.AddDays(20),
            isEnabled: true).Value;
        enabledCoupon.ClearDomainEvents();
        await AddAsync(enabledCoupon);

        var disabledCoupon = Coupon.Create(
            restaurantId,
            code: "LIST_DISABLED15",
            description: "Disabled coupon",
            value,
            applies,
            validityStartDate: now.AddDays(-1),
            validityEndDate: now.AddDays(20),
            isEnabled: true).Value;
        disabledCoupon.ClearDomainEvents();
        disabledCoupon.Disable();
        disabledCoupon.ClearDomainEvents();
        await AddAsync(disabledCoupon);

        var result = await SendAsync(new ListCouponsByRestaurantQuery(
            RestaurantId: restaurantGuid,
            PageNumber: 1,
            PageSize: 20,
            Q: null,
            IsEnabled: false,
            ActiveFrom: null,
            ActiveTo: null));

        result.ShouldBeSuccessful();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items.First().Code.Should().Be("LIST_DISABLED15");
        result.Value.Items.First().IsEnabled.Should().BeFalse();
    }

    [Test]
    public async Task ListCoupons_FilterByActiveWindow_ReturnsOverlapping()
    {
        var (restaurantGuid, _) = await TestDataFactory.CreateSecondRestaurantWithMenuItemsAsync();
        await RunAsRestaurantStaffAsync("staff-window@restaurant.com", restaurantGuid);

        var restaurantId = RestaurantId.Create(restaurantGuid);
        var now = DateTime.UtcNow;
        var value = CouponValue.CreatePercentage(5).Value;
        var applies = AppliesTo.CreateForWholeOrder().Value;

        var early = Coupon.Create(
            restaurantId,
            code: "LIST_EARLY5",
            description: "Early window",
            value,
            applies,
            validityStartDate: now.AddDays(-10),
            validityEndDate: now.AddDays(-5),
            isEnabled: true).Value;
        early.ClearDomainEvents();
        await AddAsync(early);

        var overlapping = Coupon.Create(
            restaurantId,
            code: "LIST_MID5",
            description: "Mid window",
            value,
            applies,
            validityStartDate: now.AddDays(-1),
            validityEndDate: now.AddDays(3),
            isEnabled: true).Value;
        overlapping.ClearDomainEvents();
        await AddAsync(overlapping);

        var late = Coupon.Create(
            restaurantId,
            code: "LIST_LATE5",
            description: "Late window",
            value,
            applies,
            validityStartDate: now.AddDays(5),
            validityEndDate: now.AddDays(10),
            isEnabled: true).Value;
        late.ClearDomainEvents();
        await AddAsync(late);

        var result = await SendAsync(new ListCouponsByRestaurantQuery(
            RestaurantId: restaurantGuid,
            PageNumber: 1,
            PageSize: 10,
            Q: null,
            IsEnabled: null,
            ActiveFrom: now,
            ActiveTo: now.AddDays(4)));

        result.ShouldBeSuccessful();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items.First().Code.Should().Be("LIST_MID5");
    }

    [Test]
    public async Task ListCoupons_SearchByQuery_FiltersByCodeOrDescription()
    {
        var (restaurantGuid, _) = await TestDataFactory.CreateSecondRestaurantWithMenuItemsAsync();
        await RunAsRestaurantStaffAsync("staff-search@restaurant.com", restaurantGuid);

        var restaurantId = RestaurantId.Create(restaurantGuid);
        var now = DateTime.UtcNow;
        var value = CouponValue.CreatePercentage(8).Value;
        var applies = AppliesTo.CreateForWholeOrder().Value;

        var match = Coupon.Create(
            restaurantId,
            code: "LIST_FALLSAVE",
            description: "Autumn special savings",
            value,
            applies,
            validityStartDate: now.AddDays(-1),
            validityEndDate: now.AddDays(8),
            isEnabled: true).Value;
        match.ClearDomainEvents();
        await AddAsync(match);

        var nonMatch = Coupon.Create(
            restaurantId,
            code: "LIST_WINTER",
            description: "Winter promo",
            value,
            applies,
            validityStartDate: now.AddDays(-1),
            validityEndDate: now.AddDays(8),
            isEnabled: true).Value;
        nonMatch.ClearDomainEvents();
        await AddAsync(nonMatch);

        var result = await SendAsync(new ListCouponsByRestaurantQuery(
            RestaurantId: restaurantGuid,
            PageNumber: 1,
            PageSize: 10,
            Q: "fall",
            IsEnabled: null,
            ActiveFrom: null,
            ActiveTo: null));

        result.ShouldBeSuccessful();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items.First().Code.Should().Be("LIST_FALLSAVE");
    }

    [Test]
    public async Task ListCoupons_WrongRestaurantScope_ThrowsForbidden()
    {
        var (secondRestaurantId, _) = await TestDataFactory.CreateSecondRestaurantWithMenuItemsAsync();
        await RunAsRestaurantStaffAsync("staff-second@restaurant.com", secondRestaurantId);

        await FluentActions.Invoking(() => SendAsync(new ListCouponsByRestaurantQuery(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            PageNumber: 1,
            PageSize: 10)))
            .Should().ThrowAsync<ForbiddenAccessException>();
    }
}

