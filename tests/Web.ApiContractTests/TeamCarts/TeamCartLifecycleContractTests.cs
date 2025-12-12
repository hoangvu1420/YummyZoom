using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using YummyZoom.Application.TeamCarts.Commands.CreateTeamCart;
using YummyZoom.Application.TeamCarts.Commands.JoinTeamCart;
using YummyZoom.Application.TeamCarts.Models;
using YummyZoom.Application.TeamCarts.Queries.Common;
using YummyZoom.Application.TeamCarts.Queries.GetTeamCartDetails;
using YummyZoom.Application.TeamCarts.Queries.GetTeamCartRealTimeViewModel;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Infrastructure.Serialization.JsonOptions;
using YummyZoom.SharedKernel;
using YummyZoom.Web.ApiContractTests.Infrastructure;

namespace YummyZoom.Web.ApiContractTests.TeamCarts;

public class TeamCartLifecycleContractTests
{
    #region Create TeamCart Tests

    [Test]
    public async Task CreateTeamCart_WithValidRequest_Returns201Created()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        var expectedCartId = Guid.NewGuid();
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<CreateTeamCartCommand>();
            var cmd = (CreateTeamCartCommand)req;
            cmd.RestaurantId.Should().NotBeEmpty();
            cmd.HostName.Should().NotBeNullOrEmpty();
            return Result.Success(new CreateTeamCartResponse(
                expectedCartId,
                "SHARE_TOKEN_123",
                DateTime.UtcNow.AddHours(1)
            ));
        });

        var body = new CreateTeamCartCommand(
            Guid.NewGuid(),
            "John's Team Cart",
            DateTime.UtcNow.AddHours(2)
        );

        var requestJson = JsonSerializer.Serialize(body, DomainJson.Options);
        TestContext.WriteLine("REQUEST POST /api/v1/team-carts");
        TestContext.WriteLine(requestJson);

        var resp = await client.PostAsJsonAsync("/api/v1/team-carts", body, DomainJson.Options);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        resp.Headers.Location.Should().NotBeNull();
        resp.Headers.Location!.ToString().Should().Contain($"/api/v1/team-carts/{expectedCartId}");

        var responseDto = JsonSerializer.Deserialize<CreateTeamCartResponse>(rawResponse, DomainJson.Options);
        responseDto!.TeamCartId.Should().Be(expectedCartId);
        responseDto.ShareToken.Should().Be("SHARE_TOKEN_123");
        responseDto.ShareTokenExpiresAtUtc.Should().BeAfter(DateTime.UtcNow);
    }

    [Test]
    public async Task CreateTeamCart_WhenRestaurantNotFound_Returns404Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        factory.Sender.RespondWith(_ => Result.Failure<CreateTeamCartResponse>(
            Error.NotFound("CreateTeamCart.RestaurantNotFound", "Restaurant not found")));

        var body = new CreateTeamCartCommand(
            Guid.NewGuid(),
            "Test Cart",
            null
        );

        var requestJson = JsonSerializer.Serialize(body, DomainJson.Options);
        TestContext.WriteLine("REQUEST POST /api/v1/team-carts");
        TestContext.WriteLine(requestJson);

        var resp = await client.PostAsJsonAsync("/api/v1/team-carts", body, DomainJson.Options);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = JsonSerializer.Deserialize<ProblemDetails>(rawResponse);
        problem!.Status.Should().Be(404);
        problem.Title.Should().Be("CreateTeamCart");
    }

    [Test]
    public async Task CreateTeamCart_WithoutAuth_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();

        var body = new CreateTeamCartCommand(Guid.NewGuid(), "Test", null);
        var requestJson = JsonSerializer.Serialize(body, DomainJson.Options);
        TestContext.WriteLine("REQUEST POST /api/v1/team-carts");
        TestContext.WriteLine(requestJson);

        var resp = await client.PostAsJsonAsync("/api/v1/team-carts", body, DomainJson.Options);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Get TeamCart Details Tests

    [Test]
    public async Task GetTeamCartDetails_WhenFound_Returns200()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        var cartId = Guid.NewGuid();
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<GetTeamCartDetailsQuery>();
            var query = (GetTeamCartDetailsQuery)req;
            query.TeamCartIdGuid.Should().Be(cartId);
            return Result.Success(new GetTeamCartDetailsResponse(CreateSampleTeamCartDetailsDto(cartId)));
        });

        var path = $"/api/v1/team-carts/{cartId}";
        TestContext.WriteLine($"REQUEST GET {path}");

        var resp = await client.GetAsync(path);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(rawResponse);
        doc.RootElement.GetProperty("teamCart").GetProperty("teamCartId").GetGuid().Should().Be(cartId);
        doc.RootElement.GetProperty("teamCart").GetProperty("status").GetString().Should().Be("Open");
    }

    [Test]
    public async Task GetTeamCartDetails_WhenNotFound_Returns404Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        var cartId = Guid.NewGuid();
        factory.Sender.RespondWith(_ => Result.Failure<GetTeamCartDetailsResponse>(
            Error.NotFound("TeamCart.NotFound", "TeamCart not found")));

        var path = $"/api/v1/team-carts/{cartId}";
        TestContext.WriteLine($"REQUEST GET {path}");

        var resp = await client.GetAsync(path);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = JsonSerializer.Deserialize<ProblemDetails>(rawResponse);
        problem!.Status.Should().Be(404);
        problem.Title.Should().Be("TeamCart");
    }

    [Test]
    public async Task GetTeamCartDetails_WithoutAuth_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();

        var path = $"/api/v1/team-carts/{Guid.NewGuid()}";
        TestContext.WriteLine($"REQUEST GET {path}");

        var resp = await client.GetAsync(path);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Get TeamCart RealTime ViewModel Tests

    [Test]
    public async Task GetTeamCartRealTimeViewModel_WhenFound_Returns200()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        var cartId = Guid.NewGuid();
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<GetTeamCartRealTimeViewModelQuery>();
            var query = (GetTeamCartRealTimeViewModelQuery)req;
            query.TeamCartIdGuid.Should().Be(cartId);
            return Result.Success(new GetTeamCartRealTimeViewModelResponse(CreateSampleTeamCartViewModel(cartId)));
        });

        var path = $"/api/v1/team-carts/{cartId}/rt";
        TestContext.WriteLine($"REQUEST GET {path}");

        var resp = await client.GetAsync(path);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(rawResponse);
        doc.RootElement.GetProperty("teamCart").GetProperty("cartId").GetGuid().Should().Be(cartId);
        doc.RootElement.GetProperty("teamCart").GetProperty("status").GetString().Should().Be("Open");
        doc.RootElement.GetProperty("teamCart").GetProperty("version").GetInt64().Should().BeGreaterThan(0);
    }

    [Test]
    public async Task GetTeamCartRealTimeViewModel_WhenNotFound_Returns404Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        var cartId = Guid.NewGuid();
        factory.Sender.RespondWith(_ => Result.Failure<GetTeamCartRealTimeViewModelResponse>(
            Error.NotFound("TeamCart.NotFound", "TeamCart not found")));

        var path = $"/api/v1/team-carts/{cartId}/rt";
        TestContext.WriteLine($"REQUEST GET {path}");

        var resp = await client.GetAsync(path);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = JsonSerializer.Deserialize<ProblemDetails>(rawResponse);
        problem!.Status.Should().Be(404);
        problem.Title.Should().Be("TeamCart");
    }

    #endregion

    #region Join TeamCart Tests

    [Test]
    public async Task JoinTeamCart_WithValidToken_Returns204()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-2");

        var cartId = Guid.NewGuid();
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<JoinTeamCartCommand>();
            var cmd = (JoinTeamCartCommand)req;
            cmd.TeamCartId.Should().Be(cartId);
            cmd.ShareToken.Should().Be("VALID_TOKEN");
            cmd.GuestName.Should().Be("Jane Doe");
            return Result.Success();
        });

        var body = new { ShareToken = "VALID_TOKEN", GuestName = "Jane Doe" };
        var path = $"/api/v1/team-carts/{cartId}/join";

        var requestJson = JsonSerializer.Serialize(body, DomainJson.Options);
        TestContext.WriteLine($"REQUEST POST {path}");
        TestContext.WriteLine(requestJson);

        var resp = await client.PostAsJsonAsync(path, body, DomainJson.Options);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task JoinTeamCart_WithInvalidToken_Returns400Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-2");

        var cartId = Guid.NewGuid();
        factory.Sender.RespondWith(_ => Result.Failure(
            Error.Validation("TeamCart.InvalidShareToken", "Invalid share token")));

        var body = new { ShareToken = "INVALID_TOKEN", GuestName = "Jane Doe" };
        var path = $"/api/v1/team-carts/{cartId}/join";

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
        problem.Title.Should().Be("TeamCart");
    }

    #endregion

    #region Helper Methods

    private static TeamCartDetailsDto CreateSampleTeamCartDetailsDto(Guid cartId)
        => new(
            TeamCartId: cartId,
            RestaurantId: Guid.NewGuid(),
            HostUserId: Guid.NewGuid(),
            Status: TeamCartStatus.Open,
            ShareTokenMasked: "TOKEN***",
            Deadline: DateTime.UtcNow.AddHours(2),
            CreatedAt: DateTime.UtcNow.AddMinutes(-30),
            ExpiresAt: DateTime.UtcNow.AddHours(1),
            TipAmount: 0m,
            TipCurrency: "USD",
            AppliedCouponId: null,
            DiscountAmount: 0m,
            DiscountCurrency: "USD",
            Subtotal: 25.00m,
            DeliveryFee: 5.00m,
            TaxAmount: 2.50m,
            Total: 32.50m,
            Currency: "USD",
            Members: Array.Empty<TeamCartMemberDto>(),
            Items: Array.Empty<TeamCartItemDto>(),
            MemberPayments: Array.Empty<MemberPaymentDto>()
        );

    private static TeamCartViewModel CreateSampleTeamCartViewModel(Guid cartId)
    {
        return new TeamCartViewModel
        {
            CartId = TeamCartId.Create(cartId),
            RestaurantId = Guid.NewGuid(),
            RestaurantName = "Sample Restaurant",
            Status = TeamCartStatus.Open,
            Deadline = DateTime.UtcNow.AddHours(2),
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            ShareTokenMasked = "TOKEN***",
            TipAmount = 0m,
            TipCurrency = "USD",
            CouponCode = null,
            DiscountAmount = 0m,
            DiscountCurrency = "USD",
            Subtotal = 25.00m,
            Currency = "USD",
            DeliveryFee = 5.00m,
            TaxAmount = 2.50m,
            Total = 32.50m,
            CashOnDeliveryPortion = 0m,
            Version = 1
        };
    }

    #endregion
}

