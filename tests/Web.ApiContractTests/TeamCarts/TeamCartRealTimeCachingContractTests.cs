using System.Net;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Application.TeamCarts.Models;
using YummyZoom.Application.TeamCarts.Queries.GetTeamCartRealTimeViewModel;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.Web.ApiContractTests.Infrastructure;

namespace YummyZoom.Web.ApiContractTests.TeamCarts;

// Contract tests for GET /api/v1/team-carts/{id}/rt ETag and 304 semantics
public class TeamCartRealTimeCachingContractTests
{
    [Test]
    public async Task GetTeamCartRt_Returns200_With_Etag_And_CacheHeaders()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        var cartId = Guid.NewGuid();
        const long version = 5;

        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<GetTeamCartRealTimeViewModelQuery>();
            ((GetTeamCartRealTimeViewModelQuery)req).TeamCartIdGuid.Should().Be(cartId);
            return Result.Success(new GetTeamCartRealTimeViewModelResponse(CreateVm(cartId, version)));
        });

        var path = $"/api/v1/team-carts/{cartId}/rt";
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Headers.ETag.Should().NotBeNull();
        var expectedEtag = $"\"teamcart-{cartId}-v{version}\""; // quoted strong ETag
        resp.Headers.ETag!.Tag.Should().Be(expectedEtag);
        resp.Content.Headers.TryGetValues("Last-Modified", out var _).Should().BeTrue();
        resp.Headers.CacheControl!.NoCache.Should().BeTrue();
        resp.Headers.CacheControl.MustRevalidate.Should().BeTrue();

        using var doc = JsonDocument.Parse(raw);
        doc.RootElement.GetProperty("teamCart").GetProperty("version").GetInt64().Should().Be(version);
    }

    [Test]
    public async Task GetTeamCartRt_IfNoneMatch_Match_Returns304()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        var cartId = Guid.NewGuid();
        const long version = 7;

        factory.Sender.RespondWith(_ => Result.Success(new GetTeamCartRealTimeViewModelResponse(CreateVm(cartId, version))));

        var expectedEtag = $"\"teamcart-{cartId}-v{version}\"";
        client.DefaultRequestHeaders.TryAddWithoutValidation("If-None-Match", expectedEtag);

        var path = $"/api/v1/team-carts/{cartId}/rt";
        TestContext.WriteLine($"REQUEST GET {path} If-None-Match: {expectedEtag}");
        var resp = await client.GetAsync(path);
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");

        resp.StatusCode.Should().Be(HttpStatusCode.NotModified);
        var body = await resp.Content.ReadAsStringAsync();
        body.Should().BeNullOrEmpty();
        resp.Headers.ETag!.Tag.Should().Be(expectedEtag);
        resp.Headers.CacheControl!.NoCache.Should().BeTrue();
        resp.Headers.CacheControl.MustRevalidate.Should().BeTrue();
    }

    private static TeamCartViewModel CreateVm(Guid cartId, long version)
    {
        return new TeamCartViewModel
        {
            CartId = TeamCartId.Create(cartId),
            RestaurantId = Guid.NewGuid(),
            Status = TeamCartStatus.Open,
            Deadline = DateTime.UtcNow.AddHours(1),
            ExpiresAt = DateTime.UtcNow.AddHours(2),
            TipAmount = 0m,
            TipCurrency = "USD",
            DiscountAmount = 0m,
            DiscountCurrency = "USD",
            Subtotal = 0m,
            Currency = "USD",
            DeliveryFee = 0m,
            TaxAmount = 0m,
            Total = 0m,
            CashOnDeliveryPortion = 0m,
            QuoteVersion = 0,
            Version = version
        };
    }
}

