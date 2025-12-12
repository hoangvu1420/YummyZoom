using Microsoft.AspNetCore.Mvc;
using YummyZoom.Application.Pricing.Queries.GetPricingPreview;

namespace YummyZoom.Web.Endpoints;

public class Pricing : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup(this)
            .RequireAuthorization();

        // POST /api/v1/pricing/preview
        group.MapPost("/preview", async ([FromBody] GetPricingPreviewRequest request, ISender sender) =>
        {
            var query = new GetPricingPreviewQuery(
                request.RestaurantId,
                request.Items.Select(i => new PricingPreviewItemDto(
                    i.MenuItemId,
                    i.Quantity,
                    i.Customizations?.Select(c => new PricingPreviewCustomizationDto(
                        c.CustomizationGroupId,
                        c.ChoiceIds
                    )).ToList()
                )).ToList(),
                request.CouponCode,
                request.TipAmount,
                request.IncludeCouponSuggestions
            );

            var result = await sender.Send(query);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("GetPricingPreview")
        .WithStandardResults<GetPricingPreviewResponse>();
    }
}

// Request body for pricing preview
public sealed record GetPricingPreviewRequest(
    Guid RestaurantId,
    List<PricingPreviewItemRequest> Items,
    string? CouponCode,
    decimal? TipAmount,
    bool IncludeCouponSuggestions = false
);

public sealed record PricingPreviewItemRequest(
    Guid MenuItemId,
    int Quantity,
    List<PricingPreviewCustomizationRequest>? Customizations
);

public sealed record PricingPreviewCustomizationRequest(
    Guid CustomizationGroupId,
    List<Guid> ChoiceIds
);
