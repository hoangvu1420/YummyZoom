using System.Net;
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Web.ApiContractTests.Infrastructure;
using YummyZoom.SharedKernel;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Orders.Queries.Common;
using YummyZoom.Application.Orders.Queries.GetRestaurantActiveOrders;
using System.Text.Json;

namespace YummyZoom.Web.ApiContractTests.Restaurants;

// Contract tests for GET /api/v1/restaurants/{restaurantId}/orders/active
public class RestaurantActiveOrdersContractTests
{
    private static OrderSummaryDto CreateSummary(Guid orderId)
        => new(orderId, "ORD-A1", "Accepted", DateTime.UtcNow.AddMinutes(-15), Guid.NewGuid(), Guid.NewGuid(), 22m, "USD", 3);

    [Test]
    public async Task GetRestaurantActiveOrders_WhenNonEmpty_Returns200WithItems()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        var restId = Guid.NewGuid();
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<GetRestaurantActiveOrdersQuery>();
            var q = (GetRestaurantActiveOrdersQuery)req;
            q.RestaurantGuid.Should().Be(restId);
            q.PageNumber.Should().Be(1);
            q.PageSize.Should().Be(25);
            var list = new PaginatedList<OrderSummaryDto>(new[] { CreateSummary(Guid.NewGuid()) }, 1, 1, 25);
            return YummyZoom.SharedKernel.Result.Success(list);
        });
        var path = $"/api/v1/restaurants/{restId}/orders/active?pageNumber=1&pageSize=25";
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(raw);
        doc.RootElement.GetProperty("items").GetArrayLength().Should().Be(1);
    }

    [Test]
    public async Task GetRestaurantActiveOrders_WhenEmpty_Returns200WithEmptyItems()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        var restId = Guid.NewGuid();
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<GetRestaurantActiveOrdersQuery>();
            var list = new PaginatedList<OrderSummaryDto>(Array.Empty<OrderSummaryDto>(), 0, 2, 10);
            return YummyZoom.SharedKernel.Result.Success(list);
        });
        var path = $"/api/v1/restaurants/{restId}/orders/active?pageNumber=2&pageSize=10";
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(raw);
        doc.RootElement.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    [Test]
    public async Task GetRestaurantActiveOrders_WithoutAuth_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        var path = $"/api/v1/restaurants/{Guid.NewGuid()}/orders/active?pageNumber=1&pageSize=10";
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
