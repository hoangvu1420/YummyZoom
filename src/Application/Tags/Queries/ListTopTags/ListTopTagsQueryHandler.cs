using Dapper;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Domain.TagEntity.Enums;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Tags.Queries.ListTopTags;

public sealed class ListTopTagsQueryHandler : IRequestHandler<ListTopTagsQuery, Result<IReadOnlyList<TopTagDto>>>
{
    private readonly IDbConnectionFactory _db;

    public ListTopTagsQueryHandler(IDbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<Result<IReadOnlyList<TopTagDto>>> Handle(ListTopTagsQuery request, CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(request.Limit <= 0 ? 10 : request.Limit, 1, 100);
        if (limit != request.Limit && request.Limit != 0)
        {
            return Result.Failure<IReadOnlyList<TopTagDto>>(ListTopTagsErrors.InvalidLimit);
        }

        using var connection = _db.CreateConnection();

        const string baseSql = """
            WITH item_tags AS (
                SELECT
                    (jsonb_array_elements_text(i."DietaryTagIds"))::uuid AS tag_id
                FROM "MenuItems" i
                JOIN "Restaurants" r ON r."Id" = i."RestaurantId"
                WHERE i."IsDeleted" = FALSE
                  AND r."IsDeleted" = FALSE
                  AND r."IsVerified" = TRUE
            )
            SELECT
                t."Id"          AS TagId,
                t."TagName"     AS TagName,
                t."TagCategory" AS TagCategoryName,
                COUNT(*)         AS UsageCount
            FROM item_tags it
            JOIN "Tags" t ON t."Id" = it.tag_id
            WHERE t."IsDeleted" = FALSE
            {0}
            GROUP BY t."Id", t."TagName", t."TagCategory"
            ORDER BY UsageCount DESC, t."TagName"
            LIMIT @Limit;
        """;

        var (whereClause, parameters) = BuildWhereClauseAndParameters(request.Categories, limit);
        var sql = string.Format(baseSql, whereClause);

        var rows = await connection.QueryAsync<Row>(new CommandDefinition(
            sql,
            parameters,
            cancellationToken: cancellationToken));

        var list = rows
            .Select(r => new TopTagDto(r.TagId, r.TagName, TagCategoryExtensions.Parse(r.TagCategoryName), (int)r.UsageCount))
            .ToList();
        return Result.Success<IReadOnlyList<TopTagDto>>(list);
    }

    private static (string whereClause, object parameters) BuildWhereClauseAndParameters(
        IReadOnlyList<TagCategory>? categories, 
        int limit)
    {
        if (categories == null || categories.Count == 0)
        {
            return (string.Empty, new { Limit = limit });
        }

        if (categories.Count == 1)
        {
            return ("AND t.\"TagCategory\" = @Category", 
                    new { Category = categories[0].ToStringValue(), Limit = limit });
        }

        // Multiple categories - use IN clause
        var categoryValues = categories.Select(c => c.ToStringValue()).ToArray();
        return ("AND t.\"TagCategory\" = ANY(@Categories)", 
                new { Categories = categoryValues, Limit = limit });
    }

    private sealed record Row(Guid TagId, string TagName, string TagCategoryName, long UsageCount)
    {
        public Row() : this(Guid.Empty, string.Empty, string.Empty, 0) { }
    }
}
