using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Web.ApiContractTests.Infrastructure;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Coupons.Queries.ListCouponsByRestaurant;
using YummyZoom.Application.Coupons.Queries.GetCouponDetails;
using YummyZoom.Application.Coupons.Commands.CreateCoupon;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Application.Coupons.Commands.UpdateCoupon;
using YummyZoom.Application.Coupons.Commands.EnableCoupon;
using YummyZoom.Application.Coupons.Commands.DisableCoupon;
using YummyZoom.Application.Coupons.Commands.DeleteCoupon;
using Result = YummyZoom.SharedKernel.Result;

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
    public async Task EnableCoupon_WithAuth_Returns204()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<EnableCouponCommand>();
            return Result.Success();
        });

        var restaurantId = Guid.NewGuid();
        var couponId = Guid.NewGuid();
        var resp = await client.PutAsync($"/api/v1/restaurants/{restaurantId}/coupons/{couponId}/enable", null);
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task EnableCoupon_WithoutAuth_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        var restaurantId = Guid.NewGuid();
        var couponId = Guid.NewGuid();
        var resp = await client.PutAsync($"/api/v1/restaurants/{restaurantId}/coupons/{couponId}/enable", null);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task DisableCoupon_WithAuth_Returns204()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<DisableCouponCommand>();
            return Result.Success();
        });

        var restaurantId = Guid.NewGuid();
        var couponId = Guid.NewGuid();
        var resp = await client.PutAsync($"/api/v1/restaurants/{restaurantId}/coupons/{couponId}/disable", null);
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task DisableCoupon_WithoutAuth_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        var restaurantId = Guid.NewGuid();
        var couponId = Guid.NewGuid();
        var resp = await client.PutAsync($"/api/v1/restaurants/{restaurantId}/coupons/{couponId}/disable", null);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task DeleteCoupon_WithAuth_Returns204()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<DeleteCouponCommand>();
            return Result.Success();
        });

        var restaurantId = Guid.NewGuid();
        var couponId = Guid.NewGuid();
        var resp = await client.DeleteAsync($"/api/v1/restaurants/{restaurantId}/coupons/{couponId}");
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task DeleteCoupon_WithoutAuth_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();

        var restaurantId = Guid.NewGuid();
        var couponId = Guid.NewGuid();
        var resp = await client.DeleteAsync($"/api/v1/restaurants/{restaurantId}/coupons/{couponId}");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ListCoupons_WithAuth_Returns200()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        var from = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-1), DateTimeKind.Utc);
        var to = from.AddDays(7);

        var summary = new CouponSummaryDto(
            CouponId: Guid.NewGuid(),
            Code: "SAVE10",
            Description: "Ten off",
            ValueType: CouponType.Percentage,
            Percentage: 10,
            FixedAmount: null,
            FixedCurrency: null,
            FreeItemId: null,
            Scope: CouponScope.WholeOrder,
            ValidityStartDate: from.Date,
            ValidityEndDate: to.Date,
            MinOrderAmount: null,
            MinOrderCurrency: null,
            TotalUsageLimit: null,
            CurrentTotalUsageCount: 0,
            UsageLimitPerUser: null,
            IsEnabled: true,
            Created: DateTimeOffset.UtcNow,
            LastModified: DateTimeOffset.UtcNow);

        var page = new PaginatedList<CouponSummaryDto>(new[] { summary }, 1, 2, 5);

        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<ListCouponsByRestaurantQuery>();
            var query = (ListCouponsByRestaurantQuery)req;
            query.PageNumber.Should().Be(2);
            query.PageSize.Should().Be(5);
            query.Q.Should().Be("promo");
            query.IsEnabled.Should().BeTrue();
            query.ActiveFrom.Should().Be(from);
            query.ActiveTo.Should().Be(to);
            return Result.Success(page);
        });

        var restaurantId = Guid.NewGuid();
        var path = $"/api/v1/restaurants/{restaurantId}/coupons?pageNumber=2&pageSize=5&q=promo&enabled=true&from={Uri.EscapeDataString(from.ToString("O"))}&to={Uri.EscapeDataString(to.ToString("O"))}";
        var resp = await client.GetAsync(path);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine(json);
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("items").GetArrayLength().Should().Be(1);
    }

    [Test]
    public async Task ListCoupons_WithoutAuth_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        var resp = await client.GetAsync($"/api/v1/restaurants/{Guid.NewGuid()}/coupons");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetCouponDetails_WithAuth_Returns200()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        var restaurantId = Guid.NewGuid();
        var couponId = Guid.NewGuid();
        var dto = new CouponDetailsDto(
            CouponId: couponId,
            Code: "DETAIL10",
            Description: "Detail coupon",
            ValueType: CouponType.Percentage,
            Percentage: 10,
            FixedAmount: null,
            FixedCurrency: null,
            FreeItemId: null,
            Scope: CouponScope.SpecificItems,
            ItemIds: new List<Guid> { Guid.NewGuid() },
            CategoryIds: Array.Empty<Guid>(),
            ValidityStartDate: DateTime.UtcNow.AddDays(-1),
            ValidityEndDate: DateTime.UtcNow.AddDays(5),
            MinOrderAmount: 25,
            MinOrderCurrency: "USD",
            TotalUsageLimit: 100,
            CurrentTotalUsageCount: 3,
            UsageLimitPerUser: 2,
            IsEnabled: true,
            Created: DateTimeOffset.UtcNow.AddDays(-2),
            LastModified: DateTimeOffset.UtcNow);

        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<GetCouponDetailsQuery>();
            var query = (GetCouponDetailsQuery)req;
            query.RestaurantId.Should().Be(restaurantId);
            query.CouponId.Should().Be(couponId);
            return Result.Success(dto);
        });

        var resp = await client.GetAsync($"/api/v1/restaurants/{restaurantId}/coupons/{couponId}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("code").GetString().Should().Be("DETAIL10");
        doc.RootElement.GetProperty("scope").GetString().Should().Be("SpecificItems");
    }

    [Test]
    public async Task GetCouponDetails_NotFound_Returns404()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        var restaurantId = Guid.NewGuid();
        var couponId = Guid.NewGuid();

        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<GetCouponDetailsQuery>();
            return Result.Failure<CouponDetailsDto>(GetCouponDetailsErrors.NotFound(couponId));
        });

        var resp = await client.GetAsync($"/api/v1/restaurants/{restaurantId}/coupons/{couponId}");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetCouponDetails_WithoutAuth_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        var resp = await client.GetAsync($"/api/v1/restaurants/{Guid.NewGuid()}/coupons/{Guid.NewGuid()}");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

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


