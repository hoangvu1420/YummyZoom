using System.Text.Json;
using Dapper;
using YummyZoom.Application.Common.Caching;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Restaurants.Queries.Common;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.TagEntity.ValueObjects;
using YummyZoom.Infrastructure.Serialization.JsonOptions;

namespace YummyZoom.Infrastructure.Persistence.ReadModels.FullMenu;

public sealed class FullMenuViewMaintainer : IFullMenuViewMaintainer
{
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly ICacheInvalidationPublisher _invalidation;

    public FullMenuViewMaintainer(IDbConnectionFactory dbConnectionFactory, ICacheInvalidationPublisher invalidation)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _invalidation = invalidation;
    }

    public async Task<(string menuJson, DateTimeOffset lastRebuiltAt)> RebuildAsync(Guid restaurantId, CancellationToken ct = default)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        // Trip 1: menu + categories + items via CTEs and QueryMultiple
        const string trip1Sql = """
            SELECT
              m."Id"          AS "Id",
              m."Name"        AS "Name",
              m."Description" AS "Description",
              m."IsEnabled"   AS "IsEnabled"
            FROM "Menus" m
            WHERE m."RestaurantId" = @RestaurantId
              AND m."IsEnabled" = TRUE
              AND m."IsDeleted" = FALSE
            LIMIT 1;

            SELECT
              c."Id"           AS "Id",
              c."Name"         AS "Name",
              c."DisplayOrder"  AS "DisplayOrder"
            FROM "MenuCategories" c
            WHERE c."MenuId" = (
                SELECT mm."Id"
                FROM "Menus" mm
                WHERE mm."RestaurantId" = @RestaurantId
                  AND mm."IsEnabled" = TRUE
                  AND mm."IsDeleted" = FALSE
                LIMIT 1
            )
              AND c."IsDeleted" = FALSE
            ORDER BY c."DisplayOrder", c."Name";

            SELECT
              i."Id"                 AS "Id",
              i."MenuCategoryId"     AS "MenuCategoryId",
              i."Name"               AS "Name",
              i."Description"        AS "Description",
              i."BasePrice_Amount"   AS "Amount",
              i."BasePrice_Currency" AS "Currency",
              i."ImageUrl"           AS "ImageUrl",
              i."IsAvailable"        AS "IsAvailable",
              i."DietaryTagIds"      AS "DietaryTagIdsJson",
              i."AppliedCustomizations" AS "AppliedCustomizationsJson"
            FROM "MenuItems" i
            WHERE i."RestaurantId" = @RestaurantId
              AND i."MenuCategoryId" IN (
                SELECT cc."Id"
                FROM "MenuCategories" cc
                WHERE cc."MenuId" = (
                    SELECT mm."Id"
                    FROM "Menus" mm
                    WHERE mm."RestaurantId" = @RestaurantId
                      AND mm."IsEnabled" = TRUE
                      AND mm."IsDeleted" = FALSE
                    LIMIT 1
                )
                  AND cc."IsDeleted" = FALSE
            )
              AND i."IsDeleted" = FALSE;
            """;

        using var grid1 = await connection.QueryMultipleAsync(new CommandDefinition(trip1Sql, new { RestaurantId = restaurantId }, cancellationToken: ct));
        var menu = grid1.ReadSingleOrDefault<MenuRow>();
        if (menu is null)
        {
            throw new InvalidOperationException("Enabled menu not found for restaurant");
        }
        var categories = grid1.Read<CategoryRow>().ToList();
        var itemRows = grid1.Read<ItemRow>().ToList();

        var categoryOrder = categories.Select(c => c.Id).ToList();

        // Deserialize JSONB columns for tags and applied customizations
        var items = itemRows.Select(r => new
        {
            r.Id,
            r.MenuCategoryId,
            r.Name,
            r.Description,
            Amount = r.Amount,
            Currency = r.Currency,
            r.ImageUrl,
            r.IsAvailable,
            DietaryTagIds = string.IsNullOrWhiteSpace(r.DietaryTagIdsJson)
                ? new List<TagId>()
                : (IReadOnlyList<TagId>)(JsonSerializer.Deserialize<List<TagId>>(r.DietaryTagIdsJson, DomainJson.Options) ?? new List<TagId>()),
            AppliedCustomizations = string.IsNullOrWhiteSpace(r.AppliedCustomizationsJson)
                ? new List<AppliedCustomization>()
                : (IReadOnlyList<AppliedCustomization>)(JsonSerializer.Deserialize<List<AppliedCustomization>>(r.AppliedCustomizationsJson, DomainJson.Options) ?? new List<AppliedCustomization>())
        }).ToList();

        // 1) Customization groups referenced by items
        var groupIds = items
            .SelectMany(i => i.AppliedCustomizations.Select(c => c.CustomizationGroupId.Value))
            .Distinct()
            .ToArray();

        // Trip 2: groups + choices + tags referenced by items
        var groups = new List<GroupRow>();
        var choices = new List<ChoiceRow>();

        // 2) Tag legend referenced by items
        var tagIds = items
            .SelectMany(i => i.DietaryTagIds.Select(t => t.Value))
            .Distinct()
            .ToArray();

        var tags = new List<TagRow>();
        const string trip2Sql = """
            SELECT
              g."Id"            AS "Id",
              g."GroupName"     AS "GroupName",
              g."MinSelections" AS "MinSelections",
              g."MaxSelections" AS "MaxSelections"
            FROM "CustomizationGroups" g
            WHERE g."RestaurantId" = @RestaurantId
              AND g."IsDeleted" = FALSE
              AND g."Id" = ANY(@GroupIds::uuid[]);

            SELECT
              c."CustomizationGroupId"   AS "CustomizationGroupId",
              c."ChoiceId"               AS "Id",
              c."Name"                   AS "Name",
              c."PriceAdjustment_Amount" AS "Amount",
              c."PriceAdjustment_Currency" AS "Currency",
              c."IsDefault"              AS "IsDefault",
              c."DisplayOrder"           AS "DisplayOrder"
            FROM "CustomizationChoices" c
            WHERE c."CustomizationGroupId" = ANY(@GroupIds::uuid[])
            ORDER BY c."CustomizationGroupId", c."DisplayOrder", c."Name";

            SELECT
              t."Id"         AS "Id",
              t."TagName"    AS "TagName",
              t."TagCategory" AS "TagCategory"
            FROM "Tags" t
            WHERE t."IsDeleted" = FALSE
              AND t."Id" = ANY(@TagIds::uuid[]);
            """;

        using (var grid2 = await connection.QueryMultipleAsync(new CommandDefinition(trip2Sql, new { RestaurantId = restaurantId, GroupIds = groupIds, TagIds = tagIds }, cancellationToken: ct)))
        {
            groups = grid2.Read<GroupRow>().ToList();
            choices = grid2.Read<ChoiceRow>().ToList();
            tags = grid2.Read<TagRow>().ToList();
        }

        // Build normalized JSON object
        var now = DateTimeOffset.UtcNow;
        var doc = new
        {
            version = 1,
            restaurantId,
            menuId = menu.Id,
            menuName = menu.Name,
            menuDescription = menu.Description,
            menuEnabled = menu.IsEnabled,
            lastRebuiltAt = now,
            currency = items.Select(i => i.Currency).FirstOrDefault() ?? "USD",
            categories = new
            {
                order = categoryOrder,
                byId = categories.ToDictionary(
                    c => c.Id,
                    c => new
                    {
                        id = c.Id,
                        name = c.Name,
                        displayOrder = c.DisplayOrder,
                        itemOrder = items.Where(i => i.MenuCategoryId == c.Id)
                                         .OrderBy(i => i.Name)
                                         .Select(i => i.Id)
                                         .ToList()
                    })
            },
            items = new
            {
                byId = items.ToDictionary(
                    i => i.Id,
                    i => new
                    {
                        id = i.Id,
                        categoryId = i.MenuCategoryId,
                        name = i.Name,
                        description = i.Description,
                        price = new { amount = i.Amount, currency = i.Currency },
                        imageUrl = i.ImageUrl,
                        isAvailable = i.IsAvailable,
                        dietaryTagIds = i.DietaryTagIds.Select(t => t.Value).ToList(),
                        customizationGroups = i.AppliedCustomizations
                            .OrderBy(c => c.DisplayOrder)
                            .Select(c => new { groupId = c.CustomizationGroupId.Value, displayTitle = c.DisplayTitle, displayOrder = c.DisplayOrder })
                            .ToList()
                    })
            },
            customizationGroups = new
            {
                byId = groups.ToDictionary(
                    g => g.Id,
                    g => new
                    {
                        id = g.Id,
                        name = g.GroupName,
                        min = g.MinSelections,
                        max = g.MaxSelections,
                        options = choices.Where(c => c.CustomizationGroupId == g.Id)
                            .OrderBy(c => c.DisplayOrder)
                            .ThenBy(c => c.Name)
                            .Select(c => new
                            {
                                id = c.Id,
                                name = c.Name,
                                priceDelta = new { amount = c.Amount, currency = c.Currency },
                                isDefault = c.IsDefault,
                                displayOrder = c.DisplayOrder
                            }).ToList()
                    })
            },
            tagLegend = new
            {
                byId = tags.ToDictionary(t => t.Id, t => new { name = t.TagName, category = t.TagCategory })
            }
        };

        var json = JsonSerializer.Serialize(doc, DomainJson.Options);
        return (json, now);
    }

    public async Task UpsertAsync(Guid restaurantId, string menuJson, DateTimeOffset lastRebuiltAt, CancellationToken ct = default)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        const string sql = """
            INSERT INTO "FullMenuViews" ("RestaurantId", "MenuJson", "LastRebuiltAt")
            VALUES (@RestaurantId, CAST(@MenuJson AS jsonb), @LastRebuiltAt)
            ON CONFLICT ("RestaurantId")
            DO UPDATE SET
                "MenuJson" = CAST(EXCLUDED."MenuJson" AS jsonb),
                "LastRebuiltAt" = EXCLUDED."LastRebuiltAt";
            """;

        await connection.ExecuteAsync(new CommandDefinition(sql, new { RestaurantId = restaurantId, MenuJson = menuJson, LastRebuiltAt = lastRebuiltAt }, cancellationToken: ct));

        // Publish cache invalidation for menu-related caches
        await _invalidation.PublishAsync(new CacheInvalidationMessage
        {
            Tags = new[] { $"restaurant:{restaurantId:N}:menu" },
            Reason = "FullMenuViewUpsert",
            SourceEvent = "FullMenuViewMaintainer"
        }, ct);
    }

    public async Task DeleteAsync(Guid restaurantId, CancellationToken ct = default)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        const string sql = """
            DELETE FROM "FullMenuViews"
            WHERE "RestaurantId" = @RestaurantId;
            """;

        await connection.ExecuteAsync(new CommandDefinition(sql, new { RestaurantId = restaurantId }, cancellationToken: ct));

        await _invalidation.PublishAsync(new CacheInvalidationMessage
        {
            Tags = new[] { $"restaurant:{restaurantId:N}:menu" },
            Reason = "FullMenuViewDelete",
            SourceEvent = "FullMenuViewMaintainer"
        }, ct);
    }

    // Dapper row-shaping types (private to this rebuilder)
    private sealed record MenuRow(Guid Id, string Name, string Description, bool IsEnabled);
    private sealed record CategoryRow(Guid Id, string Name, int DisplayOrder);
    private sealed record ItemRow(
        Guid Id,
        Guid MenuCategoryId,
        string Name,
        string Description,
        decimal Amount,
        string Currency,
        string? ImageUrl,
        bool IsAvailable,
        string? DietaryTagIdsJson,
        string? AppliedCustomizationsJson);
    private sealed record GroupRow(Guid Id, string GroupName, int MinSelections, int MaxSelections);
    private sealed record ChoiceRow(
        Guid CustomizationGroupId,
        Guid Id,
        string Name,
        decimal Amount,
        string Currency,
        bool IsDefault,
        int DisplayOrder);
    private sealed record TagRow(Guid Id, string TagName, string TagCategory);
}
