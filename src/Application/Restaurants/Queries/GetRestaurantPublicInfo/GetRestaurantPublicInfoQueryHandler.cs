using Dapper;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Restaurants.Queries.Common;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Restaurants.Queries.GetRestaurantPublicInfo;

public sealed class GetRestaurantPublicInfoQueryHandler : IRequestHandler<GetRestaurantPublicInfoQuery, Result<RestaurantPublicInfoDto>>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public GetRestaurantPublicInfoQueryHandler(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<Result<RestaurantPublicInfoDto>> Handle(GetRestaurantPublicInfoQuery request, CancellationToken cancellationToken)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

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
            WHERE r."Id" = @RestaurantId AND r."IsDeleted" = false AND r."IsVerified" = true
            """;

        var row = await connection.QuerySingleOrDefaultAsync<RestaurantPublicInfoData>(
            new CommandDefinition(sql, new { request.RestaurantId, request.Lat, request.Lng }, cancellationToken: cancellationToken));

        if (row is null)
        {
            return Result.Failure<RestaurantPublicInfoDto>(GetRestaurantPublicInfoErrors.NotFound);
        }

        var dto = RestaurantPublicInfoMapper.Map(row);

        return Result.Success(dto);
    }
}
