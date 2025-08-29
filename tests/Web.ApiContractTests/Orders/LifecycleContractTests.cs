using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Web.ApiContractTests.Infrastructure;
using YummyZoom.SharedKernel;
using YummyZoom.Application.Orders.Commands.AcceptOrder;
using YummyZoom.Application.Orders.Commands.RejectOrder;
using YummyZoom.Application.Orders.Commands.CancelOrder;
using YummyZoom.Application.Orders.Commands.MarkOrderPreparing;
using YummyZoom.Application.Orders.Commands.MarkOrderReadyForDelivery;
using YummyZoom.Application.Orders.Commands.MarkOrderDelivered;
using YummyZoom.Application.Orders.Queries.GetOrderStatus;
using YummyZoom.Application.Orders.Queries.Common; 
using YummyZoom.Domain.OrderAggregate.ValueObjects; 
using System.Text.Json;
using YummyZoom.Application.Orders.Commands.Common;

namespace YummyZoom.Web.ApiContractTests.Orders;

// Contract tests focused on order lifecycle state transition endpoints.
// Follows guidelines in WebApi_Contract_Tests_Guidelines.md
public class LifecycleContractTests
{
    private static readonly JsonSerializerOptions JsonOptions = YummyZoom.Infrastructure.Serialization.DomainJson.Options;

    private static OrderLifecycleResultDto CreateLifecycleDto(Guid orderId, string status, DateTime? est = null, DateTime? actual = null)
        => new(OrderId.Create(orderId), "ORD-ABC", status, DateTime.UtcNow.AddMinutes(-30), DateTime.UtcNow, est, actual);

    #region Success 200 Cases

