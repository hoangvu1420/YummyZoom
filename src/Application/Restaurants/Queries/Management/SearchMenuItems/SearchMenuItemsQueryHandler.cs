using Dapper;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Orders.Queries.Common;
using YummyZoom.SharedKernel;
using Result = YummyZoom.SharedKernel.Result;

namespace YummyZoom.Application.Restaurants.Queries.Management.SearchMenuItems;

public sealed class SearchMenuItemsQueryHandler
    : IRequestHandler<SearchMenuItemsQuery, Result<PaginatedList<MenuItemSearchResultDto>>>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public SearchMenuItemsQueryHandler(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    private sealed class MenuItemRow
    {
        public Guid ItemId { get; init; }
        public Guid MenuCategoryId { get; init; }
        public string CategoryName { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public decimal PriceAmount { get; init; }
        public string PriceCurrency { get; init; } = string.Empty;
        public bool IsAvailable { get; init; }
        public string? ImageUrl { get; init; }
        public DateTime LastModified { get; init; }
    }

    public async Task<Result<PaginatedList<MenuItemSearchResultDto>>> Handle(
        SearchMenuItemsQuery request,
        CancellationToken cancellationToken)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        if (request.MenuCategoryId is Guid categoryId)
        {
            const string existsSql = """
                SELECT 1
                FROM "MenuCategories" mc
                JOIN "Menus" m ON mc."MenuId" = m."Id"
                WHERE mc."Id" = @MenuCategoryId
                  AND m."RestaurantId" = @RestaurantId
                  AND mc."IsDeleted" = false
                  AND m."IsDeleted" = false
                LIMIT 1
                """;

            var exists = await connection.ExecuteScalarAsync<int?>(
                new CommandDefinition(existsSql, new { MenuCategoryId = categoryId, request.RestaurantId }, cancellationToken: cancellationToken));

            if (exists is null)
            {
                return Result.Failure<PaginatedList<MenuItemSearchResultDto>>(SearchMenuItemsErrors.CategoryNotFound);
            }
        }

        var where = new List<string>
        {
            "mi.\"RestaurantId\" = @RestaurantId",
            "mi.\"IsDeleted\" = false",
            "mc.\"IsDeleted\" = false",
            "m.\"IsDeleted\" = false"
        };

        var parameters = new DynamicParameters();
        parameters.Add("RestaurantId", request.RestaurantId);

        if (request.MenuCategoryId is Guid menuCategoryId)
        {
            where.Add("mi.\"MenuCategoryId\" = @MenuCategoryId");
            parameters.Add("MenuCategoryId", menuCategoryId);
        }

        if (!string.IsNullOrWhiteSpace(request.Q))
        {
            where.Add("(mi.\"Name\" ILIKE '%' || @Q || '%')");
            parameters.Add("Q", request.Q);
        }

        if (request.IsAvailable.HasValue)
        {
            where.Add("mi.\"IsAvailable\" = @IsAvailable");
            parameters.Add("IsAvailable", request.IsAvailable.Value);
        }

        const string selectCols = """
            mi."Id"                 AS "ItemId",
            mi."MenuCategoryId"     AS "MenuCategoryId",
            mc."Name"               AS "CategoryName",
            mi."Name"               AS "Name",
            mi."Description"        AS "Description",
            mi."BasePrice_Amount"   AS "PriceAmount",
            mi."BasePrice_Currency" AS "PriceCurrency",
            mi."IsAvailable"        AS "IsAvailable",
            mi."ImageUrl"           AS "ImageUrl",
            mi."LastModified"       AS "LastModified"
            """;

        var fromWhere = $"""
            FROM "MenuItems" mi
            JOIN "MenuCategories" mc ON mc."Id" = mi."MenuCategoryId"
            JOIN "Menus" m ON m."Id" = mc."MenuId"
            WHERE {string.Join(" AND ", where)}
            """;

        var orderBy = "mi.\"Name\" ASC, mi.\"Id\" ASC";

        var (countSql, pageSql) = DapperPagination.BuildPagedSql(
            selectCols,
            fromWhere,
            orderBy,
            request.PageNumber,
            request.PageSize);

        var page = await connection.QueryPageAsync<MenuItemRow>(
            countSql,
            pageSql,
            parameters,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var mapped = page.Items.Select(r =>
        {
            var dt = r.LastModified;
            var last = dt.Kind == DateTimeKind.Unspecified
                ? new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc))
                : new DateTimeOffset(dt);

            return new MenuItemSearchResultDto(
                r.ItemId,
                r.MenuCategoryId,
                r.CategoryName,
                r.Name,
                r.Description,
                r.PriceAmount,
                r.PriceCurrency,
                r.IsAvailable,
                r.ImageUrl,
                last);
        }).ToList();

        var resultPage = new PaginatedList<MenuItemSearchResultDto>(mapped, page.TotalCount, page.PageNumber, request.PageSize);
        return Result.Success(resultPage);
    }
}
