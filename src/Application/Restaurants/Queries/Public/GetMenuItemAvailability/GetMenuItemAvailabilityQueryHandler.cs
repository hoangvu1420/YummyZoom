using Dapper;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Restaurants.Queries.Public.GetMenuItemAvailability;

public sealed class GetMenuItemAvailabilityQueryHandler
    : IRequestHandler<GetMenuItemAvailabilityQuery, Result<MenuItemAvailabilityDto>>
{
    private readonly IDbConnectionFactory _db;

    public GetMenuItemAvailabilityQueryHandler(IDbConnectionFactory db)
    {
        _db = db;
    }

    private sealed record AvailRow(bool IsAcceptingOrders, bool ItemAvailable, bool ItemDeleted);

    public async Task<Result<MenuItemAvailabilityDto>> Handle(GetMenuItemAvailabilityQuery request, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();

        const string sql = """
            SELECT r."IsAcceptingOrders" AS "IsAcceptingOrders",
                   mi."IsAvailable"      AS "ItemAvailable",
                   mi."IsDeleted"        AS "ItemDeleted"
            FROM "Restaurants" r
            JOIN "MenuItems" mi ON mi."RestaurantId" = r."Id"
            WHERE r."Id" = @RestaurantId AND mi."Id" = @ItemId
            """;

        var row = await conn.QuerySingleOrDefaultAsync<AvailRow>(
            new CommandDefinition(sql, new { request.RestaurantId, request.ItemId }, cancellationToken: ct));

        if (row is null || row.ItemDeleted)
        {
            return Result.Failure<MenuItemAvailabilityDto>(Error.NotFound(
                "Public.MenuItemAvailability.NotFound", "Menu item not found for the restaurant."));
        }

        var isAvailable = row.IsAcceptingOrders && row.ItemAvailable;
        var now = DateTimeOffset.UtcNow;
        var dto = new MenuItemAvailabilityDto(request.RestaurantId, request.ItemId, isAvailable, null, now, 15);
        return Result.Success(dto);
    }
}

