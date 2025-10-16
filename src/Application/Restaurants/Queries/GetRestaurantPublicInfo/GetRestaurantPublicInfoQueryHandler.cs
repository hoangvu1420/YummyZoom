using System.Text.Json;
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

        var row = await connection.QuerySingleOrDefaultAsync<RestaurantPublicInfoRow>(
            new CommandDefinition(sql, new { request.RestaurantId, request.Lat, request.Lng }, cancellationToken: cancellationToken));

        if (row is null)
        {
            return Result.Failure<RestaurantPublicInfoDto>(GetRestaurantPublicInfoErrors.NotFound);
        }

        // CuisineTags stored as jsonb/text array: parse minimally
        IReadOnlyList<string> cuisine = [];
        try
        {
            string tagsJson = row.CuisineTagsJson ?? "[]";
            cuisine = JsonSerializer.Deserialize<List<string>>(tagsJson) ?? [];
        }
        catch
        {
            cuisine = [];
        }

        var addressDto = new AddressDto(
            row.Street,
            row.City,
            row.State,
            row.ZipCode,
            row.Country);

        var contactInfoDto = new ContactInfoDto(
            row.PhoneNumber,
            row.Email);

        var dto = new RestaurantPublicInfoDto(
            row.RestaurantId,
            row.Name,
            row.LogoUrl,
            row.BackgroundImageUrl,
            row.Description,
            row.CuisineType,
            cuisine,
            row.IsAcceptingOrders,
            row.IsVerified,
            addressDto,
            contactInfoDto,
            row.BusinessHours,
            row.EstablishedDate,
            row.DistanceKm);

        return Result.Success(dto);
    }
}

file sealed class RestaurantPublicInfoRow
{
    public Guid RestaurantId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? LogoUrl { get; init; }
    public string? BackgroundImageUrl { get; init; }
    public string Description { get; init; } = string.Empty;
    public string CuisineType { get; init; } = string.Empty;
    public string? CuisineTagsJson { get; init; }
    public bool IsAcceptingOrders { get; init; }
    public bool IsVerified { get; init; }
    public string Street { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string ZipCode { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string BusinessHours { get; init; } = string.Empty;
    public DateTimeOffset EstablishedDate { get; init; }
    public decimal? DistanceKm { get; init; }
}
