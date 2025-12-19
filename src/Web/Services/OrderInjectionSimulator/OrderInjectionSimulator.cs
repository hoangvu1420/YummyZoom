using System.Collections.Concurrent;
using Dapper;
using MediatR;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Orders.Commands.InitiateOrder;
using YummyZoom.Domain.OrderAggregate.Enums;
using YummyZoom.Web.Security;
using YummyZoom.Web.Services.OrderFlowSimulator;
using YummyZoom.Web.Services.OrderFlowSimulator.Models;
using YummyZoom.Web.Services.OrderInjectionSimulator.Models;

namespace YummyZoom.Web.Services.OrderInjectionSimulator;

public sealed class OrderInjectionSimulator : IOrderInjectionSimulator
{
    private readonly IDbConnectionFactory _db;
    private readonly IDevImpersonationService _impersonation;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOrderFlowSimulator _orderFlowSimulator;
    private readonly Random _random = new();

    public OrderInjectionSimulator(
        IDbConnectionFactory db,
        IDevImpersonationService impersonation,
        IServiceScopeFactory scopeFactory,
        IOrderFlowSimulator orderFlowSimulator)
    {
        _db = db;
        _impersonation = impersonation;
        _scopeFactory = scopeFactory;
        _orderFlowSimulator = orderFlowSimulator;
    }

