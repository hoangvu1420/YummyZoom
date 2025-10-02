using YummyZoom.Application.Coupons.Commands.DisableCoupon;
using YummyZoom.Application.Coupons.Commands.EnableCoupon;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Domain.CouponAggregate;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Coupons.Commands;

public class EnableDisableCouponTests : BaseTestFixture
{
    [Test]
    public async Task DisableCoupon_Succeeds_AndPersists()
    {
        await RunAsRestaurantStaffAsync("owner@r.com", Testing.TestData.DefaultRestaurantId);

        var disable = new DisableCouponCommand(Testing.TestData.DefaultRestaurantId, Testing.TestData.DefaultCouponId);
        var result = await SendAsync(disable);
        result.ShouldBeSuccessful();

        var agg = await FindAsync<Coupon>(CouponId.Create(Testing.TestData.DefaultCouponId));
        agg!.IsEnabled.Should().BeFalse();
    }

    [Test]
    public async Task EnableCoupon_Succeeds_AndPersists()
    {
        await RunAsRestaurantStaffAsync("owner@r.com", Testing.TestData.DefaultRestaurantId);

        // Ensure disabled first (idempotent if already disabled/enabled)
        await SendAsync(new DisableCouponCommand(Testing.TestData.DefaultRestaurantId, Testing.TestData.DefaultCouponId));

        var enable = new EnableCouponCommand(Testing.TestData.DefaultRestaurantId, Testing.TestData.DefaultCouponId);
        var result = await SendAsync(enable);
        result.ShouldBeSuccessful();

        var agg = await FindAsync<Coupon>(CouponId.Create(Testing.TestData.DefaultCouponId));
        agg!.IsEnabled.Should().BeTrue();
    }
}

