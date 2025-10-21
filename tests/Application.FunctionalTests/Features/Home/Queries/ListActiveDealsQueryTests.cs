using FluentAssertions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.Home.Queries.ActiveDeals;
using YummyZoom.Domain.CouponAggregate;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Application.FunctionalTests.Features.Home.Queries;

using static Testing;

public class ListActiveDealsQueryTests : BaseTestFixture
{
    [Test]
    public async Task Returns_Only_Restaurants_With_Active_Enabled_Coupons()
    {
        var rWithDeal = await CreateRestaurantAsync("Deals Resto");
        var rNoDeal   = await CreateRestaurantAsync("No Deals Resto");

        var now = DateTime.UtcNow;
        var value = CouponValue.CreatePercentage(15).Value;
        var applies = AppliesTo.CreateForWholeOrder().Value;

        var coupon = Coupon.Create(
            RestaurantId.Create(rWithDeal),
            code: "ACTIVE15",
            description: "15% off",
            value,
            applies,
            validityStartDate: now.AddDays(-1),
            validityEndDate: now.AddDays(7),
            minOrderAmount: null,
            totalUsageLimit: null,
            usageLimitPerUser: null,
            isEnabled: true).Value;
        coupon.ClearDomainEvents();
        await AddAsync(coupon);

        var result = await SendAsync(new ListActiveDealsQuery(10));
        result.ShouldBeSuccessful();

        var list = result.Value;
        list.Should().NotBeEmpty();
        list.Any(x => x.RestaurantId == rWithDeal).Should().BeTrue();
        list.Any(x => x.RestaurantId == rNoDeal).Should().BeFalse();
        list.First(x => x.RestaurantId == rWithDeal).BestCouponLabel.Should().Contain("15");
    }

    [Test]
    public async Task Picks_Best_Label_Per_Restaurant_By_Value()
    {
        var r = await CreateRestaurantAsync("Label Rank Resto");
        var now = DateTime.UtcNow;

        var pct10 = Coupon.Create(
            RestaurantId.Create(r),
            code: "PCT10",
            description: "10%",
            CouponValue.CreatePercentage(10).Value,
            AppliesTo.CreateForWholeOrder().Value,
            validityStartDate: now.AddDays(-1),
            validityEndDate: now.AddDays(10),
            minOrderAmount: null,
            totalUsageLimit: null,
            usageLimitPerUser: null,
            isEnabled: true).Value;
        pct10.ClearDomainEvents();
        await AddAsync(pct10);

        var pct20 = Coupon.Create(
            RestaurantId.Create(r),
            code: "PCT20",
            description: "20%",
            CouponValue.CreatePercentage(20).Value,
            AppliesTo.CreateForWholeOrder().Value,
            validityStartDate: now.AddDays(-1),
            validityEndDate: now.AddDays(5),
            minOrderAmount: null,
            totalUsageLimit: null,
            usageLimitPerUser: null,
            isEnabled: true).Value;
        pct20.ClearDomainEvents();
        await AddAsync(pct20);

        var fixed5 = Coupon.Create(
            RestaurantId.Create(r),
            code: "FIX5",
            description: "5 off",
            CouponValue.CreateFixedAmount(new YummyZoom.Domain.Common.ValueObjects.Money(5, "USD")).Value,
            AppliesTo.CreateForWholeOrder().Value,
            validityStartDate: now.AddDays(-1),
            validityEndDate: now.AddDays(5),
            minOrderAmount: null,
            totalUsageLimit: null,
            usageLimitPerUser: null,
            isEnabled: true).Value;
        fixed5.ClearDomainEvents();
        await AddAsync(fixed5);

        var res = await SendAsync(new ListActiveDealsQuery(5));
        res.ShouldBeSuccessful();
        var card = res.Value.First(x => x.RestaurantId == r);
        card.BestCouponLabel.Should().Contain("20");
    }

    private static async Task<Guid> CreateRestaurantAsync(string name)
    {
        var address = Address.Create("1 St", "C", "S", "Z", "US").Value;
        var contact = ContactInfo.Create("+1-555-0000", "resto@test.local").Value;
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
}
