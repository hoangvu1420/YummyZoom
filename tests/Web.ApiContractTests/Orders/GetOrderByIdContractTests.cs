using System.Net;
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Web.ApiContractTests.Infrastructure;
using YummyZoom.SharedKernel;
using Microsoft.AspNetCore.Mvc;
using YummyZoom.Application.Orders.Queries.GetOrderById;
using YummyZoom.Application.Orders.Queries.Common;
using System.Text.Json;

namespace YummyZoom.Web.ApiContractTests.Orders;

// Contract tests for GET /api/v1.0/orders/{orderId}
public class GetOrderByIdContractTests
{
    private static OrderDetailsDto CreateDetails(Guid orderId)
        => new(
            orderId,
            "ORD-123",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Preparing",
            DateTime.UtcNow.AddMinutes(-30),
            DateTime.UtcNow,
            DateTime.UtcNow.AddMinutes(40),
            null,
            20m, "USD",
            0m, "USD",
            5m, "USD",
            2m, "USD",
            3m, "USD",
            30m, "USD",
            null,
            null,
            "Street", "City", "State", "Country", "12345",
            Array.Empty<OrderItemDto>()
        );

    [Test]
    public async Task GetOrderById_WhenFound_Returns200()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        var orderId = Guid.NewGuid();
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<GetOrderByIdQuery>();
            ((GetOrderByIdQuery)req).OrderIdGuid.Should().Be(orderId);
            return Result.Success(new GetOrderByIdResponse(CreateDetails(orderId)));
        });

        var path = $"/api/v1.0/orders/{orderId}";
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(raw);
        doc.RootElement.GetProperty("order").GetProperty("orderId").ValueKind.Should().Be(JsonValueKind.String);
    }

    [Test]
    public async Task GetOrderById_WhenNotFound_Returns404Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        factory.Sender.RespondWith(_ => Result.Failure<GetOrderByIdResponse>(Error.NotFound("Order.NotFound", "Missing")));
        var path = $"/api/v1.0/orders/{Guid.NewGuid()}";
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
    public async Task GetOrderById_WithoutAuth_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        var path = $"/api/v1.0/orders/{Guid.NewGuid()}";
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
