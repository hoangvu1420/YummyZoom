using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.Coupons.Commands.UpdateCoupon;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Domain.CouponAggregate;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Coupons.Commands;

public class UpdateCouponTests : BaseTestFixture
{
    [Test]
    public async Task UpdateCoupon_Validity_And_Value_And_Scope_Succeeds()
    {
        await RunAsRestaurantStaffAsync("owner@r.com", Testing.TestData.DefaultRestaurantId);

        var now = DateTime.UtcNow;
        var cmd = new UpdateCouponCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            CouponId: Testing.TestData.DefaultCouponId,
            Description: "Updated description",
            ValidityStartDate: now.AddDays(-2),
            ValidityEndDate: now.AddDays(20),
            ValueType: CouponType.Percentage,
            Percentage: 15,
            FixedAmount: null,
            FixedCurrency: null,
            FreeItemId: null,
            Scope: CouponScope.WholeOrder,
            ItemIds: null,
            CategoryIds: null,
            MinOrderAmount: 30,
            MinOrderCurrency: "USD",
            TotalUsageLimit: 500,
            UsageLimitPerUser: 5);

        var result = await SendAsync(cmd);
        result.ShouldBeSuccessful();

        var agg = await FindAsync<Coupon>(CouponId.Create(Testing.TestData.DefaultCouponId));
        agg!.Description.Should().Be("Updated description");
        agg.ValidityStartDate.Should().BeCloseTo(now.AddDays(-2), TimeSpan.FromSeconds(5));
        agg.ValidityEndDate.Should().BeCloseTo(now.AddDays(20), TimeSpan.FromSeconds(5));
        agg.Value.Type.Should().Be(CouponType.Percentage);
        agg.MinOrderAmount!.Amount.Should().Be(30);
        agg.MinOrderAmount.Currency.Should().Be("USD");
        agg.TotalUsageLimit.Should().Be(500);
        agg.UsageLimitPerUser.Should().Be(5);
        agg.RestaurantId.Should().Be(RestaurantId.Create(Testing.TestData.DefaultRestaurantId));
    }

    [Test]
    public async Task UpdateCoupon_InvalidDates_FailsValidation()
    {
        await RunAsRestaurantStaffAsync("owner@r.com", Testing.TestData.DefaultRestaurantId);

        var now = DateTime.UtcNow;
        var cmd = new UpdateCouponCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            CouponId: Testing.TestData.DefaultCouponId,
            Description: "Bad dates",
            ValidityStartDate: now.AddDays(2),
            ValidityEndDate: now.AddDays(1),
            ValueType: CouponType.Percentage,
            Percentage: 10,
            FixedAmount: null,
            FixedCurrency: null,
            FreeItemId: null,
            Scope: CouponScope.WholeOrder,
            ItemIds: null,
            CategoryIds: null,
            MinOrderAmount: null,
            MinOrderCurrency: null,
            TotalUsageLimit: null,
            UsageLimitPerUser: null);

        await FluentActions.Invoking(() => SendAsync(cmd))
            .Should().ThrowAsync<ValidationException>();
    }
}
