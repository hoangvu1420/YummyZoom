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
            END                      AS DistanceKm
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
                    row.DistanceKm);
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
}
