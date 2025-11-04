using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using YummyZoom.Application.Orders.Queries.Common;
using YummyZoom.Application.Orders.Queries.GetOrderById;
using YummyZoom.SharedKernel;
using YummyZoom.Web.ApiContractTests.Infrastructure;

namespace YummyZoom.Web.ApiContractTests.Orders;

// Contract tests for GET /api/v1/orders/{orderId}
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
            "USD",
            20m,
            0m,
            5m,
            2m,
            3m,
            30m,
            null,
            null,
            "Street", "City", "State", "Country", "12345",
            Array.Empty<OrderItemDto>(),
            // Restaurant snapshot & geo (nullable for test)
            null, null, null, null, null, null,
            null, null,
            null, null,
            null,
            // Payment method & cancellable flag (nullable/defaults)
            null,
            false
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

        var path = $"/api/v1/orders/{orderId}";
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(raw);
        var order = doc.RootElement.GetProperty("order");
        order.GetProperty("orderId").ValueKind.Should().Be(JsonValueKind.String);
        order.GetProperty("currency").ValueKind.Should().Be(JsonValueKind.String);
        order.GetProperty("totalAmount").ValueKind.Should().Be(JsonValueKind.Number);
        order.TryGetProperty("totalCurrency", out _).Should().BeFalse(); // Ensure old field is gone
    }

    [Test]
    public async Task GetOrderById_WhenNotFound_Returns404Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");
        factory.Sender.RespondWith(_ => Result.Failure<GetOrderByIdResponse>(Error.NotFound("Order.NotFound", "Missing")));
        var path = $"/api/v1/orders/{Guid.NewGuid()}";
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
    public async Task GetOrderById_WithoutAuth_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        var path = $"/api/v1/orders/{Guid.NewGuid()}";
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetOrderById_Returns200_With_Etag_And_CacheHeaders()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        var orderId = Guid.NewGuid();
        var lastUpdate = new DateTime(2024, 10, 1, 12, 0, 0, DateTimeKind.Utc);

        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<GetOrderByIdQuery>();
            ((GetOrderByIdQuery)req).OrderIdGuid.Should().Be(orderId);

            var details = new OrderDetailsDto(
                OrderId: orderId,
                OrderNumber: "ORD-ETAG",
                CustomerId: Guid.NewGuid(),
                RestaurantId: Guid.NewGuid(),
                Status: "Preparing",
                PlacementTimestamp: lastUpdate.AddMinutes(-30),
                LastUpdateTimestamp: lastUpdate,
                EstimatedDeliveryTime: lastUpdate.AddMinutes(40),
                ActualDeliveryTime: null,
                Currency: "USD",
                SubtotalAmount: 20m,
                DiscountAmount: 0m,
                DeliveryFeeAmount: 5m,
                TipAmount: 2m,
                TaxAmount: 3m,
                TotalAmount: 30m,
                AppliedCouponId: null,
                SourceTeamCartId: null,
                DeliveryAddress_Street: "Street",
                DeliveryAddress_City: "City",
                DeliveryAddress_State: "State",
                DeliveryAddress_Country: "Country",
                DeliveryAddress_PostalCode: "12345",
                Items: Array.Empty<OrderItemDto>(),
                RestaurantName: null,
                RestaurantAddress_Street: null,
                RestaurantAddress_City: null,
                RestaurantAddress_State: null,
                RestaurantAddress_Country: null,
                RestaurantAddress_PostalCode: null,
                RestaurantLat: null,
                RestaurantLon: null,
                DeliveryLat: null,
                DeliveryLon: null,
                DistanceKm: null,
                PaymentMethod: null,
                Cancellable: false);

            return Result.Success(new GetOrderByIdResponse(details));
        });

        var path = $"/api/v1/orders/{orderId}";
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Headers.ETag.Should().NotBeNull();
        var expectedEtag = $"\"order-{orderId}-t{lastUpdate.Ticks}\""; // quoted strong etag
        resp.Headers.ETag!.Tag.Should().Be(expectedEtag);
        resp.Content.Headers.TryGetValues("Last-Modified", out var _).Should().BeTrue();
        resp.Headers.CacheControl!.NoCache.Should().BeTrue();
        resp.Headers.CacheControl.MustRevalidate.Should().BeTrue();
    }

    [Test]
    public async Task GetOrderById_IfNoneMatch_Match_Returns304()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        var orderId = Guid.NewGuid();
        var lastUpdate = new DateTime(2024, 10, 2, 8, 0, 0, DateTimeKind.Utc);
        var expectedEtag = $"\"order-{orderId}-t{lastUpdate.Ticks}\"";

        factory.Sender.RespondWith(_ =>
        {
            var details = new OrderDetailsDto(
                OrderId: orderId,
                OrderNumber: "ORD-ETAG2",
                CustomerId: Guid.NewGuid(),
                RestaurantId: Guid.NewGuid(),
                Status: "Accepted",
                PlacementTimestamp: lastUpdate.AddMinutes(-60),
                LastUpdateTimestamp: lastUpdate,
                EstimatedDeliveryTime: lastUpdate.AddMinutes(30),
                ActualDeliveryTime: null,
                Currency: "USD",
                SubtotalAmount: 10m,
                DiscountAmount: 0m,
                DeliveryFeeAmount: 0m,
                TipAmount: 0m,
                TaxAmount: 0m,
                TotalAmount: 10m,
                AppliedCouponId: null,
                SourceTeamCartId: null,
                DeliveryAddress_Street: "Street",
                DeliveryAddress_City: "City",
                DeliveryAddress_State: "State",
                DeliveryAddress_Country: "Country",
                DeliveryAddress_PostalCode: "12345",
                Items: Array.Empty<OrderItemDto>(),
                RestaurantName: null,
                RestaurantAddress_Street: null,
                RestaurantAddress_City: null,
                RestaurantAddress_State: null,
                RestaurantAddress_Country: null,
                RestaurantAddress_PostalCode: null,
                RestaurantLat: null,
                RestaurantLon: null,
                DeliveryLat: null,
                DeliveryLon: null,
                DistanceKm: null,
                PaymentMethod: null,
                Cancellable: false);
            return Result.Success(new GetOrderByIdResponse(details));
        });

        client.DefaultRequestHeaders.TryAddWithoutValidation("If-None-Match", expectedEtag);
        var path = $"/api/v1/orders/{orderId}";
        var resp = await client.GetAsync(path);
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        resp.StatusCode.Should().Be(HttpStatusCode.NotModified);
        var body = await resp.Content.ReadAsStringAsync();
        body.Should().BeNullOrEmpty();
    }

    [Test]
    public async Task GetOrderById_Payload_Includes_New_Fields()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        var orderId = Guid.NewGuid();
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<GetOrderByIdQuery>();
            ((GetOrderByIdQuery)req).OrderIdGuid.Should().Be(orderId);

            var item = new OrderItemDto(
                OrderItemId: Guid.NewGuid(),
                MenuItemId: Guid.NewGuid(),
                Name: "Classic Burger",
                Quantity: 1,
                UnitPriceAmount: 10m,
                LineItemTotalAmount: 10m,
                Customizations: Array.Empty<OrderItemCustomizationDto>(),
                ImageUrl: "https://example.com/img.jpg");

            var details = new OrderDetailsDto(
                OrderId: orderId,
                OrderNumber: "ORD-DETAIL",
                CustomerId: Guid.NewGuid(),
                RestaurantId: Guid.NewGuid(),
                Status: "Placed",
                PlacementTimestamp: DateTime.UtcNow.AddMinutes(-10),
                LastUpdateTimestamp: DateTime.UtcNow,
                EstimatedDeliveryTime: DateTime.UtcNow.AddMinutes(30),
                ActualDeliveryTime: null,
                Currency: "USD",
                SubtotalAmount: 10m,
                DiscountAmount: 0m,
                DeliveryFeeAmount: 2m,
                TipAmount: 1m,
                TaxAmount: 1m,
                TotalAmount: 14m,
                AppliedCouponId: null,
                SourceTeamCartId: null,
                DeliveryAddress_Street: "1 Main St",
                DeliveryAddress_City: "Town",
                DeliveryAddress_State: "ST",
                DeliveryAddress_Country: "US",
                DeliveryAddress_PostalCode: "99999",
                Items: new[] { item },
                RestaurantName: "Test Resto",
                RestaurantAddress_Street: "2 Side St",
                RestaurantAddress_City: "Town",
                RestaurantAddress_State: "ST",
                RestaurantAddress_Country: "US",
                RestaurantAddress_PostalCode: "11111",
                RestaurantLat: 12.34,
                RestaurantLon: 56.78,
                DeliveryLat: null,
                DeliveryLon: null,
                DistanceKm: null,
                PaymentMethod: "CreditCard",
                Cancellable: true);

            return Result.Success(new GetOrderByIdResponse(details));
        });

        var path = $"/api/v1/orders/{orderId}";
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(raw);
        var order = doc.RootElement.GetProperty("order");
        order.GetProperty("restaurantName").GetString().Should().Be("Test Resto");
        order.GetProperty("restaurantAddress_Street").GetString().Should().Be("2 Side St");
        order.GetProperty("restaurantLat").GetDouble().Should().Be(12.34);
        order.GetProperty("restaurantLon").GetDouble().Should().Be(56.78);
        order.GetProperty("paymentMethod").GetString().Should().Be("CreditCard");
        order.GetProperty("cancellable").GetBoolean().Should().BeTrue();

        var items = order.GetProperty("items");
        items.GetArrayLength().Should().Be(1);
        var first = items[0];
        first.GetProperty("imageUrl").GetString().Should().Be("https://example.com/img.jpg");
    }
}
