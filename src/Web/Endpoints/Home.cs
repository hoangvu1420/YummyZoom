using YummyZoom.Application.Home.Queries.ActiveDeals;

namespace YummyZoom.Web.Endpoints;

public sealed class Home : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var publicGroup = app.MapGroup(this);

        // GET /api/v1/home/active-deals
        publicGroup.MapGet("/active-deals", async (int? limit, ISender sender) =>
        {
            var lim = limit ?? 10;
            var res = await sender.Send(new ListActiveDealsQuery(lim));
            return res.IsSuccess ? Results.Ok(res.Value) : res.ToIResult();
        })
        .WithName("Home_ActiveDeals")
        .WithSummary("Active deals by restaurant")
        .WithDescription("Returns a list of restaurants with currently active coupons and a best-available coupon label. Public endpoint.")
        .Produces<IReadOnlyList<ActiveDealCardDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}