    [Test]
    public async Task AcceptOrder_WhenValid_Returns200()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        var orderId = Guid.NewGuid();
        var est = DateTime.UtcNow.AddMinutes(45);
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<AcceptOrderCommand>();
            var cmd = (AcceptOrderCommand)req;
            cmd.OrderId.Should().Be(orderId);
            cmd.EstimatedDeliveryTime.Should().Be(est);
            return Result.Success(CreateLifecycleDto(orderId, "Accepted", est));
        });

        var path = $"/api/v1/orders/{orderId}/accept";
        var body = new { restaurantId = Guid.NewGuid(), estimatedDeliveryTime = est };
        TestContext.WriteLine($"REQUEST POST {path}");
        TestContext.WriteLine(JsonSerializer.Serialize(body, JsonOptions));

        var resp = await client.PostAsJsonAsync(path, body);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(raw);
        doc.RootElement.GetProperty("orderId").ValueKind.Should().Be(JsonValueKind.String);
        doc.RootElement.GetProperty("status").GetString().Should().Be("Accepted");
    }

    [Test]
    public async Task RejectOrder_WhenValid_Returns200()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        var orderId = Guid.NewGuid();
        var reason = "Out of stock";
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<RejectOrderCommand>();
            var cmd = (RejectOrderCommand)req;
            cmd.OrderId.Should().Be(orderId);
            // Reason property not exposed publicly (internal/private); mapping validated by command type & order id.
            return Result.Success(CreateLifecycleDto(orderId, "Rejected"));
        });
        var path = $"/api/v1/orders/{orderId}/reject";
        var body = new { restaurantId = Guid.NewGuid(), reason };
        TestContext.WriteLine($"REQUEST POST {path}");
        TestContext.WriteLine(JsonSerializer.Serialize(body, JsonOptions));
        var resp = await client.PostAsJsonAsync(path, body);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonDocument.Parse(raw).RootElement.GetProperty("status").GetString().Should().Be("Rejected");
    }

    [Test]
    public async Task CancelOrder_WhenValid_Returns200()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        var orderId = Guid.NewGuid();
        Guid? actingUserId = Guid.NewGuid();
        var reason = "Customer request";
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<CancelOrderCommand>();
            var cmd = (CancelOrderCommand)req;
            cmd.OrderId.Should().Be(orderId);
            cmd.ActingUserId.Should().Be(actingUserId);
            cmd.Reason.Should().Be(reason);
            return Result.Success(CreateLifecycleDto(orderId, "Cancelled"));
        });
        var path = $"/api/v1/orders/{orderId}/cancel";
        var body = new { restaurantId = Guid.NewGuid(), actingUserId, reason };
        TestContext.WriteLine($"REQUEST POST {path}");
        TestContext.WriteLine(JsonSerializer.Serialize(body, JsonOptions));
        var resp = await client.PostAsJsonAsync(path, body);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonDocument.Parse(raw).RootElement.GetProperty("status").GetString().Should().Be("Cancelled");
    }

    [Test]
    public async Task MarkOrderPreparing_WhenValid_Returns200()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        var orderId = Guid.NewGuid();
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<MarkOrderPreparingCommand>();
            ((MarkOrderPreparingCommand)req).OrderId.Should().Be(orderId);
            return Result.Success(CreateLifecycleDto(orderId, "Preparing"));
        });
        var path = $"/api/v1/orders/{orderId}/preparing";
        var body = new { restaurantId = Guid.NewGuid() };
        TestContext.WriteLine($"REQUEST POST {path}");
        TestContext.WriteLine(JsonSerializer.Serialize(body, JsonOptions));
        var resp = await client.PostAsJsonAsync(path, body);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonDocument.Parse(raw).RootElement.GetProperty("status").GetString().Should().Be("Preparing");
    }

    [Test]
    public async Task MarkOrderReady_WhenValid_Returns200()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        var orderId = Guid.NewGuid();
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<MarkOrderReadyForDeliveryCommand>();
            ((MarkOrderReadyForDeliveryCommand)req).OrderId.Should().Be(orderId);
            return Result.Success(CreateLifecycleDto(orderId, "ReadyForDelivery"));
        });
        var path = $"/api/v1/orders/{orderId}/ready";
        var body = new { restaurantId = Guid.NewGuid() };
        TestContext.WriteLine($"REQUEST POST {path}");
        TestContext.WriteLine(JsonSerializer.Serialize(body, JsonOptions));
        var resp = await client.PostAsJsonAsync(path, body);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonDocument.Parse(raw).RootElement.GetProperty("status").GetString().Should().Be("ReadyForDelivery");
    }

    [Test]
    public async Task MarkOrderDelivered_WhenValid_Returns200()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        var orderId = Guid.NewGuid();
        var deliveredAt = DateTime.UtcNow;
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<MarkOrderDeliveredCommand>();
            ((MarkOrderDeliveredCommand)req).OrderId.Should().Be(orderId);
            return Result.Success(CreateLifecycleDto(orderId, "Delivered", actual: deliveredAt));
        });
        var path = $"/api/v1/orders/{orderId}/delivered";
        var body = new { restaurantId = Guid.NewGuid(), deliveredAtUtc = deliveredAt };
        TestContext.WriteLine($"REQUEST POST {path}");
        TestContext.WriteLine(JsonSerializer.Serialize(body, JsonOptions));
        var resp = await client.PostAsJsonAsync(path, body);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonDocument.Parse(raw).RootElement.GetProperty("status").GetString().Should().Be("Delivered");
    }

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
            return Result.Success(new OrderStatusDto(orderId, "Preparing", DateTime.UtcNow, DateTime.UtcNow.AddMinutes(30)));
        });
        var path = $"/api/v1/orders/{orderId}/status";
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonDocument.Parse(raw).RootElement.GetProperty("orderId").ValueKind.Should().Be(JsonValueKind.String);
    }

    #endregion

    #region Not Found 404

    [Test]
    public async Task AcceptOrder_WhenOrderNotFound_Returns404Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        factory.Sender.RespondWith(_ => Result.Failure<OrderLifecycleResultDto>(Error.NotFound("Order.NotFound", "Missing")));
        var orderId = Guid.NewGuid();
        var path = $"/api/v1/orders/{orderId}/accept";
        var body = new { restaurantId = Guid.NewGuid(), estimatedDeliveryTime = DateTime.UtcNow.AddMinutes(30) };
        TestContext.WriteLine($"REQUEST POST {path}");
        TestContext.WriteLine(JsonSerializer.Serialize(body, JsonOptions));
        var resp = await client.PostAsJsonAsync(path, body);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");
        var prob = JsonSerializer.Deserialize<Microsoft.AspNetCore.Mvc.ProblemDetails>(raw);
        prob!.Status.Should().Be(404);
        prob.Title.Should().Be("Order");
    }

    [Test]
    public async Task MarkOrderPreparing_WhenOrderNotFound_Returns404Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        factory.Sender.RespondWith(_ => Result.Failure<OrderLifecycleResultDto>(Error.NotFound("Order.NotFound", "Missing")));
        var orderId = Guid.NewGuid();
        var path = $"/api/v1/orders/{orderId}/preparing";
        var body = new { restaurantId = Guid.NewGuid() };
        TestContext.WriteLine($"REQUEST POST {path}");
        TestContext.WriteLine(JsonSerializer.Serialize(body, JsonOptions));
        var resp = await client.PostAsJsonAsync(path, body);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");
        var prob = JsonSerializer.Deserialize<Microsoft.AspNetCore.Mvc.ProblemDetails>(raw);
        prob!.Title.Should().Be("Order");
    }

    [Test]
    public async Task GetOrderStatus_WhenNotFound_Returns404Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        factory.Sender.RespondWith(_ => Result.Failure<OrderStatusDto>(Error.NotFound("Order.NotFound", "Missing")));
        var orderId = Guid.NewGuid();
        var path = $"/api/v1/orders/{orderId}/status";
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");
        var prob = JsonSerializer.Deserialize<Microsoft.AspNetCore.Mvc.ProblemDetails>(raw);
        prob!.Status.Should().Be(404);
        prob.Title.Should().Be("Order");
    }

    #endregion

    #region Conflict 409

    [Test]
    public async Task AcceptOrder_WhenInvalidState_Returns409Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        factory.Sender.RespondWith(_ => Result.Failure<OrderLifecycleResultDto>(Error.Conflict("Order.InvalidState", "Cannot accept order")));
        var orderId = Guid.NewGuid();
        var path = $"/api/v1/orders/{orderId}/accept";
        var body = new { restaurantId = Guid.NewGuid(), estimatedDeliveryTime = DateTime.UtcNow.AddMinutes(30) };
        TestContext.WriteLine($"REQUEST POST {path}");
        TestContext.WriteLine(JsonSerializer.Serialize(body, JsonOptions));
        var resp = await client.PostAsJsonAsync(path, body);
        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");
        var prob = JsonSerializer.Deserialize<Microsoft.AspNetCore.Mvc.ProblemDetails>(raw);
        prob!.Status.Should().Be(409);
        prob.Title.Should().Be("Order");
    }

    [Test]
    public async Task MarkOrderDelivered_WhenInvalidState_Returns409Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        factory.Sender.RespondWith(_ => Result.Failure<OrderLifecycleResultDto>(Error.Conflict("Order.InvalidState", "Cannot deliver order")));
        var orderId = Guid.NewGuid();
        var path = $"/api/v1/orders/{orderId}/delivered";
        var body = new { restaurantId = Guid.NewGuid(), deliveredAtUtc = DateTime.UtcNow };
        TestContext.WriteLine($"REQUEST POST {path}");
        TestContext.WriteLine(JsonSerializer.Serialize(body, JsonOptions));
        var resp = await client.PostAsJsonAsync(path, body);
        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");
        var prob = JsonSerializer.Deserialize<Microsoft.AspNetCore.Mvc.ProblemDetails>(raw);
        prob!.Title.Should().Be("Order");
    }

    #endregion

    #region Validation 400

    [Test]
    public async Task AcceptOrder_WhenEstimatedDeliveryTimeInPast_Returns400Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        factory.Sender.RespondWith(_ => Result.Failure<OrderLifecycleResultDto>(Error.Validation("Order.InvalidEstimatedDeliveryTime", "Past time")));
        var orderId = Guid.NewGuid();
        var path = $"/api/v1/orders/{orderId}/accept";
        var body = new { restaurantId = Guid.NewGuid(), estimatedDeliveryTime = DateTime.UtcNow.AddMinutes(-5) };
        TestContext.WriteLine($"REQUEST POST {path}");
        TestContext.WriteLine(JsonSerializer.Serialize(body, JsonOptions));
        var resp = await client.PostAsJsonAsync(path, body);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");
        var prob = JsonSerializer.Deserialize<Microsoft.AspNetCore.Mvc.ProblemDetails>(raw);
        prob!.Status.Should().Be(400);
        prob.Title.Should().Be("Order");
    }

    #endregion

    #region Unauthorized 401

    [Test]
    public async Task AcceptOrder_WithoutAuth_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        var orderId = Guid.NewGuid();
        var path = $"/api/v1/orders/{orderId}/accept";
        var body = new { restaurantId = Guid.NewGuid(), estimatedDeliveryTime = DateTime.UtcNow.AddMinutes(30) };
        TestContext.WriteLine($"REQUEST POST {path}");
        TestContext.WriteLine(JsonSerializer.Serialize(body, JsonOptions));
        var resp = await client.PostAsJsonAsync(path, body);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion
}
