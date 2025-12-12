using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Application.Coupons.Queries.FastCheck;
using YummyZoom.Application.Coupons.Queries.Common;
using YummyZoom.Infrastructure.Serialization.JsonOptions;
using YummyZoom.Web.ApiContractTests.Infrastructure;

namespace YummyZoom.Web.ApiContractTests.Coupons;

public class FastCheckContractTests
{
    [Test]
    public async Task FastCheck_MapsRequestToQuery_Returns200()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        var expectedBest = new CouponSuggestion(
            Code: "SAVE15",
            Label: "15% off",
            Savings: 7.50m,
            IsEligible: true,
            EligibilityReason: null,
            MinOrderGap: 0m,
            ExpiresOn: DateTime.UtcNow.AddDays(3),
            Scope: "WholeOrder",
            Urgency: CouponUrgency.None);

        var expectedResponse = new CouponSuggestionsResponse(
            CartSummary: new CartSummary(28.98m, "USD", 3),
            BestDeal: expectedBest,
            Suggestions: new List<CouponSuggestion> { expectedBest }
        );

        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<FastCouponCheckQuery>();
            return YummyZoom.SharedKernel.Result.Success(expectedResponse);
        });

        var body = new FastCouponCheckQuery(
            RestaurantId: Guid.NewGuid(),
            Items: new List<FastCouponCheckItemDto>
            {
                new(Guid.NewGuid(), 2, null),
                new(Guid.NewGuid(), 1, null)
            },
            TipAmount: null
        );

        var requestJson = JsonSerializer.Serialize(body, DomainJson.Options);
        TestContext.WriteLine("REQUEST POST /api/v1/coupons/fast-check");
        TestContext.WriteLine(requestJson);

        var resp = await client.PostAsJsonAsync("/api/v1/coupons/fast-check", body, DomainJson.Options);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(raw);

        var dto = JsonSerializer.Deserialize<CouponSuggestionsResponse>(raw, DomainJson.Options);
        dto.Should().NotBeNull();
        dto!.BestDeal.Should().NotBeNull();
        dto.BestDeal!.Code.Should().Be(expectedBest.Code);
        dto.Suggestions.Should().HaveCount(1);
    }
}

