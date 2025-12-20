using System.Net;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Orders.Queries.Common;
using YummyZoom.Application.Orders.Queries.GetRestaurantOrderHistory;
using YummyZoom.Web.ApiContractTests.Infrastructure;

namespace YummyZoom.Web.ApiContractTests.Restaurants;

// Contract tests for GET /api/v1/restaurants/{restaurantId}/orders/history
public class RestaurantOrderHistoryContractTests
{
    private static OrderHistorySummaryDto CreateSummary(Guid orderId)
        => new(orderId, "ORD-H1", "Delivered", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, 25m, "USD", 2, "Test Customer", "0900000000", "Paid", "CashOnDelivery");

    [Test]
    public async Task GetRestaurantOrderHistory_WhenNonEmpty_Returns200WithItems()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        var restId = Guid.NewGuid();
        var from = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2025, 1, 31, 23, 59, 59, DateTimeKind.Utc);
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<GetRestaurantOrderHistoryQuery>();
            var q = (GetRestaurantOrderHistoryQuery)req;
            q.RestaurantGuid.Should().Be(restId);
            q.PageNumber.Should().Be(1);
            q.PageSize.Should().Be(25);
            q.From.Should().Be(from);
            q.To.Should().Be(to);
            q.Statuses.Should().Be("Delivered,Cancelled");
            q.Keyword.Should().Be("Nguyen");
            var list = new PaginatedList<OrderHistorySummaryDto>(new[] { CreateSummary(Guid.NewGuid()) }, 1, 1, 25);
            return YummyZoom.SharedKernel.Result.Success(list);
        });
        var path = $"/api/v1/restaurants/{restId}/orders/history?pageNumber=1&pageSize=25&from={Uri.EscapeDataString(from.ToString("O"))}&to={Uri.EscapeDataString(to.ToString("O"))}&statuses=Delivered,Cancelled&keyword=Nguyen";
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(raw);
        doc.RootElement.GetProperty("items").GetArrayLength().Should().Be(1);
    }

    [Test]
    public async Task GetRestaurantOrderHistory_WhenEmpty_Returns200WithEmptyItems()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        var restId = Guid.NewGuid();
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<GetRestaurantOrderHistoryQuery>();
            var list = new PaginatedList<OrderHistorySummaryDto>(Array.Empty<OrderHistorySummaryDto>(), 0, 2, 10);
            return YummyZoom.SharedKernel.Result.Success(list);
        });
        var path = $"/api/v1/restaurants/{restId}/orders/history?pageNumber=2&pageSize=10";
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(raw);
        doc.RootElement.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    [Test]
    public async Task GetRestaurantOrderHistory_WithoutAuth_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        var path = $"/api/v1/restaurants/{Guid.NewGuid()}/orders/history?pageNumber=1&pageSize=10";
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
