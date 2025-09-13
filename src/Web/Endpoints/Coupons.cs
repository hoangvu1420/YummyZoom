using Microsoft.AspNetCore.Mvc;
using YummyZoom.Application.Coupons.Queries.FastCheck;
using YummyZoom.Web.Infrastructure;

namespace YummyZoom.Web.Endpoints;

public class Coupons : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup(this)
            .RequireAuthorization();

        // POST /api/v1/coupons/fast-check
        group.MapPost("/fast-check", async ([FromBody] FastCouponCheckQuery request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("Coupons_FastCheck")
        .WithSummary("Returns fast-evaluated coupon candidates and best deal for a cart snapshot");
    }
}
