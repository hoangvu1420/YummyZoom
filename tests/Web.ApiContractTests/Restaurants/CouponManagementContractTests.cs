using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Web.ApiContractTests.Infrastructure;
using YummyZoom.SharedKernel;
using YummyZoom.Application.Coupons.Commands.CreateCoupon;
using YummyZoom.Domain.CouponAggregate.ValueObjects;

namespace YummyZoom.Web.ApiContractTests.Restaurants;

public class CouponManagementContractTests
{
    [Test]
    public async Task CreateCoupon_WithAuth_MapsRequest_AndReturns201()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        var expectedId = Guid.NewGuid();
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<CreateCouponCommand>();
            var cmd = (CreateCouponCommand)req;
            cmd.RestaurantId.Should().NotBeEmpty();
            cmd.ValueType.Should().Be(CouponType.Percentage);
            cmd.Scope.Should().Be(CouponScope.WholeOrder);
            return Result.Success(new CreateCouponResponse(expectedId));
        });

        var restaurantId = Guid.NewGuid();
        var body = new YummyZoom.Web.Endpoints.Restaurants.CreateCouponRequestDto(
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
            ValidityStartDate: DateTime.UtcNow.AddDays(-1),
            ValidityEndDate: DateTime.UtcNow.AddDays(7),
            MinOrderAmount: null,
            MinOrderCurrency: null,
            TotalUsageLimit: null,
            UsageLimitPerUser: null,
            IsEnabled: true);

        var path = $"/api/v1/restaurants/{restaurantId}/coupons";
        var resp = await client.PostAsJsonAsync(path, body);
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await resp.Content.ReadAsStringAsync();
        json.Should().Contain("couponId");
    }

    [Test]
    public async Task CreateCoupon_WithoutAuth_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        var restaurantId = Guid.NewGuid();
        var body = new YummyZoom.Web.Endpoints.Restaurants.CreateCouponRequestDto(
            Code: "S",
            Description: "D",
            ValueType: CouponType.Percentage,
            Percentage: 5,
            FixedAmount: null,
            FixedCurrency: null,
            FreeItemId: null,
            Scope: CouponScope.WholeOrder,
            ItemIds: null,
            CategoryIds: null,
            ValidityStartDate: DateTime.UtcNow.AddDays(-1),
            ValidityEndDate: DateTime.UtcNow.AddDays(3),
            MinOrderAmount: null,
            MinOrderCurrency: null,
            TotalUsageLimit: null,
            UsageLimitPerUser: null,
            IsEnabled: true);
        var resp = await client.PostAsJsonAsync($"/api/v1/restaurants/{restaurantId}/coupons", body);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

