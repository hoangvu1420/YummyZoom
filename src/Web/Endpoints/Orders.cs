using Microsoft.AspNetCore.Mvc;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Orders.Commands.AcceptOrder;
using YummyZoom.Application.Orders.Commands.CancelOrder;
using YummyZoom.Application.Orders.Commands.Common;
using YummyZoom.Application.Orders.Commands.InitiateOrder;
using YummyZoom.Application.Orders.Commands.MarkOrderDelivered;
using YummyZoom.Application.Orders.Commands.MarkOrderPreparing;
using YummyZoom.Application.Orders.Commands.MarkOrderReadyForDelivery;
using YummyZoom.Application.Orders.Commands.RejectOrder;
using YummyZoom.Application.Orders.Queries.Common;
using YummyZoom.Application.Orders.Queries.GetCustomerRecentOrders;
using YummyZoom.Application.Orders.Queries.GetOrderById;
using YummyZoom.Application.Orders.Queries.GetOrderStatus;

namespace YummyZoom.Web.Endpoints;

public class Orders : EndpointGroupBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup(this)
            .RequireAuthorization();

        // POST /api/v1/orders/initiate
        group.MapPost("/initiate", async ([FromBody] InitiateOrderRequest request, HttpContext context, ISender sender) =>
        {
            // Extract idempotency key from header
            var idempotencyKey = context.Request.Headers["Idempotency-Key"].FirstOrDefault();
            
            // Map request DTO -> command (explicit mapping keeps layers decoupled)
            var command = new InitiateOrderCommand(
                request.CustomerId,
                request.RestaurantId,
                request.Items.Select(i => new Application.Orders.Commands.InitiateOrder.OrderItemDto(
                    i.MenuItemId, 
                    i.Quantity,
                    i.Customizations?.Select(c => new OrderItemCustomizationRequestDto(
                        c.CustomizationGroupId,
                        c.ChoiceIds
                    )).ToList()
                )).ToList(),
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
                request.TeamCartId,
                idempotencyKey
            );

            var result = await sender.Send(command);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToIResult();
        })
        .WithName("InitiateOrder")
        .WithStandardResults<InitiateOrderResponse>();

        // POST /api/v1/orders/{orderId}/accept
        group.MapPost("/{orderId:guid}/accept", async (Guid orderId, [FromBody] AcceptOrderRequest body, ISender sender) =>
        {
            var command = new AcceptOrderCommand(orderId, body.RestaurantId, body.EstimatedDeliveryTime);
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("AcceptOrder")
        .WithStandardResults<OrderLifecycleResultDto>();

        // POST /api/v1/orders/{orderId}/reject
        group.MapPost("/{orderId:guid}/reject", async (Guid orderId, [FromBody] RejectOrderRequest body, ISender sender) =>
        {
            var command = new RejectOrderCommand(orderId, body.RestaurantId, body.Reason);
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("RejectOrder")
        .WithStandardResults<OrderLifecycleResultDto>();

        // POST /api/v1/orders/{orderId}/cancel
        group.MapPost("/{orderId:guid}/cancel", async (Guid orderId, [FromBody] CancelOrderRequest body, ISender sender) =>
        {
            // Actor user id inferred from auth principal; body.ActingUserId optional for staff tools
            var command = new CancelOrderCommand(orderId, body.RestaurantId, body.ActingUserId, body.Reason);
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("CancelOrder")
        .WithStandardResults<OrderLifecycleResultDto>();

        // POST /api/v1/orders/{orderId}/preparing
        group.MapPost("/{orderId:guid}/preparing", async (Guid orderId, [FromBody] SimpleRestaurantScoped body, ISender sender) =>
        {
            var command = new MarkOrderPreparingCommand(orderId, body.RestaurantId);
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("MarkOrderPreparing")
        .WithStandardResults<OrderLifecycleResultDto>();

        // POST /api/v1/orders/{orderId}/ready
        group.MapPost("/{orderId:guid}/ready", async (Guid orderId, [FromBody] SimpleRestaurantScoped body, ISender sender) =>
        {
            var command = new MarkOrderReadyForDeliveryCommand(orderId, body.RestaurantId);
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("MarkOrderReadyForDelivery")
        .WithStandardResults<OrderLifecycleResultDto>();

        // POST /api/v1/orders/{orderId}/delivered
        group.MapPost("/{orderId:guid}/delivered", async (Guid orderId, [FromBody] MarkDeliveredRequest body, ISender sender) =>
        {
            var command = new MarkOrderDeliveredCommand(orderId, body.RestaurantId, body.DeliveredAtUtc);
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("MarkOrderDelivered")
        .WithStandardResults<OrderLifecycleResultDto>();

        // GET /api/v1/orders/{orderId}
        group.MapGet("/{orderId:guid}", async (Guid orderId, ISender sender) =>
        {
            var query = new GetOrderByIdQuery(orderId);
            var result = await sender.Send(query);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("GetOrderById")
        .WithStandardResults<GetOrderByIdResponse>();

        // GET /api/v1/orders/{orderId}/status
        group.MapGet("/{orderId:guid}/status", async (Guid orderId, HttpContext http, ISender sender) =>
        {
            var query = new GetOrderStatusQuery(orderId);
            var result = await sender.Send(query);
            if (!result.IsSuccess)
            {
                return result.ToIResult();
            }

            var dto = result.Value;

            // Compute strong ETag from Version
            var etag = $"\"order-{dto.OrderId}-v{dto.Version}\"";
            var ifNoneMatch = http.Request.Headers.IfNoneMatch.FirstOrDefault();
            if (!string.IsNullOrEmpty(ifNoneMatch) && string.Equals(ifNoneMatch, etag, StringComparison.Ordinal))
            {
                return Results.StatusCode(StatusCodes.Status304NotModified);
            }

            http.Response.Headers.ETag = etag;
            http.Response.Headers.LastModified = dto.LastUpdateTimestamp.ToUniversalTime().ToString("R");
            http.Response.Headers.CacheControl = "no-cache, must-revalidate";
            return Results.Ok(dto);
        })
        .WithName("GetOrderStatus")
        .WithStandardResults<OrderStatusDto>();

        // GET /api/v1/orders/my?pageNumber=1&pageSize=20
        group.MapGet("/my", async (int? pageNumber, int? pageSize, ISender sender) =>
        {
            // Apply defaults after binding to avoid Minimal API early 400s for missing value-type properties
            var page = pageNumber ?? 1;
            var size = pageSize ?? 10;

            var query = new GetCustomerRecentOrdersQuery(page, size);
            var result = await sender.Send(query);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToIResult();
        })
        .WithName("GetCustomerRecentOrders")
        .WithStandardResults<PaginatedList<OrderSummaryDto>>();
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
    int Quantity,
    List<InitiateOrderCustomizationRequest>? Customizations = null
);

public sealed record InitiateOrderCustomizationRequest(
    Guid CustomizationGroupId,
    List<Guid> ChoiceIds
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
