using YummyZoom.Application.Orders.Commands.InitiateOrder;
using YummyZoom.Application.Orders.Commands.AcceptOrder;
using YummyZoom.Application.Orders.Commands.RejectOrder;
using YummyZoom.Application.Orders.Commands.CancelOrder;
using YummyZoom.Application.Orders.Commands.MarkOrderPreparing;
using YummyZoom.Application.Orders.Commands.MarkOrderReadyForDelivery;
using YummyZoom.Application.Orders.Commands.MarkOrderDelivered;
using YummyZoom.Application.Orders.Common;
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
        group.MapPost("/initiate", async ([FromBody] InitiateOrderRequest request, ISender sender) =>
        {
            // Map request DTO -> command (explicit mapping keeps layers decoupled)
            var command = new InitiateOrderCommand(
                request.CustomerId,
                request.RestaurantId,
                request.Items.Select(i => new OrderItemDto(i.MenuItemId, i.Quantity)).ToList(),
                new DeliveryAddressDto(
                    request.DeliveryAddress.Street,
                    request.DeliveryAddress.City,
                    request.DeliveryAddress.State,
                    request.DeliveryAddress.ZipCode,
                    request.DeliveryAddress.Country
                ),
                request.PaymentMethod,
                request.SpecialInstructions,
                request.CouponCode,
                request.TipAmount,
                request.TeamCartId
            );

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToIResult();
        })
        .WithName("InitiateOrder")
        .WithStandardResults<InitiateOrderResponse>();

        // POST /api/orders/{orderId}/accept
        group.MapPost("/{orderId:guid}/accept", async (Guid orderId, [FromBody] AcceptOrderRequest body, ISender sender) =>
        {
            var command = new AcceptOrderCommand(orderId, body.RestaurantId, body.EstimatedDeliveryTime);
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("AcceptOrder")
        .WithStandardResults<OrderLifecycleResultDto>();

        // POST /api/orders/{orderId}/reject
        group.MapPost("/{orderId:guid}/reject", async (Guid orderId, [FromBody] RejectOrderRequest body, ISender sender) =>
        {
            var command = new RejectOrderCommand(orderId, body.RestaurantId, body.Reason);
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("RejectOrder")
        .WithStandardResults<OrderLifecycleResultDto>();

        // POST /api/orders/{orderId}/cancel
        group.MapPost("/{orderId:guid}/cancel", async (Guid orderId, [FromBody] CancelOrderRequest body, ISender sender) =>
        {
            // Actor user id inferred from auth principal; body.ActingUserId optional for staff tools
            var command = new CancelOrderCommand(orderId, body.RestaurantId, body.ActingUserId, body.Reason);
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("CancelOrder")
        .WithStandardResults<OrderLifecycleResultDto>();

        // POST /api/orders/{orderId}/preparing
        group.MapPost("/{orderId:guid}/preparing", async (Guid orderId, [FromBody] SimpleRestaurantScoped body, ISender sender) =>
        {
            var command = new MarkOrderPreparingCommand(orderId, body.RestaurantId);
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("MarkOrderPreparing")
        .WithStandardResults<OrderLifecycleResultDto>();

        // POST /api/orders/{orderId}/ready
        group.MapPost("/{orderId:guid}/ready", async (Guid orderId, [FromBody] SimpleRestaurantScoped body, ISender sender) =>
        {
            var command = new MarkOrderReadyForDeliveryCommand(orderId, body.RestaurantId);
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("MarkOrderReadyForDelivery")
        .WithStandardResults<OrderLifecycleResultDto>();

        // POST /api/orders/{orderId}/delivered
        group.MapPost("/{orderId:guid}/delivered", async (Guid orderId, [FromBody] MarkDeliveredRequest body, ISender sender) =>
        {
            var command = new MarkOrderDeliveredCommand(orderId, body.RestaurantId, body.DeliveredAtUtc);
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("MarkOrderDelivered")
        .WithStandardResults<OrderLifecycleResultDto>();
    }
}

// Request bodies (kept minimal & explicit)
public sealed record InitiateOrderRequest(
    Guid CustomerId,
    Guid RestaurantId,
    List<InitiateOrderItemRequest> Items,
    DeliveryAddressRequest DeliveryAddress,
    string PaymentMethod,
    string? SpecialInstructions,
    string? CouponCode,
    decimal? TipAmount,
    Guid? TeamCartId
);

public sealed record InitiateOrderItemRequest(
    Guid MenuItemId,
    int Quantity
);

public sealed record DeliveryAddressRequest(
    string Street,
    string City,
    string State,
    string ZipCode,
    string Country
);

public sealed record AcceptOrderRequest(Guid RestaurantId, DateTime EstimatedDeliveryTime);

public sealed record RejectOrderRequest(Guid RestaurantId, string? Reason);

public sealed record CancelOrderRequest(Guid RestaurantId, Guid? ActingUserId, string? Reason);

public sealed record SimpleRestaurantScoped(Guid RestaurantId);

public sealed record MarkDeliveredRequest(Guid RestaurantId, DateTime? DeliveredAtUtc);
