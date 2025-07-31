using YummyZoom.Application.Orders.Commands.InitiateOrder;
using Microsoft.AspNetCore.Mvc;

namespace YummyZoom.Web.Endpoints;

public class Orders : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup(this)
            .RequireAuthorization();

        // POST /api/orders/initiate
        group.MapPost("/initiate", async ([FromBody] InitiateOrderCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToIResult();
        })
        .WithName("InitiateOrder")
        .WithStandardResults<InitiateOrderResponse>();
    }
}