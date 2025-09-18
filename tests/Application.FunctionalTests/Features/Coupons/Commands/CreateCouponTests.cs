using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.Coupons.Commands.CreateCoupon;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Coupons.Commands;

public class CreateCouponTests : BaseTestFixture
{
    [Test]
    public async Task Create_Percentage_WholeOrder_Succeeds()
    {
        await RunAsRestaurantStaffAsync("owner@r.com", Testing.TestData.DefaultRestaurantId);

        var now = DateTime.UtcNow;
        var cmd = new CreateCouponCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Code: "save10",
            Description: "Ten off",
            ValueType: CouponType.Percentage,
            Percentage: 10,
            FixedAmount: null,
            FixedCurrency: null,
            FreeItemId: null,
            Scope: CouponScope.WholeOrder,
            ItemIds: null,
            CategoryIds: null,
            ValidityStartDate: now.AddDays(-1),
            ValidityEndDate: now.AddDays(10),
            MinOrderAmount: null,
            MinOrderCurrency: null,
            TotalUsageLimit: null,
            UsageLimitPerUser: null,
            IsEnabled: true);

        var result = await SendAsync(cmd);
        result.ShouldBeSuccessful();
        result.Value.CouponId.Should().NotBeEmpty();
    }
}

