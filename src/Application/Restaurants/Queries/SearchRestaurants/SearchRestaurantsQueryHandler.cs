using System.Text.Json;
using Dapper;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Orders.Queries.Common;
using YummyZoom.Application.Restaurants.Queries.Common;
using YummyZoom.SharedKernel;
using Result = YummyZoom.SharedKernel.Result;

namespace YummyZoom.Application.Restaurants.Queries.SearchRestaurants;

public sealed class SearchRestaurantsQueryHandler : IRequestHandler<SearchRestaurantsQuery, Result<PaginatedList<RestaurantSearchResultDto>>>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public SearchRestaurantsQueryHandler(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<Result<PaginatedList<RestaurantSearchResultDto>>> Handle(SearchRestaurantsQuery request, CancellationToken cancellationToken)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        // Stub: text + cuisine filter only; deterministic ordering by name asc then id
        const string selectColumns = """
            r."Id"                  AS RestaurantId,
            r."Name"                AS Name,
            r."LogoUrl"             AS LogoUrl,
            to_jsonb(array_remove(ARRAY[r."CuisineType"], NULL))::text AS CuisineTagsJson,
            COALESCE(rr."AverageRating", 0)::numeric AS AvgRating,
            COALESCE(rr."TotalReviews", 0)          AS RatingCount,
            r."Location_City"       AS City,
            CASE 
                WHEN CAST(@Lat AS double precision) IS NOT NULL AND CAST(@Lng AS double precision) IS NOT NULL 
                     AND r."Geo_Latitude" IS NOT NULL AND r."Geo_Longitude" IS NOT NULL THEN
                    6371 * 2 * ASIN(SQRT(POWER(SIN(RADIANS((CAST(@Lat AS double precision) - r."Geo_Latitude")/2)),2) 
                        + COS(RADIANS(CAST(@Lat AS double precision))) * COS(RADIANS(r."Geo_Latitude")) 
                        * POWER(SIN(RADIANS((CAST(@Lng AS double precision) - r."Geo_Longitude")/2)),2)))
                ELSE NULL
            END                      AS DistanceKm,
            CAST(r."Geo_Latitude" AS double precision)  AS Latitude,
            CAST(r."Geo_Longitude" AS double precision) AS Longitude
            """;

        var where = new List<string> { "r.\"IsDeleted\" = false", "r.\"IsVerified\" = true" };
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(request.Q))
        {
            where.Add("(r.\"Name\" ILIKE '%' || @Q || '%' )");
            parameters.Add("Q", request.Q);
        }

        if (!string.IsNullOrWhiteSpace(request.Cuisine))
        {
            where.Add("r.\"CuisineType\" ILIKE @Cuisine");
            parameters.Add("Cuisine", request.Cuisine);
        }

        if (request.MinRating.HasValue)
        {
            where.Add("COALESCE(rr.\"AverageRating\",0) >= @MinRating");
            parameters.Add("MinRating", request.MinRating.Value);
        }

        // Parameters used in distance calculation (may be null)
        parameters.Add("Lat", request.Lat);
        parameters.Add("Lng", request.Lng);

        // BBox filter using numeric lat/lon columns when provided
        if (!string.IsNullOrWhiteSpace(request.Bbox))
        {
            var parts = request.Bbox.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 4
                && double.TryParse(parts[0], out var minLon)
                && double.TryParse(parts[1], out var minLat)
                && double.TryParse(parts[2], out var maxLon)
                && double.TryParse(parts[3], out var maxLat)
                && minLon < maxLon && minLat < maxLat)
            {
                where.Add("r.\"Geo_Latitude\" IS NOT NULL AND r.\"Geo_Longitude\" IS NOT NULL");
                where.Add("r.\"Geo_Latitude\" BETWEEN @MinLat AND @MaxLat");
                where.Add("r.\"Geo_Longitude\" BETWEEN @MinLon AND @MaxLon");
                parameters.Add("MinLat", minLat);
                parameters.Add("MaxLat", maxLat);
                parameters.Add("MinLon", minLon);
                parameters.Add("MaxLon", maxLon);
            }
        }

        // Tag filters (by TagIds and/or TagNames). Matches restaurants having at least one menu item with any of the provided tags.
        var tagIdFilter = request.TagIds is { Count: > 0 };
        var tagNameFilter = request.Tags is { Count: > 0 };

        if (tagIdFilter || tagNameFilter)
        {
            var idExists = tagIdFilter
                ? "EXISTS (SELECT 1 FROM \"MenuItems\" i WHERE i.\"RestaurantId\" = r.\"Id\" AND i.\"IsDeleted\" = FALSE AND EXISTS (SELECT 1 FROM jsonb_array_elements_text(i.\"DietaryTagIds\") AS t(tag_id_text) WHERE (t.tag_id_text)::uuid = ANY(@TagIds)))"
                : null;

            var nameExists = tagNameFilter
                ? "EXISTS (SELECT 1 FROM \"MenuItems\" i WHERE i.\"RestaurantId\" = r.\"Id\" AND i.\"IsDeleted\" = FALSE AND EXISTS (SELECT 1 FROM jsonb_array_elements_text(i.\"DietaryTagIds\") AS t(tag_id_text) JOIN \"Tags\" tg ON tg.\"Id\" = (t.tag_id_text)::uuid AND tg.\"IsDeleted\" = FALSE WHERE LOWER(tg.\"TagName\") = ANY(@TagNamesLower)))"
                : null;

            var tagWhere = (idExists, nameExists) switch
            {
                (not null, not null) => $"(({idExists}) OR ({nameExists}))",
                (not null, null) => idExists!,
                (null, not null) => nameExists!,
                _ => null
            };

            if (tagWhere is not null)
            {
                where.Add(tagWhere);
                if (tagIdFilter) parameters.Add("TagIds", request.TagIds!.ToArray());
                if (tagNameFilter) parameters.Add("TagNamesLower", request.Tags!.Select(s => s.ToLowerInvariant()).ToArray());
            }
        }

        var fromAndWhere = $"FROM \"Restaurants\" r LEFT JOIN \"RestaurantReviewSummaries\" rr ON rr.\"RestaurantId\" = r.\"Id\" WHERE {string.Join(" AND ", where)}";

        string orderBy;
        var sort = (request.Sort ?? string.Empty).Trim().ToLowerInvariant();
        if (sort == "rating")
        {
            orderBy = "COALESCE(rr.\"AverageRating\",0) DESC NULLS LAST, r.\"Name\" ASC, r.\"Id\" ASC";
        }
        else if (sort == "distance" && request.Lat.HasValue && request.Lng.HasValue)
        {
            orderBy = "DistanceKm ASC NULLS LAST, r.\"Name\" ASC, r.\"Id\" ASC";
        }
        else if (sort == "popularity")
        {
            orderBy = "COALESCE(rr.\"TotalReviews\",0) DESC, COALESCE(rr.\"AverageRating\",0) DESC, r.\"Name\" ASC, r.\"Id\" ASC";
        }
        else
        {
            orderBy = "r.\"Name\" ASC, r.\"Id\" ASC";
        }

        var (countSql, pageSql) = DapperPagination.BuildPagedSql(selectColumns, fromAndWhere, orderBy, request.PageNumber, request.PageSize);

        var page = await connection.QueryPageAsync<RestaurantSearchRow>(
            countSql,
            pageSql,
            parameters,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        // Map dynamic rows -> DTOs with minimal JSON parsing for tags
        var mapped = page.Items
            .Select(row =>
            {
                IReadOnlyList<string> cuisine = Array.Empty<string>();
                try
                {
                    var tagsJson = row.CuisineTagsJson ?? "[]";
                    var list = JsonSerializer.Deserialize<List<string>>(tagsJson);
                    cuisine = list is not null ? list : Array.Empty<string>();
                }
                catch { }

                return new RestaurantSearchResultDto(
                    row.RestaurantId,
                    row.Name,
                    row.LogoUrl,
                    cuisine,
                    row.AvgRating,
                    row.RatingCount,
                    row.City,
                    row.DistanceKm,
                    row.Latitude,
                    row.Longitude);
            })
            .ToList();

        var resultPage = new PaginatedList<RestaurantSearchResultDto>(mapped, page.TotalCount, page.PageNumber, request.PageSize);
        return Result.Success(resultPage);
    }
}

file sealed class RestaurantSearchRow
{
    public Guid RestaurantId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? LogoUrl { get; init; }
    public string? CuisineTagsJson { get; init; }
    public decimal? AvgRating { get; init; }
    public int? RatingCount { get; init; }
    public string? City { get; init; }
    public decimal? DistanceKm { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
}
