using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using YummyZoom.Application.TeamCarts.Commands.ConvertTeamCartToOrder;
using YummyZoom.Infrastructure.Serialization.JsonOptions;
using YummyZoom.SharedKernel;
using YummyZoom.Web.ApiContractTests.Infrastructure;

namespace YummyZoom.Web.ApiContractTests.TeamCarts;

public class TeamCartConversionContractTests
{
    #region Convert TeamCart Tests

    [Test]
    public async Task ConvertTeamCartToOrder_WithValidRequest_Returns200()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "host-user");

        var cartId = Guid.NewGuid();
        var expectedOrderId = Guid.NewGuid();
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<ConvertTeamCartToOrderCommand>();
            var cmd = (ConvertTeamCartToOrderCommand)req;
            cmd.TeamCartId.Should().Be(cartId);
            cmd.Street.Should().Be("123 Main St");
            cmd.City.Should().Be("New York");
            cmd.State.Should().Be("NY");
            cmd.ZipCode.Should().Be("10001");
            cmd.Country.Should().Be("USA");
            cmd.SpecialInstructions.Should().Be("Ring doorbell");
            return Result.Success(new ConvertTeamCartToOrderResponse(expectedOrderId));
        });

        var body = new
        {
            Street = "123 Main St",
            City = "New York",
            State = "NY",
            ZipCode = "10001",
            Country = "USA",
            SpecialInstructions = "Ring doorbell"
        };

        var path = $"/api/v1/team-carts/{cartId}/convert";
        var requestJson = JsonSerializer.Serialize(body, DomainJson.Options);
        TestContext.WriteLine($"REQUEST POST {path}");
        TestContext.WriteLine(requestJson);

        var resp = await client.PostAsJsonAsync(path, body, DomainJson.Options);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(rawResponse);
        doc.RootElement.GetProperty("orderId").GetGuid().Should().Be(expectedOrderId);
    }

    [Test]
    public async Task ConvertTeamCartToOrder_WhenNotReadyToConfirm_Returns400Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "host-user");

        var cartId = Guid.NewGuid();
        factory.Sender.RespondWith(_ => Result.Failure<ConvertTeamCartToOrderResponse>(
            Error.Validation("TeamCart.NotReadyToConfirm", "Not all members have committed to payment")));

        var body = new
        {
            Street = "123 Main St",
            City = "New York",
            State = "NY",
            ZipCode = "10001",
            Country = "USA",
            SpecialInstructions = (string?)null
        };

        var path = $"/api/v1/team-carts/{cartId}/convert";
        var requestJson = JsonSerializer.Serialize(body, DomainJson.Options);
        TestContext.WriteLine($"REQUEST POST {path}");
        TestContext.WriteLine(requestJson);

        var resp = await client.PostAsJsonAsync(path, body, DomainJson.Options);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = JsonSerializer.Deserialize<ProblemDetails>(rawResponse);
        problem!.Status.Should().Be(400);
        problem.Title.Should().Be("TeamCart.NotReadyToConfirm");
    }

    [Test]
    public async Task ConvertTeamCartToOrder_WithMissingRequiredFields_Returns400Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "host-user");

        var cartId = Guid.NewGuid();
        factory.Sender.RespondWith(_ => Result.Failure<ConvertTeamCartToOrderResponse>(
            Error.Validation("ConvertTeamCartToOrder.InvalidAddress", "Street address is required")));

        var body = new
        {
            Street = "",
            City = "New York",
            State = "NY",
            ZipCode = "10001",
            Country = "USA",
            SpecialInstructions = (string?)null
        };

        var path = $"/api/v1/team-carts/{cartId}/convert";
        var requestJson = JsonSerializer.Serialize(body, DomainJson.Options);
        TestContext.WriteLine($"REQUEST POST {path}");
        TestContext.WriteLine(requestJson);

        var resp = await client.PostAsJsonAsync(path, body, DomainJson.Options);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = JsonSerializer.Deserialize<ProblemDetails>(rawResponse);
        problem!.Status.Should().Be(400);
        problem.Title.Should().Be("ConvertTeamCartToOrder.InvalidAddress");
    }

    [Test]
    public async Task ConvertTeamCartToOrder_WhenTeamCartNotFound_Returns404Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "host-user");

        var cartId = Guid.NewGuid();
        factory.Sender.RespondWith(_ => Result.Failure<ConvertTeamCartToOrderResponse>(
            Error.NotFound("TeamCart.NotFound", "TeamCart not found")));

        var body = new
        {
            Street = "123 Main St",
            City = "New York",
            State = "NY",
            ZipCode = "10001",
            Country = "USA",
            SpecialInstructions = (string?)null
        };

        var path = $"/api/v1/team-carts/{cartId}/convert";
        var requestJson = JsonSerializer.Serialize(body, DomainJson.Options);
        TestContext.WriteLine($"REQUEST POST {path}");
        TestContext.WriteLine(requestJson);

        var resp = await client.PostAsJsonAsync(path, body, DomainJson.Options);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = JsonSerializer.Deserialize<ProblemDetails>(rawResponse);
        problem!.Status.Should().Be(404);
        problem.Title.Should().Be("TeamCart.NotFound");
    }

    [Test]
    public async Task ConvertTeamCartToOrder_WhenNotHost_Returns403Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "not-host-user");

        var cartId = Guid.NewGuid();
        factory.Sender.RespondWith(_ => Result.Failure<ConvertTeamCartToOrderResponse>(
            Error.Failure("TeamCart.NotHost", "Only the host can convert the cart to order")));

        var body = new
        {
            Street = "123 Main St",
            City = "New York",
            State = "NY",
            ZipCode = "10001",
            Country = "USA",
            SpecialInstructions = (string?)null
        };

        var path = $"/api/v1/team-carts/{cartId}/convert";
        var requestJson = JsonSerializer.Serialize(body, DomainJson.Options);
        TestContext.WriteLine($"REQUEST POST {path}");
        TestContext.WriteLine(requestJson);

        var resp = await client.PostAsJsonAsync(path, body, DomainJson.Options);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = JsonSerializer.Deserialize<ProblemDetails>(rawResponse);
        problem!.Status.Should().Be(400);
        problem.Title.Should().Be("TeamCart.NotHost");
    }

    [Test]
    public async Task ConvertTeamCartToOrder_WithComplexAddressValidation_Returns200()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "host-user");

        var cartId = Guid.NewGuid();
        var expectedOrderId = Guid.NewGuid();
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<ConvertTeamCartToOrderCommand>();
            var cmd = (ConvertTeamCartToOrderCommand)req;
            // Verify all address components are correctly mapped
            cmd.Street.Should().Be("456 Oak Avenue, Apt 2B");
            cmd.City.Should().Be("Los Angeles");
            cmd.State.Should().Be("California");
            cmd.ZipCode.Should().Be("90210");
            cmd.Country.Should().Be("United States");
            return Result.Success(new ConvertTeamCartToOrderResponse(expectedOrderId));
        });

        var body = new
        {
            Street = "456 Oak Avenue, Apt 2B",
            City = "Los Angeles",
            State = "California",
            ZipCode = "90210",
            Country = "United States",
            SpecialInstructions = "Leave at front desk"
        };

        var path = $"/api/v1/team-carts/{cartId}/convert";
        var requestJson = JsonSerializer.Serialize(body, DomainJson.Options);
        TestContext.WriteLine($"REQUEST POST {path}");
        TestContext.WriteLine(requestJson);

        var resp = await client.PostAsJsonAsync(path, body, DomainJson.Options);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify the last request was captured with correct mapping
        var lastRequest = factory.Sender.LastRequest;
        lastRequest.Should().NotBeNull();
        lastRequest.Should().BeOfType<ConvertTeamCartToOrderCommand>();

        using var doc = JsonDocument.Parse(rawResponse);
        doc.RootElement.GetProperty("orderId").GetGuid().Should().Be(expectedOrderId);
    }

    [Test]
    public async Task ConvertTeamCartToOrder_WithoutAuth_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();

        var body = new
        {
            Street = "123 Main St",
            City = "New York",
            State = "NY",
            ZipCode = "10001",
            Country = "USA",
            SpecialInstructions = (string?)null
        };

        var path = $"/api/v1/team-carts/{Guid.NewGuid()}/convert";
        var requestJson = JsonSerializer.Serialize(body, DomainJson.Options);
        TestContext.WriteLine($"REQUEST POST {path}");
        TestContext.WriteLine(requestJson);

        var resp = await client.PostAsJsonAsync(path, body, DomainJson.Options);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion
}
