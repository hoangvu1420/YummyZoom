using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Web.ApiContractTests.Infrastructure;
using YummyZoom.SharedKernel;
using YummyZoom.Application.Coupons.Commands.CreateCoupon;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Application.Coupons.Commands.UpdateCoupon;

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
    public async Task UpdateCoupon_WithAuth_MapsRequest_AndReturns204()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<UpdateCouponCommand>();
            var cmd = (UpdateCouponCommand)req;
            cmd.ValueType.Should().Be(CouponType.Percentage);
            cmd.Scope.Should().Be(CouponScope.WholeOrder);
            cmd.ValidityStartDate.Should().BeBefore(cmd.ValidityEndDate);
            return Result.Success();
        });

        var restaurantId = Guid.NewGuid();
        var couponId = Guid.NewGuid();
        var body = new YummyZoom.Web.Endpoints.Restaurants.UpdateCouponRequestDto(
            Description: "Updated",
            ValidityStartDate: DateTime.UtcNow.AddDays(-2),
            ValidityEndDate: DateTime.UtcNow.AddDays(20),
            ValueType: CouponType.Percentage,
            Percentage: 15,
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

        var path = $"/api/v1/restaurants/{restaurantId}/coupons/{couponId}";
        var resp = await client.PutAsJsonAsync(path, body);
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task UpdateCoupon_WithoutAuth_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        var restaurantId = Guid.NewGuid();
        var couponId = Guid.NewGuid();
        var body = new YummyZoom.Web.Endpoints.Restaurants.UpdateCouponRequestDto(
            Description: "Updated",
            ValidityStartDate: DateTime.UtcNow.AddDays(-2),
            ValidityEndDate: DateTime.UtcNow.AddDays(20),
            ValueType: CouponType.Percentage,
            Percentage: 15,
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
        var resp = await client.PutAsJsonAsync($"/api/v1/restaurants/{restaurantId}/coupons/{couponId}", body);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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
