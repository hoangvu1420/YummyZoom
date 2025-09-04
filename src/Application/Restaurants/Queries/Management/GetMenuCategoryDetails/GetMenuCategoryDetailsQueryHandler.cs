using Dapper;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Restaurants.Queries.Management.GetMenuCategoryDetails;

public sealed class GetMenuCategoryDetailsQueryHandler
    : IRequestHandler<GetMenuCategoryDetailsQuery, Result<MenuCategoryDetailsDto>>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public GetMenuCategoryDetailsQueryHandler(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<Result<MenuCategoryDetailsDto>> Handle(
        GetMenuCategoryDetailsQuery request,
        CancellationToken cancellationToken)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        const string sql = """
            SELECT
                m."Id"              AS "MenuId",
                m."Name"            AS "MenuName",
                mc."Id"             AS "CategoryId",
                mc."Name"           AS "Name",
                mc."DisplayOrder"   AS "DisplayOrder",
                mc."LastModified"   AS "LastModified",
                (
                  SELECT COUNT(1)
                  FROM "MenuItems" mi
                  WHERE mi."MenuCategoryId" = mc."Id" AND mi."IsDeleted" = false
                ) AS "ItemCount"
            FROM "MenuCategories" mc
            JOIN "Menus" m ON mc."MenuId" = m."Id"
            WHERE mc."Id" = @MenuCategoryId
              AND m."RestaurantId" = @RestaurantId
              AND mc."IsDeleted" = false
              AND m."IsDeleted" = false
            """;

        var row = await connection.QuerySingleOrDefaultAsync<MenuCategoryDetailsRow>(
            new CommandDefinition(sql, new { request.RestaurantId, request.MenuCategoryId }, cancellationToken: cancellationToken));

        if (row is null)
        {
            return Result.Failure<MenuCategoryDetailsDto>(GetMenuCategoryDetailsErrors.NotFound);
        }

        var dt = row.LastModified;
        var lastOffset = dt.Kind == DateTimeKind.Unspecified
            ? new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc))
            : new DateTimeOffset(dt);

        var dto = new MenuCategoryDetailsDto(
            row.MenuId,
            row.MenuName,
            row.CategoryId,
            row.Name,
            row.DisplayOrder,
            (int)row.ItemCount,
            lastOffset);

        return Result.Success(dto);
    }
}

file sealed class MenuCategoryDetailsRow
{
    public Guid MenuId { get; init; }
    public string MenuName { get; init; } = string.Empty;
    public Guid CategoryId { get; init; }
    public string Name { get; init; } = string.Empty;
    public int DisplayOrder { get; init; }
    public DateTime LastModified { get; init; }
    public long ItemCount { get; init; }
}

