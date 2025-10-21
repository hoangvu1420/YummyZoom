using Dapper;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Restaurants.Queries.Common;
using YummyZoom.Application.Reviews.Queries.Common;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Restaurants.Queries.GetRestaurantAggregatedDetails;

public sealed class GetRestaurantAggregatedDetailsQueryHandler
    : IRequestHandler<GetRestaurantAggregatedDetailsQuery, Result<RestaurantAggregatedDetailsDto>>
{
    private readonly IDbConnectionFactory _db;

    public GetRestaurantAggregatedDetailsQueryHandler(IDbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<Result<RestaurantAggregatedDetailsDto>> Handle(
        GetRestaurantAggregatedDetailsQuery request,
        CancellationToken cancellationToken)
    {
        using var connection = _db.CreateConnection();

        const string sql = """
            SELECT
                r."Id"                          AS RestaurantId,
                r."Name"                        AS Name,
                r."LogoUrl"                     AS LogoUrl,
                r."BackgroundImageUrl"          AS BackgroundImageUrl,
                r."Description"                 AS Description,
                r."CuisineType"                 AS CuisineType,
                (
                    SELECT COALESCE(jsonb_agg(DISTINCT t."TagName" ORDER BY t."TagName"), '[]'::jsonb)::text
                    FROM "MenuItems" i
                    CROSS JOIN LATERAL jsonb_array_elements_text(i."DietaryTagIds") AS tag_id_text
                    JOIN "Tags" t ON t."Id" = tag_id_text::uuid
                    WHERE i."RestaurantId" = r."Id"
                      AND i."IsDeleted" = FALSE
                      AND t."IsDeleted" = FALSE
                      AND t."TagCategory" = 'Cuisine'
                )                               AS CuisineTagsJson,
                r."IsAcceptingOrders"           AS IsAcceptingOrders,
                r."IsVerified"                  AS IsVerified,
                r."Location_Street"             AS Street,
                r."Location_City"               AS City,
                r."Location_State"              AS State,
                r."Location_ZipCode"            AS ZipCode,
                r."Location_Country"            AS Country,
                r."ContactInfo_PhoneNumber"     AS PhoneNumber,
                r."ContactInfo_Email"           AS Email,
                r."BusinessHours"               AS BusinessHours,
                r."Created"                     AS EstablishedDate,
                COALESCE(r."LastModified", r."Created") AS LastModified,
                CASE 
                    WHEN CAST(@Lat AS double precision) IS NOT NULL AND CAST(@Lng AS double precision) IS NOT NULL 
                         AND r."Geo_Latitude" IS NOT NULL AND r."Geo_Longitude" IS NOT NULL THEN
                        6371 * 2 * ASIN(SQRT(POWER(SIN(RADIANS((CAST(@Lat AS double precision) - r."Geo_Latitude")/2)),2) 
                            + COS(RADIANS(CAST(@Lat AS double precision))) * COS(RADIANS(r."Geo_Latitude")) 
                            * POWER(SIN(RADIANS((CAST(@Lng AS double precision) - r."Geo_Longitude")/2)),2)))
                    ELSE NULL
                END                              AS DistanceKm
            FROM "Restaurants" r
            WHERE r."Id" = @RestaurantId AND r."IsDeleted" = false AND r."IsVerified" = true;

            SELECT
                "MenuJson"      AS MenuJson,
                "LastRebuiltAt" AS LastRebuiltAt
            FROM "FullMenuViews"
            WHERE "RestaurantId" = @RestaurantId;

            SELECT 
                s."AverageRating",
                s."TotalReviews",
                s."Ratings1",
                s."Ratings2",
                s."Ratings3",
                s."Ratings4",
                s."Ratings5",
                s."TotalWithText",
                s."LastReviewAtUtc",
                s."UpdatedAtUtc"
            FROM "RestaurantReviewSummaries" s
            WHERE s."RestaurantId" = @RestaurantId
            LIMIT 1;
            """;

        var cmd = new CommandDefinition(
            sql,
            new { request.RestaurantId, request.Lat, request.Lng },
            cancellationToken: cancellationToken);

        using var reader = await connection.QueryMultipleAsync(cmd);

        var infoRow = await reader.ReadSingleOrDefaultAsync<RestaurantPublicInfoData>();
        if (infoRow is null)
        {
            return Result.Failure<RestaurantAggregatedDetailsDto>(GetRestaurantAggregatedDetailsErrors.NotFound(request.RestaurantId));
        }

        var menuRow = await reader.ReadSingleOrDefaultAsync<MenuViewRow>();
        var summaryRow = await reader.ReadSingleOrDefaultAsync<RestaurantReviewSummaryDto>();

        var infoDto = RestaurantPublicInfoMapper.Map(infoRow);

        var menuDto = BuildMenuDto(menuRow, infoDto.LastModified);
        var summaryDto = summaryRow ?? BuildEmptySummary(infoDto.LastModified);

        var lastChanged = DetermineLastChangedUtc(
            infoDto.LastModified,
            menuDto.LastRebuiltAt,
            summaryDto.UpdatedAtUtc);

        var aggregate = new RestaurantAggregatedDetailsDto(
            infoDto,
            menuDto,
            summaryDto,
            lastChanged);

        return Result.Success(aggregate);
    }

    private static RestaurantAggregatedMenuDto BuildMenuDto(MenuViewRow? row, DateTimeOffset lastModifiedFallback)
    {
        if (row is null)
        {
            return new RestaurantAggregatedMenuDto("{}", lastModifiedFallback);
        }

        var rebuiltAt = row.LastRebuiltAt.Kind switch
        {
            DateTimeKind.Utc => new DateTimeOffset(row.LastRebuiltAt),
            DateTimeKind.Unspecified => new DateTimeOffset(DateTime.SpecifyKind(row.LastRebuiltAt, DateTimeKind.Utc)),
            _ => new DateTimeOffset(row.LastRebuiltAt.ToUniversalTime())
        };

        return new RestaurantAggregatedMenuDto(
            row.MenuJson ?? "{}",
            rebuiltAt);
    }

    private static RestaurantReviewSummaryDto BuildEmptySummary(DateTimeOffset lastModified)
    {
        var utc = lastModified.UtcDateTime;
        return new RestaurantReviewSummaryDto(0, 0, 0, 0, 0, 0, 0, 0, null, utc);
    }

    private static DateTimeOffset DetermineLastChangedUtc(
        DateTimeOffset infoLastModified,
        DateTimeOffset menuLastRebuiltAt,
        DateTime summaryUpdatedAtUtc)
    {
        DateTimeOffset summaryUpdatedAt;
        if (summaryUpdatedAtUtc.Kind == DateTimeKind.Unspecified)
        {
            summaryUpdatedAt = new DateTimeOffset(DateTime.SpecifyKind(summaryUpdatedAtUtc, DateTimeKind.Utc));
        }
        else
        {
            summaryUpdatedAt = new DateTimeOffset(summaryUpdatedAtUtc.ToUniversalTime());
        }

        var latest = infoLastModified;
        if (menuLastRebuiltAt > latest)
        {
            latest = menuLastRebuiltAt;
        }

        if (summaryUpdatedAt > latest)
        {
            latest = summaryUpdatedAt;
        }

        return latest;
    }

    private sealed class MenuViewRow
    {
        public string MenuJson { get; init; } = "{}";
        public DateTime LastRebuiltAt { get; init; }
    }
}
