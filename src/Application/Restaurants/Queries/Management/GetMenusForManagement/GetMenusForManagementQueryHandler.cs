using Dapper;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Restaurants.Queries.Management.GetMenusForManagement;

public sealed class GetMenusForManagementQueryHandler
    : IRequestHandler<GetMenusForManagementQuery, Result<IReadOnlyList<MenuSummaryDto>>>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public GetMenusForManagementQueryHandler(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<Result<IReadOnlyList<MenuSummaryDto>>> Handle(
        GetMenusForManagementQuery request,
        CancellationToken cancellationToken)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        const string sql = """
            SELECT
                m."Id"               AS "MenuId",
                m."Name"             AS "Name",
                m."Description"      AS "Description",
                m."IsEnabled"        AS "IsEnabled",
                m."LastModified"     AS "LastModified",
                (
                  SELECT COUNT(1)
                  FROM "MenuCategories" mc
                  WHERE mc."MenuId" = m."Id" AND mc."IsDeleted" = false
                ) AS "CategoryCount",
                (
                  SELECT COUNT(1)
                  FROM "MenuItems" mi
                  JOIN "MenuCategories" mc ON mi."MenuCategoryId" = mc."Id"
                  WHERE mc."MenuId" = m."Id" AND mi."IsDeleted" = false AND mc."IsDeleted" = false
                ) AS "ItemCount"
            FROM "Menus" m
            WHERE m."RestaurantId" = @RestaurantId AND m."IsDeleted" = false
            ORDER BY m."Name" ASC, m."Id" ASC
            """;

        var rows = await connection.QueryAsync<MenuSummaryRow>(
            new CommandDefinition(sql, new { request.RestaurantId }, cancellationToken: cancellationToken));

        // Dapper maps timestamptz to DateTime (Kind may be Unspecified). Convert to DateTimeOffset.
        var mapped = rows.Select(r =>
        {
            var dt = r.LastModified;
            var dtoLast = dt.Kind == DateTimeKind.Unspecified
                ? new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc))
                : new DateTimeOffset(dt);
            return new MenuSummaryDto(
                r.MenuId,
                r.Name,
                r.Description,
                r.IsEnabled,
                dtoLast,
                (int)r.CategoryCount,
                (int)r.ItemCount);
        }).ToList();

        return Result.Success((IReadOnlyList<MenuSummaryDto>)mapped);
    }
}

file sealed class MenuSummaryRow
{
    public Guid MenuId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
    public DateTime LastModified { get; init; }
    public long CategoryCount { get; init; }
    public long ItemCount { get; init; }
}