    public async Task<OrderInjectionResult> StartAsync(OrderInjectionRequest request, CancellationToken ct = default)
    {
        var count = request.Count.GetValueOrDefault(5);
        if (count <= 0 || count > 50)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Count), "count must be between 1 and 50.");
        }
        var interOrderDelay = request.InterOrderDelayMs is > 0
            ? TimeSpan.FromMilliseconds(request.InterOrderDelayMs.Value)
            : TimeSpan.FromSeconds(5);

        var scenario = (request.Scenario ?? "incomingOnly").Trim().ToLowerInvariant();
        if (scenario is not ("incomingonly" or "autoflow" or "fastautoflow"))
        {
            throw new ArgumentException("scenario must be one of: incomingOnly | autoFlow | fastAutoFlow");
        }

        using var conn = _db.CreateConnection();

        // Pick restaurant
        var restaurantId = request.RestaurantId ?? await conn.ExecuteScalarAsync<Guid?>(
            new CommandDefinition(
                """
                SELECT "Id" FROM "Restaurants"
                WHERE "IsDeleted" = false
                ORDER BY "Id"
                LIMIT 1
                """,
                cancellationToken: ct)) ?? throw new InvalidOperationException("No restaurants available for simulation.");

        // Load a customer user
        var userId = await conn.ExecuteScalarAsync<Guid?>(
            new CommandDefinition(
                """
                SELECT "Id" FROM "DomainUsers"
                WHERE "IsDeleted" = false AND "IsActive" = true
                ORDER BY "Id"
                LIMIT 1
                """,
                cancellationToken: ct)) ?? throw new InvalidOperationException("No active DomainUser found for simulation.");

        // Load available menu items for the restaurant
        var menuItems = (await conn.QueryAsync<Guid>(
            new CommandDefinition(
                """
                SELECT "Id"
                FROM "MenuItems"
                WHERE "RestaurantId" = @RestaurantId
                  AND "IsDeleted" = false
                  AND "IsAvailable" = true
                LIMIT 50
                """,
                new { RestaurantId = restaurantId },
                cancellationToken: ct))).ToList();

        if (menuItems.Count == 0)
        {
            throw new InvalidOperationException("No available menu items found for simulation.");
        }

        // Use restaurant address snapshot as delivery destination
        var addr = await conn.QuerySingleAsync<RestaurantAddressRow>(
            new CommandDefinition(
                """
                SELECT
                    "Location_Street"  AS Street,
                    "Location_City"    AS City,
                    "Location_State"   AS State,
                    "Location_ZipCode" AS ZipCode,
                    "Location_Country" AS Country
                FROM "Restaurants"
                WHERE "Id" = @RestaurantId
                """,
                new { RestaurantId = restaurantId },
                cancellationToken: ct));

        var maxItems = Math.Clamp(request.MaxItemsPerOrder ?? 3, 1, 5);
        var maxQty = Math.Clamp(request.MaxQuantityPerItem ?? 2, 1, 5);
        var notePool = request.NotePool?.Where(s => !string.IsNullOrWhiteSpace(s)).ToList() ?? new();
        var useRandomMenuItems = request.UseRandomMenuItems ?? true;

        var created = new ConcurrentBag<OrderInjectionOrder>();

        for (var i = 0; i < count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var items = BuildItems(menuItems, maxItems, maxQty, useRandomMenuItems);
            var note = notePool.Count > 0 ? notePool[_random.Next(notePool.Count)] : null;

            var orderId = await CreateOrderAsync(userId, restaurantId, items, addr, note, ct);

            created.Add(new OrderInjectionOrder
            {
                OrderId = orderId,
                Scenario = scenario,
                Status = "Placed",
                FlowStatus = scenario is "autoflow" or "fastautoflow" ? "Starting flow" : null
            });

            if (scenario is "autoflow" or "fastautoflow")
            {
                var flowScenario = scenario == "fastautoflow" ? "fastHappyPath" : "happyPath";
                var simReq = new SimulationRequest
                {
                    Scenario = flowScenario,
                    DelaysMs = request.DelaysMs,
                    EstimatedDeliveryMinutes = request.DelaysMs is null ? 40 : null
                };

                _ = _orderFlowSimulator.StartAsync(orderId, restaurantId, simReq, ct);
            }

            if (i < count - 1 && interOrderDelay > TimeSpan.Zero)
            {
                await Task.Delay(interOrderDelay, ct);
            }
        }

        return new OrderInjectionResult
        {
            RestaurantId = restaurantId,
            Orders = created.ToList()
        };
    }

    private async Task<Guid> CreateOrderAsync(
        Guid userId,
        Guid restaurantId,
        List<OrderItemDto> items,
        RestaurantAddressRow addr,
        string? note,
        CancellationToken ct)
    {
        Guid orderId = Guid.Empty;

        await _impersonation.RunAsUserAsync(userId, _scopeFactory, async services =>
        {
            var sender = services.GetRequiredService<ISender>();
            var cmd = new InitiateOrderCommand(
                CustomerId: userId,
                RestaurantId: restaurantId,
                Items: items,
                DeliveryAddress: new DeliveryAddressDto(
                    Street: addr.Street ?? "123 Main St",
                    City: addr.City ?? "City",
                    State: addr.State ?? "State",
                    ZipCode: addr.ZipCode ?? "00000",
                    Country: addr.Country ?? "Country"),
                PaymentMethod: PaymentMethodType.CashOnDelivery.ToString(),
                SpecialInstructions: note,
                CouponCode: null,
                TipAmount: null,
                TeamCartId: null,
                IdempotencyKey: null);

            var result = await sender.Send(cmd, ct);
            if (!result.IsSuccess)
            {
                throw new InvalidOperationException($"Failed to create simulated order: {result.Error.Description}");
            }

            orderId = result.Value.OrderId.Value;
        }, ct: ct);

        return orderId;
    }

    private List<OrderItemDto> BuildItems(IReadOnlyList<Guid> menuItems, int maxItems, int maxQty, bool useRandom)
    {
        if (!useRandom)
        {
            // Deterministic: first item, quantity 1
            return new List<OrderItemDto> { new(menuItems.First(), 1) };
        }

        var count = _random.Next(1, Math.Min(maxItems, menuItems.Count) + 1);
        var chosen = menuItems.OrderBy(_ => _random.Next()).Take(count).ToList();
        var items = new List<OrderItemDto>(count);

        foreach (var id in chosen)
        {
            var qty = _random.Next(1, maxQty + 1);
            items.Add(new OrderItemDto(id, qty));
        }

        return items;
    }

    private sealed record RestaurantAddressRow(
        string? Street,
        string? City,
        string? State,
        string? ZipCode,
        string? Country);
}
