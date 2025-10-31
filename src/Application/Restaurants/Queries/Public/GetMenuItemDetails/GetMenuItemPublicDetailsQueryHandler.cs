using System.Text.Json;
using Dapper;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Restaurants.Queries.Public.GetMenuItemDetails;

public sealed class GetMenuItemPublicDetailsQueryHandler
    : IRequestHandler<GetMenuItemPublicDetailsQuery, Result<MenuItemPublicDetailsDto>>
{
    private readonly IDbConnectionFactory _db;

    public GetMenuItemPublicDetailsQueryHandler(IDbConnectionFactory db)
    {
        _db = db;
    }

    private sealed record ItemRow(
        Guid RestaurantId,
        Guid ItemId,
        Guid CategoryId,
        string Name,
        string Description,
        decimal PriceAmount,
        string PriceCurrency,
        string? ImageUrl,
        bool IsAvailable,
        string? AppliedCustomizationsJson,
        DateTime LastModified);

    private sealed record GroupRow(
        Guid GroupId,
        string GroupName,
        int MinSelections,
        int MaxSelections);

    private sealed record ChoiceRow(
        Guid GroupId,
        Guid ChoiceId,
        string Name,
        decimal PriceAdjAmount,
        string PriceAdjCurrency,
        bool IsDefault,
        int DisplayOrder);

    private sealed record SalesRow(long? LifetimeQuantity);
    private sealed record RatingRow(double? AverageRating, int? TotalReviews);

    private sealed record AppliedCustomizationJson(Guid CustomizationGroupId, string? DisplayTitle, int DisplayOrder);

    public async Task<Result<MenuItemPublicDetailsDto>> Handle(GetMenuItemPublicDetailsQuery request, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();

        // 1) Load base item
        const string itemSql = """
            SELECT
                mi."RestaurantId"         AS "RestaurantId",
                mi."Id"                   AS "ItemId",
                mi."MenuCategoryId"       AS "CategoryId",
                mi."Name"                 AS "Name",
                mi."Description"          AS "Description",
                mi."BasePrice_Amount"     AS "PriceAmount",
                mi."BasePrice_Currency"   AS "PriceCurrency",
                mi."ImageUrl"             AS "ImageUrl",
                mi."IsAvailable"          AS "IsAvailable",
                mi."AppliedCustomizations" AS "AppliedCustomizationsJson",
                mi."LastModified"         AS "LastModified"
            FROM "MenuItems" mi
            WHERE mi."Id" = @ItemId
              AND mi."RestaurantId" = @RestaurantId
              AND mi."IsDeleted" = FALSE
            """;

        var item = await conn.QuerySingleOrDefaultAsync<ItemRow>(
            new CommandDefinition(itemSql, new { request.ItemId, request.RestaurantId }, cancellationToken: ct));

        if (item is null)
        {
            return Result.Failure<MenuItemPublicDetailsDto>(Error.NotFound(
                "Public.MenuItem.NotFound", "Menu item not found for the restaurant."));
        }

        // 2) Parse applied customizations to determine which groups to load
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var appliedList = string.IsNullOrWhiteSpace(item.AppliedCustomizationsJson)
            ? new List<AppliedCustomizationJson>()
            : (JsonSerializer.Deserialize<List<AppliedCustomizationJson>>(item.AppliedCustomizationsJson!, opts)
               ?? new List<AppliedCustomizationJson>());

        var groupIds = appliedList.Select(a => a.CustomizationGroupId).Distinct().ToArray();

        IReadOnlyList<GroupRow> groups = Array.Empty<GroupRow>();
        IReadOnlyList<ChoiceRow> choices = Array.Empty<ChoiceRow>();
        if (groupIds.Length > 0)
        {
            const string groupsSql = """
                SELECT g."Id" AS "GroupId", g."GroupName", g."MinSelections", g."MaxSelections"
                FROM "CustomizationGroups" g
                WHERE g."Id" = ANY(@GroupIds)
                  AND g."RestaurantId" = @RestaurantId
                  AND g."IsDeleted" = FALSE
                """;
            groups = (await conn.QueryAsync<GroupRow>(
                new CommandDefinition(groupsSql, new { GroupIds = groupIds, request.RestaurantId }, cancellationToken: ct))).AsList();

            const string choicesSql = """
                SELECT c."CustomizationGroupId" AS "GroupId",
                       c."ChoiceId"            AS "ChoiceId",
                       c."Name"                AS "Name",
                       c."PriceAdjustment_Amount"   AS "PriceAdjAmount",
                       c."PriceAdjustment_Currency" AS "PriceAdjCurrency",
                       c."IsDefault"           AS "IsDefault",
                       c."DisplayOrder"        AS "DisplayOrder"
                FROM "CustomizationChoices" c
                WHERE c."CustomizationGroupId" = ANY(@GroupIds)
                ORDER BY c."CustomizationGroupId", c."DisplayOrder" ASC
                """;
            choices = (await conn.QueryAsync<ChoiceRow>(
                new CommandDefinition(choicesSql, new { GroupIds = groupIds }, cancellationToken: ct))).AsList();
        }

        // 3) Sales summary (lifetime sold)
        const string salesSql = """
            SELECT ms."LifetimeQuantity" AS "LifetimeQuantity"
            FROM "MenuItemSalesSummaries" ms
            WHERE ms."RestaurantId" = @RestaurantId AND ms."MenuItemId" = @ItemId
            """;
        var sales = await conn.QuerySingleOrDefaultAsync<SalesRow>(
            new CommandDefinition(salesSql, new { request.RestaurantId, request.ItemId }, cancellationToken: ct));
        var soldCount = sales?.LifetimeQuantity ?? 0;

        // 4) Rating at restaurant level (v1 rule)
        const string ratingSql = """
            SELECT rr."AverageRating" AS "AverageRating", rr."TotalReviews" AS "TotalReviews"
            FROM "RestaurantReviewSummaries" rr
            WHERE rr."RestaurantId" = @RestaurantId
            """;
        var rating = await conn.QuerySingleOrDefaultAsync<RatingRow>(
            new CommandDefinition(ratingSql, new { request.RestaurantId }, cancellationToken: ct));

        // 5) Build customization DTOs
        var groupsById = groups.ToDictionary(g => g.GroupId, g => g);
        var choicesLookup = choices.GroupBy(c => c.GroupId).ToDictionary(g => g.Key, g => g.OrderBy(x => x.DisplayOrder).ToList());

        var customizationDtos = new List<CustomizationGroupDto>();
        foreach (var ap in appliedList.OrderBy(a => a.DisplayOrder))
        {
            if (!groupsById.TryGetValue(ap.CustomizationGroupId, out var gr)) continue;
            choicesLookup.TryGetValue(gr.GroupId, out var groupChoices);
            var type = gr.MinSelections == 1 && gr.MaxSelections == 1 ? "radio" : "multi";
            var required = gr.MinSelections > 0;
            var items = (groupChoices ?? new List<ChoiceRow>())
                .Select(ch => new CustomizationChoiceDto(
                    ch.ChoiceId,
                    ch.Name,
                    ch.PriceAdjAmount, // currency assumed identical to item for v1
                    ch.IsDefault,
                    false // outOfStock: inventory not modeled yet
                )).ToList();

            customizationDtos.Add(new CustomizationGroupDto(
                gr.GroupId,
                ap.DisplayTitle ?? gr.GroupName,
                type,
                required,
                gr.MinSelections,
                gr.MaxSelections,
                items));
        }

        // 6) Upsell heuristic (same category, top rolling30 then lifetime, exclude current)
        const string upsellSql = """
            SELECT 
                mi."Id" AS "ItemId", 
                mi."Name" AS "Name", 
                mi."BasePrice_Amount" AS "Price", 
                mi."ImageUrl" AS "ImageUrl"
            FROM "MenuItems" mi
            LEFT JOIN "MenuItemSalesSummaries" ms
              ON ms."RestaurantId" = mi."RestaurantId" AND ms."MenuItemId" = mi."Id"
            WHERE mi."RestaurantId" = @RestaurantId
              AND mi."MenuCategoryId" = @CategoryId
              AND mi."IsDeleted" = FALSE
              AND mi."IsAvailable" = TRUE
              AND mi."Id" <> @ItemId
            ORDER BY COALESCE(ms."Rolling30DayQuantity", 0) DESC, COALESCE(ms."LifetimeQuantity", 0) DESC, mi."LastModified" DESC
            LIMIT 3
            """;
        var upsell = (await conn.QueryAsync<UpsellSuggestionDto>(
            new CommandDefinition(upsellSql, new { request.RestaurantId, item.CategoryId, request.ItemId }, cancellationToken: ct))).AsList();

        // 7) LastModified for HTTP validators: prefer FullMenuView timestamp when available
        DateTimeOffset lastModified;
        const string viewSql = """
            SELECT "LastRebuiltAt" FROM "FullMenuViews" WHERE "RestaurantId" = @RestaurantId
            """;
        var rebuiltAt = await conn.ExecuteScalarAsync<DateTime?>(
            new CommandDefinition(viewSql, new { request.RestaurantId }, cancellationToken: ct));
        if (rebuiltAt.HasValue)
        {
            lastModified = rebuiltAt.Value.Kind == DateTimeKind.Unspecified
                ? new DateTimeOffset(DateTime.SpecifyKind(rebuiltAt.Value, DateTimeKind.Utc))
                : new DateTimeOffset(rebuiltAt.Value);
        }
        else
        {
            var dt = item.LastModified;
            lastModified = dt.Kind == DateTimeKind.Unspecified
                ? new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc))
                : new DateTimeOffset(dt);
        }

        // 8) Static hints/limits (configurable later)
        var notesHint = "Cho quán biết thêm về yêu cầu của bạn.";
        var limits = new ItemQuantityLimits(1, 99);

        var dto = new MenuItemPublicDetailsDto(
            item.RestaurantId,
            item.ItemId,
            item.Name,
            item.Description,
            item.ImageUrl,
            item.PriceAmount,
            item.PriceCurrency,
            item.IsAvailable,
            soldCount,
            rating?.AverageRating,
            rating?.TotalReviews,
            customizationDtos,
            notesHint,
            limits,
            upsell,
            lastModified);

        return Result.Success(dto);
    }
}

