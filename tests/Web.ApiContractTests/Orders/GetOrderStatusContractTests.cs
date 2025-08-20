using System.Net;
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Web.ApiContractTests.Infrastructure;
using YummyZoom.SharedKernel;
using Microsoft.AspNetCore.Mvc;
using YummyZoom.Application.Orders.Queries.Common;
using YummyZoom.Application.Orders.Queries.GetOrderStatus;
using System.Text.Json;

namespace YummyZoom.Web.ApiContractTests.Orders;

// Contract tests for GET /api/v1.0/orders/{orderId}/status
public class GetOrderStatusContractTests
{
    [Test]
    public async Task GetOrderStatus_WhenFound_Returns200()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        var orderId = Guid.NewGuid();
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<GetOrderStatusQuery>();
            ((GetOrderStatusQuery)req).OrderIdGuid.Should().Be(orderId);
            return Result.Success(new OrderStatusDto(orderId, "Preparing", DateTime.UtcNow, DateTime.UtcNow.AddMinutes(20)));
        });
        var path = $"/api/v1.0/orders/{orderId}/status";
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(raw);
        doc.RootElement.GetProperty("orderId").ValueKind.Should().Be(JsonValueKind.String);
        doc.RootElement.GetProperty("status").GetString().Should().Be("Preparing");
    }

    [Test]
    public async Task GetOrderStatus_WhenNotFound_Returns404Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        factory.Sender.RespondWith(_ => Result.Failure<OrderStatusDto>(Error.NotFound("Order.NotFound", "Missing")));
        var path = $"/api/v1.0/orders/{Guid.NewGuid()}/status";
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var prob = JsonSerializer.Deserialize<ProblemDetails>(raw);
        prob!.Status.Should().Be(404);
        prob.Title.Should().Be("Order");
    }

    [Test]
    public async Task GetOrderStatus_WithoutAuth_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        var path = $"/api/v1.0/orders/{Guid.NewGuid()}/status";
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
