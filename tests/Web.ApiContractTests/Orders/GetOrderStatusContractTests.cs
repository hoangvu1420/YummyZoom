using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using YummyZoom.Application.Orders.Queries.Common;
using YummyZoom.Application.Orders.Queries.GetOrderStatus;
using YummyZoom.SharedKernel;
using YummyZoom.Web.ApiContractTests.Infrastructure;

namespace YummyZoom.Web.ApiContractTests.Orders;

// Contract tests for GET /api/v1/orders/{orderId}/status
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
            return Result.Success(new OrderStatusDto(orderId, "Preparing", DateTime.UtcNow, DateTime.UtcNow.AddMinutes(20), 1));
        });
        var path = $"/api/v1/orders/{orderId}/status";
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
        var path = $"/api/v1/orders/{Guid.NewGuid()}/status";
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var prob = JsonSerializer.Deserialize<ProblemDetails>(raw);
        prob!.Status.Should().Be(404);
        prob.Title.Should().Be("Order.NotFound");
    }

    [Test]
    public async Task GetOrderStatus_WithoutAuth_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        var path = $"/api/v1/orders/{Guid.NewGuid()}/status";
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetOrderStatus_Returns200_With_Etag_And_CacheHeaders()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        var orderId = Guid.NewGuid();
        const long version = 7;
        var lastUpdate = DateTime.UtcNow.AddMinutes(-1);
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<GetOrderStatusQuery>();
            ((GetOrderStatusQuery)req).OrderIdGuid.Should().Be(orderId);
            return Result.Success(new OrderStatusDto(orderId, "Preparing", lastUpdate, lastUpdate.AddMinutes(20), version));
        });

        var path = $"/api/v1/orders/{orderId}/status";
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Headers.ETag.Should().NotBeNull();
        var expectedEtag = $"\"order-{orderId}-v{version}\""; // quoted strong etag
        resp.Headers.ETag!.Tag.Should().Be(expectedEtag);
        resp.Content.Headers.TryGetValues("Last-Modified", out var _).Should().BeTrue();
        resp.Headers.CacheControl!.NoCache.Should().BeTrue();
        resp.Headers.CacheControl.MustRevalidate.Should().BeTrue();
    }

    [Test]
    public async Task GetOrderStatus_IfNoneMatch_Match_Returns304()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        var orderId = Guid.NewGuid();
        const long version = 9;
        var lastUpdate = DateTime.UtcNow.AddMinutes(-2);
        factory.Sender.RespondWith(_ => Result.Success(new OrderStatusDto(orderId, "Accepted", lastUpdate, lastUpdate.AddMinutes(30), version)));

        var expectedEtag = $"\"order-{orderId}-v{version}\"";
        client.DefaultRequestHeaders.TryAddWithoutValidation("If-None-Match", expectedEtag);

        var path = $"/api/v1/orders/{orderId}/status";
        var resp = await client.GetAsync(path);
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        resp.StatusCode.Should().Be(HttpStatusCode.NotModified);
        var body = await resp.Content.ReadAsStringAsync();
        body.Should().BeNullOrEmpty();
    }
}
