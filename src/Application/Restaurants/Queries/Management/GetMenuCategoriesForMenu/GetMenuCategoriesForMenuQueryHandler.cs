using Dapper;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.SharedKernel;
using Result = YummyZoom.SharedKernel.Result;

namespace YummyZoom.Application.Restaurants.Queries.Management.GetMenuCategoriesForMenu;

public sealed class GetMenuCategoriesForMenuQueryHandler
    : IRequestHandler<GetMenuCategoriesForMenuQuery, Result<IReadOnlyList<MenuCategorySummaryDto>>>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public GetMenuCategoriesForMenuQueryHandler(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<Result<IReadOnlyList<MenuCategorySummaryDto>>> Handle(
        GetMenuCategoriesForMenuQuery request,
        CancellationToken cancellationToken)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        const string sql = """
            SELECT EXISTS (
                    SELECT 1
                    FROM "Menus" m
                    WHERE m."Id" = @MenuId
                        AND m."RestaurantId" = @RestaurantId
                        AND m."IsDeleted" = false
            ) AS "MenuExists";

            SELECT
                    mc."Id"           AS "CategoryId",
                    mc."Name"         AS "Name",
                    mc."DisplayOrder" AS "DisplayOrder",
                    (
                        SELECT COUNT(1)
                        FROM "MenuItems" mi
                        WHERE mi."MenuCategoryId" = mc."Id" AND mi."IsDeleted" = false
                    ) AS "ItemCount"
            FROM "MenuCategories" mc
            WHERE mc."MenuId" = @MenuId
                AND mc."IsDeleted" = false
            ORDER BY mc."DisplayOrder" ASC, mc."Id" ASC;
            """;

        var parameters = new { request.RestaurantId, request.MenuId };

        using var grid = await connection.QueryMultipleAsync(
                new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));

        var menuExists = await grid.ReadSingleAsync<bool>();
        if (!menuExists)
        {
            return Result.Failure<IReadOnlyList<MenuCategorySummaryDto>>(GetMenuCategoriesForMenuErrors.MenuNotFound);
        }

        var rows = (await grid.ReadAsync<MenuCategorySummaryRow>()).ToList();

        var mapped = rows
            .Select(r => new MenuCategorySummaryDto(r.CategoryId, r.Name, r.DisplayOrder, (int)r.ItemCount))
            .ToList();

        return Result.Success((IReadOnlyList<MenuCategorySummaryDto>)mapped);
    }
}

file sealed class MenuCategorySummaryRow
{
    public Guid CategoryId { get; init; }
    public string Name { get; init; } = string.Empty;
    public int DisplayOrder { get; init; }
    public long ItemCount { get; init; }
}
