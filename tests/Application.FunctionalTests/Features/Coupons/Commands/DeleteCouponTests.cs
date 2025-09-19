using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.Coupons.Commands.DeleteCoupon;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Domain.CouponAggregate;
using YummyZoom.Domain.CouponAggregate.ValueObjects;

namespace YummyZoom.Application.FunctionalTests.Features.Coupons.Commands;

using static Testing;

public class DeleteCouponTests : BaseTestFixture
{
    [Test]
    public async Task DeleteCoupon_SoftDeletesAndPersists()
    {
        await RunAsRestaurantStaffAsync("owner@r.com", Testing.TestData.DefaultRestaurantId);

        var cmd = new DeleteCouponCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            CouponId: Testing.TestData.DefaultCouponId);

        var result = await SendAsync(cmd);
        result.ShouldBeSuccessful();

        var coupon = await FindAsync<Coupon>(CouponId.Create(Testing.TestData.DefaultCouponId));
        coupon.Should().BeNull();
    }

    [Test]
    public async Task DeleteCoupon_NotFound_ReturnsFailure()
    {
        await RunAsRestaurantStaffAsync("owner@r.com", Testing.TestData.DefaultRestaurantId);

        var cmd = new DeleteCouponCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            CouponId: Guid.NewGuid());

        var result = await SendAsync(cmd);
        result.ShouldBeFailure("Coupon.NotFound");
    }

    [Test]
    public async Task DeleteCoupon_WrongRestaurant_ThrowsForbidden()
    {
        var (otherRestaurantId, _) = await TestDataFactory.CreateSecondRestaurantWithMenuItemsAsync();
        await RunAsRestaurantStaffAsync("staff-second@restaurant.com", otherRestaurantId);

        var cmd = new DeleteCouponCommand(
            RestaurantId: otherRestaurantId,
            CouponId: Testing.TestData.DefaultCouponId);

        await FluentActions.Invoking(() => SendAsync(cmd))
            .Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task DeleteCoupon_InvalidIds_FailsValidation()
    {
        await RunAsRestaurantStaffAsync("staff-invalid@restaurant.com", Testing.TestData.DefaultRestaurantId);

        await FluentActions.Invoking(() => SendAsync(new DeleteCouponCommand(
            RestaurantId: Guid.Empty,
            CouponId: Guid.Empty)))
            .Should().ThrowAsync<ValidationException>();
    }
}
