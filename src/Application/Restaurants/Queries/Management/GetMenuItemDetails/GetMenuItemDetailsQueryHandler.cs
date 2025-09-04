using System.Text.Json;
using Dapper;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Restaurants.Queries.Management.GetMenuItemDetails;

public sealed class GetMenuItemDetailsQueryHandler
    : IRequestHandler<GetMenuItemDetailsQuery, Result<MenuItemDetailsDto>>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public GetMenuItemDetailsQueryHandler(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<Result<MenuItemDetailsDto>> Handle(GetMenuItemDetailsQuery request, CancellationToken cancellationToken)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        const string sql = """
            SELECT
                mi."Id"                 AS "ItemId",
                mi."MenuCategoryId"     AS "CategoryId",
                mi."Name"               AS "Name",
                mi."Description"        AS "Description",
                mi."BasePrice_Amount"   AS "PriceAmount",
                mi."BasePrice_Currency" AS "PriceCurrency",
                mi."IsAvailable"        AS "IsAvailable",
                mi."ImageUrl"           AS "ImageUrl",
                mi."DietaryTagIds"      AS "DietaryTagIdsJson",
                mi."AppliedCustomizations" AS "AppliedCustomizationsJson",
                mi."LastModified"       AS "LastModified"
            FROM "MenuItems" mi
            WHERE mi."Id" = @MenuItemId
              AND mi."RestaurantId" = @RestaurantId
              AND mi."IsDeleted" = FALSE
            """;

        var row = await connection.QuerySingleOrDefaultAsync<MenuItemDetailsRow>(
            new CommandDefinition(sql, new { request.MenuItemId, request.RestaurantId }, cancellationToken: cancellationToken));

        if (row is null)
        {
            return Result.Failure<MenuItemDetailsDto>(GetMenuItemDetailsErrors.NotFound);
        }

        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Deserialize tag IDs (serialized as an array of GUIDs)
        var dietaryTagIds = string.IsNullOrWhiteSpace(row.DietaryTagIdsJson)
            ? new List<Guid>()
            : (JsonSerializer.Deserialize<List<Guid>>(row.DietaryTagIdsJson, opts) ?? new List<Guid>());

        // Deserialize applied customizations
        var applied = string.IsNullOrWhiteSpace(row.AppliedCustomizationsJson)
            ? new List<CustomizationJson>()
            : (JsonSerializer.Deserialize<List<CustomizationJson>>(row.AppliedCustomizationsJson, opts) ?? new List<CustomizationJson>());

        var appliedDtos = applied
            .Select(c => new MenuItemCustomizationRefDto(c.CustomizationGroupId, c.DisplayTitle ?? string.Empty, c.DisplayOrder))
            .ToList();

        var dt = row.LastModified;
        var last = dt.Kind == DateTimeKind.Unspecified
            ? new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc))
            : new DateTimeOffset(dt);

        var dto = new MenuItemDetailsDto(
            row.ItemId,
            row.CategoryId,
            row.Name,
            row.Description,
            row.PriceAmount,
            row.PriceCurrency,
            row.IsAvailable,
            row.ImageUrl,
            dietaryTagIds,
            appliedDtos,
            last);

        return Result.Success(dto);
    }
}

file sealed class MenuItemDetailsRow
{
    public Guid ItemId { get; init; }
    public Guid CategoryId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal PriceAmount { get; init; }
    public string PriceCurrency { get; init; } = string.Empty;
    public bool IsAvailable { get; init; }
    public string? ImageUrl { get; init; }
    public string? DietaryTagIdsJson { get; init; }
    public string? AppliedCustomizationsJson { get; init; }
    public DateTime LastModified { get; init; }
}

file sealed class CustomizationJson
{
    public Guid CustomizationGroupId { get; init; }
    public string? DisplayTitle { get; init; }
    public int DisplayOrder { get; init; }
}

