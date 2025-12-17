using Dapper;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Orders.Queries.Common;
using YummyZoom.SharedKernel;
using Result = YummyZoom.SharedKernel.Result;

namespace YummyZoom.Application.Restaurants.Queries.Management.GetMenuItemsByCategory;

public sealed class GetMenuItemsByCategoryQueryHandler
    : IRequestHandler<GetMenuItemsByCategoryQuery, Result<PaginatedList<MenuItemSummaryDto>>>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public GetMenuItemsByCategoryQueryHandler(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<Result<PaginatedList<MenuItemSummaryDto>>> Handle(
        GetMenuItemsByCategoryQuery request,
        CancellationToken cancellationToken)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        // Ensure category belongs to restaurant and is not soft-deleted
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
            new CommandDefinition(existsSql, new { request.MenuCategoryId, request.RestaurantId }, cancellationToken: cancellationToken));
        if (exists is null)
        {
            return Result.Failure<PaginatedList<MenuItemSummaryDto>>(GetMenuItemsByCategoryErrors.NotFound);
        }

        // Build WHERE and parameters
        var where = new List<string>
        {
            "mi.\"RestaurantId\" = @RestaurantId",
            "mi.\"MenuCategoryId\" = @MenuCategoryId",
            "mi.\"IsDeleted\" = false"
        };

        var parameters = new DynamicParameters();
        parameters.Add("RestaurantId", request.RestaurantId);
        parameters.Add("MenuCategoryId", request.MenuCategoryId);

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
            mi."Name"               AS "Name",
            mi."Description"        AS "Description",
            mi."BasePrice_Amount"   AS "PriceAmount",
            mi."BasePrice_Currency" AS "PriceCurrency",
            mi."IsAvailable"        AS "IsAvailable",
            mi."ImageUrl"           AS "ImageUrl",
            mi."LastModified"       AS "LastModified"
            """;
        var fromWhere = $"FROM \"MenuItems\" mi WHERE {string.Join(" AND ", where)}";
        var orderBy = "mi.\"Name\" ASC, mi.\"Id\" ASC";

        var (countSql, pageSql) = DapperPagination.BuildPagedSql(selectCols, fromWhere, orderBy, request.PageNumber, request.PageSize);

        var page = await connection.QueryPageAsync<MenuItemRow>(
            countSql,
            pageSql,
            parameters,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        // Map to DTOs with DateTimeOffset conversion
        var mapped = page.Items.Select(r =>
        {
            var dt = r.LastModified;
            var last = dt.Kind == DateTimeKind.Unspecified
                ? new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc))
                : new DateTimeOffset(dt);
            return new MenuItemSummaryDto(
                r.ItemId,
                r.Name,
                r.Description,
                r.PriceAmount,
                r.PriceCurrency,
                r.IsAvailable,
                r.ImageUrl,
                last);
        }).ToList();

        var resultPage = new PaginatedList<MenuItemSummaryDto>(mapped, page.TotalCount, page.PageNumber, request.PageSize);
        return Result.Success(resultPage);
    }
}

file sealed class MenuItemRow
{
    public Guid ItemId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal PriceAmount { get; init; }
    public string PriceCurrency { get; init; } = string.Empty;
    public bool IsAvailable { get; init; }
    public string? ImageUrl { get; init; }
    public DateTime LastModified { get; init; }
}
