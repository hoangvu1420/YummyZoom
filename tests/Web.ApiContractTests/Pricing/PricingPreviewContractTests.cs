using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Application.Coupons.Queries.Common;
using YummyZoom.Application.Pricing.Queries.GetPricingPreview;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Infrastructure.Serialization.JsonOptions;
using YummyZoom.SharedKernel;
using YummyZoom.Web.ApiContractTests.Infrastructure;
using YummyZoom.Web.Endpoints;

namespace YummyZoom.Web.ApiContractTests.Pricing;

public class PricingPreviewContractTests
{
    [Test]
    public async Task PricingPreview_WithSuggestionsFlag_ReturnsEnvelope()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        var expectedSuggestion = new CouponSuggestion(
            Code: "SAVE15",
            Label: "15% off",
            Savings: 4.50m,
            IsEligible: true,
            EligibilityReason: null,
            MinOrderGap: 0,
            ExpiresOn: DateTime.UtcNow.AddDays(2),
            Scope: "WholeOrder",
            Urgency: CouponUrgency.ExpiresWithin7Days);

        var expectedCouponSuggestions = new CouponSuggestionsResponse(
            new CartSummary(30m, "USD", 2),
            expectedSuggestion,
            new List<CouponSuggestion> { expectedSuggestion }
        );

        var expectedResponse = new GetPricingPreviewResponse(
            Subtotal: new Money(30m, "USD"),
            DiscountAmount: new Money(4.5m, "USD"),
            DeliveryFee: new Money(2.99m, "USD"),
            TipAmount: new Money(2m, "USD"),
            TaxAmount: new Money(1.8m, "USD"),
            TotalAmount: new Money(32.29m, "USD"),
            Currency: "USD",
            Notes: new List<PricingPreviewNoteDto>(),
            CalculatedAt: DateTime.UtcNow,
            CouponSuggestions: expectedCouponSuggestions
        );

        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<GetPricingPreviewQuery>();
            var typed = (GetPricingPreviewQuery)req;
            typed.IncludeCouponSuggestions.Should().BeTrue();
            typed.Items.Should().HaveCount(1);
            return Result.Success(expectedResponse);
        });

        var restaurantId = Guid.NewGuid();
        var menuItemId = Guid.NewGuid();
        var customizationGroupId = Guid.NewGuid();
        var customizationChoiceId = Guid.NewGuid();

        var body = new GetPricingPreviewRequest(
            RestaurantId: restaurantId,
            Items: new List<PricingPreviewItemRequest>
            {
                new(
                    menuItemId,
                    2,
                    new List<PricingPreviewCustomizationRequest>
                    {
                        new(customizationGroupId, new List<Guid> { customizationChoiceId })
                    })
            },
            CouponCode: "SAVE15",
            TipAmount: 2m,
            IncludeCouponSuggestions: true
        );

        var requestJson = JsonSerializer.Serialize(body, DomainJson.Options);
        TestContext.WriteLine("REQUEST POST /api/v1/pricing/preview");
        TestContext.WriteLine(requestJson);

        var response = await client.PostAsJsonAsync("/api/v1/pricing/preview", body, DomainJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var raw = await response.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)response.StatusCode} {response.StatusCode}");
        TestContext.WriteLine(raw);

        var dto = JsonSerializer.Deserialize<GetPricingPreviewResponse>(raw, DomainJson.Options);
        dto.Should().NotBeNull();
        dto!.CouponSuggestions.Should().NotBeNull();
        dto.CouponSuggestions!.BestDeal.Should().NotBeNull();
        dto.CouponSuggestions.Suggestions.Should().HaveCount(1);
        dto.CouponSuggestions.Suggestions[0].Code.Should().Be("SAVE15");
    }
}
