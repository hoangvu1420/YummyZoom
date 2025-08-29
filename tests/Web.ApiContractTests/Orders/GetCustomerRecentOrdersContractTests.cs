using System.Net;
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Web.ApiContractTests.Infrastructure;
using YummyZoom.SharedKernel;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Orders.Queries.Common;
using YummyZoom.Application.Orders.Queries.GetCustomerRecentOrders;
using System.Text.Json;

namespace YummyZoom.Web.ApiContractTests.Orders;

// Contract tests for GET /api/v1/orders/my?pageNumber=&pageSize=
public class GetCustomerRecentOrdersContractTests
{
    private static OrderSummaryDto CreateSummary(Guid orderId)
        => new(orderId, "ORD-1", "Placed", DateTime.UtcNow.AddMinutes(-10), Guid.NewGuid(), Guid.NewGuid(), 15m, "USD", 2);

    [Test]
    public async Task GetCustomerRecentOrders_WhenNonEmpty_Returns200WithItems()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<GetCustomerRecentOrdersQuery>();
            var q = (GetCustomerRecentOrdersQuery)req;
            q.PageNumber.Should().Be(1);
            q.PageSize.Should().Be(20);
            var list = new PaginatedList<OrderSummaryDto>(new[] { CreateSummary(Guid.NewGuid()) }, count: 1, pageNumber: 1, pageSize: 20);
            return YummyZoom.SharedKernel.Result.Success(list);
        });
        var path = "/api/v1/orders/my?pageNumber=1&pageSize=20";
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(raw);
        doc.RootElement.GetProperty("items").GetArrayLength().Should().Be(1);
        doc.RootElement.GetProperty("pageNumber").GetInt32().Should().Be(1);
    }

    [Test]
    public async Task GetCustomerRecentOrders_WhenEmpty_Returns200WithEmptyItems()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<GetCustomerRecentOrdersQuery>();
            var list = new PaginatedList<OrderSummaryDto>(Array.Empty<OrderSummaryDto>(), count: 0, pageNumber: 2, pageSize: 10);
            return YummyZoom.SharedKernel.Result.Success(list);
        });
        var path = "/api/v1/orders/my?pageNumber=2&pageSize=10";
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(raw);
        doc.RootElement.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    [Test]
    public async Task GetCustomerRecentOrders_WithoutAuth_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        var path = "/api/v1/orders/my?pageNumber=1&pageSize=10";
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
