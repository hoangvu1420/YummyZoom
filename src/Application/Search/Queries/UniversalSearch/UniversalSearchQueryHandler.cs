using Dapper;
using Microsoft.Extensions.Options;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Orders.Queries.Common;
using YummyZoom.SharedKernel;
using Result = YummyZoom.SharedKernel.Result;

namespace YummyZoom.Application.Search.Queries.UniversalSearch;

// Request + DTO
public sealed partial record UniversalSearchQuery(
    string? Term,
    double? Latitude,
    double? Longitude,
    bool? OpenNow,
    string[]? Cuisines,
    string[]? Tags = null,
    short[]? PriceBands = null,
    bool IncludeFacets = false,
    int PageNumber = 1,
    int PageSize = 10
) : IRequest<Result<UniversalSearchResponseDto>>;

// Backward-compatible constructor to support existing call sites (tests, endpoints)
public sealed partial record UniversalSearchQuery
{
    public UniversalSearchQuery(
        string? term,
        double? latitude,
        double? longitude,
        bool? openNow,
        string[]? cuisines,
        int pageNumber,
        int pageSize)
        : this(term, latitude, longitude, openNow, cuisines, null, null, false, pageNumber, pageSize)
    { }
}

public sealed record SearchResultDto(
    Guid Id,
    string Type,
    Guid? RestaurantId,
    string Name,
    string? DescriptionSnippet,
    string? Cuisine,
    double Score,
    double? DistanceKm,
    IReadOnlyList<SearchBadgeDto> Badges,
    string? Reason
);

public sealed record UniversalSearchResponseDto(
    PaginatedList<SearchResultDto> Page,
    FacetBlock Facets
);

public sealed record FacetBlock(
    IReadOnlyList<FacetCount<string>> Cuisines,
    IReadOnlyList<FacetCount<string>> Tags,
    IReadOnlyList<FacetCount<short>> PriceBands,
    int OpenNowCount
);

public sealed record FacetCount<T>(T Value, int Count);

// Handler
public sealed class UniversalSearchQueryHandler
    : IRequestHandler<UniversalSearchQuery, Result<UniversalSearchResponseDto>>
{
    private readonly IDbConnectionFactory _db;
    private readonly ResultExplanationOptions _options;

    public UniversalSearchQueryHandler(IDbConnectionFactory db, IOptions<ResultExplanationOptions> options)
    {
        _db = db;
        _options = options.Value ?? new ResultExplanationOptions();
    }

    private sealed record SearchResultRow(
        Guid Id,
        string Type,
        Guid? RestaurantId,
        string Name,
        string? Description,
        string? Cuisine,
        bool IsOpenNow,
        bool IsAcceptingOrders,
        double? AvgRating,
        int ReviewCount,
        double Score,
        double? DistanceKm);

    public async Task<Result<UniversalSearchResponseDto>> Handle(UniversalSearchQuery request, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();

        var where = new List<string> { "s.\"SoftDeleted\" = FALSE" };
        var p = new DynamicParameters();

        // Always add parameters that are used in the scoring calculation
        p.Add("q", request.Term, System.Data.DbType.String);
        p.Add("lat", request.Latitude, System.Data.DbType.Double);
        p.Add("lon", request.Longitude, System.Data.DbType.Double);

        if (!string.IsNullOrWhiteSpace(request.Term))
        {
            var termLength = request.Term.Length;
            if (termLength <= 2)
            {
                // For very short queries, restrict to prefix match to avoid noisy substring matches
                // Make prefix check accent-insensitive via unaccent
                where.Add("unaccent(s.\"Name\") ILIKE unaccent(@prefix)");
                p.Add("prefix", request.Term + "%");
            }
            else
            {
                // Accent-insensitive FTS using unaccent on the query side.
                // Keep substring fallback unchanged to preserve trigram index usage.
                where.Add("(s.\"TsAll\" @@ websearch_to_tsquery('simple', unaccent(@q)) OR s.\"Name\" ILIKE '%' || @q || '%')");
            }
        }

        if (request.OpenNow is true)
        {
            where.Add("(s.\"IsOpenNow\" AND s.\"IsAcceptingOrders\")");
        }

        if (request.Cuisines is { Length: > 0 })
        {
            where.Add("s.\"Cuisine\" = ANY(@cuisines)");
            p.Add("cuisines", request.Cuisines);
        }

        if (request.Tags is { Length: > 0 })
        {
            where.Add("s.\"Tags\" && @tags");
            p.Add("tags", request.Tags);
        }

        if (request.PriceBands is { Length: > 0 })
        {
            where.Add("s.\"PriceBand\" = ANY(@priceBands)");
            p.Add("priceBands", request.PriceBands);
        }

        p.Add("pageNumber", request.PageNumber);
        p.Add("pageSize", request.PageSize);

        const string selectCols = """
            s."Id"                  AS Id,
            s."Type"                AS Type,
            s."RestaurantId"        AS RestaurantId,
            s."Name"                AS Name,
            s."Description"         AS Description,
            s."Cuisine"             AS Cuisine,
            s."IsOpenNow"           AS IsOpenNow,
            s."IsAcceptingOrders"   AS IsAcceptingOrders,
            s."AvgRating"           AS AvgRating,
            s."ReviewCount"         AS ReviewCount,
            (
              0.6 * (CASE WHEN @q IS NOT NULL THEN ts_rank_cd(s."TsAll", websearch_to_tsquery('simple', unaccent(@q))) ELSE 0 END)
              + 0.2 * (CASE WHEN @lat IS NOT NULL AND @lon IS NOT NULL AND s."Geo" IS NOT NULL
                     THEN 1.0 / (1.0 + (ST_Distance(s."Geo", ST_SetSRID(ST_MakePoint(@lon,@lat),4326)::geography) / 1000.0))
                     ELSE 0.5 END)
              + 0.2 * (CASE WHEN s."IsOpenNow" AND s."IsAcceptingOrders" THEN 1 ELSE 0 END)
            ) AS Score,
            CASE
              WHEN @lat IS NOT NULL AND @lon IS NOT NULL AND s."Geo" IS NOT NULL
                THEN ST_Distance(s."Geo", ST_SetSRID(ST_MakePoint(@lon,@lat),4326)::geography) / 1000.0
              ELSE NULL
            END AS DistanceKm
            """;

        var fromWhere = $"FROM \"SearchIndexItems\" s WHERE {string.Join(" AND ", where)}";
        var orderBy = "Score DESC, s.\"UpdatedAt\" DESC, s.\"Id\" ASC";

        var (countSql, pageSql) = DapperPagination.BuildPagedSql(selectCols, fromWhere, orderBy, request.PageNumber, request.PageSize);

        var page = await conn.QueryPageAsync<SearchResultRow>(
            countSql,
            pageSql,
            p,
            request.PageNumber,
            request.PageSize,
            ct);

        var mapped = page.Items.Select(r =>
        {
            var (badges, reason) = SearchResultExplainer.Explain(
                r.IsOpenNow,
                r.IsAcceptingOrders,
                r.AvgRating,
                r.ReviewCount,
                r.DistanceKm,
                _options);

            return new SearchResultDto(
                r.Id,
                r.Type,
                r.RestaurantId,
                r.Name,
                r.Description,
                r.Cuisine,
                r.Score,
                r.DistanceKm,
                badges,
                reason);
        }).ToList();

        var pageList = new PaginatedList<SearchResultDto>(mapped, page.TotalCount, page.PageNumber, request.PageSize);

        // Facets: compute only when requested
        FacetBlock facets;
        if (request.IncludeFacets)
        {
            // Default top-N for buckets
            p.Add("topN", 10);

            // Build the shared WHERE clause for facets
            var facetFromWhere = $"FROM \"SearchIndexItems\" s WHERE {string.Join(" AND ", where)}";

            var facetsSql = $@"
-- Cuisine facet
WITH base AS (
  SELECT s.* {facetFromWhere}
)
SELECT LOWER(c) AS Value, CAST(COUNT(*) AS int) AS Count
FROM base b
CROSS JOIN LATERAL NULLIF(b.""Cuisine"", '') c
GROUP BY LOWER(c)
ORDER BY COUNT(*) DESC, LOWER(c) ASC
LIMIT @topN;

-- Tags facet
WITH base AS (
  SELECT s.* {facetFromWhere}
)
SELECT LOWER(t) AS Value, CAST(COUNT(*) AS int) AS Count
FROM base b, LATERAL unnest(b.""Tags"") t
WHERE NULLIF(t, '') IS NOT NULL
GROUP BY LOWER(t)
ORDER BY COUNT(*) DESC, LOWER(t) ASC
LIMIT @topN;

-- Price bands facet
WITH base AS (
  SELECT s.* {facetFromWhere}
)
SELECT b.""PriceBand"" AS Value, CAST(COUNT(*) AS int) AS Count
FROM base b
WHERE b.""PriceBand"" IS NOT NULL
GROUP BY b.""PriceBand""
ORDER BY b.""PriceBand"" ASC;

-- Open-now count
WITH base AS (
  SELECT s.* {facetFromWhere}
)
SELECT CAST(COUNT(*) AS int) AS Count
FROM base b
WHERE b.""IsOpenNow"" AND b.""IsAcceptingOrders"";";

            using var grid = await conn.QueryMultipleAsync(new CommandDefinition(facetsSql, p, cancellationToken: ct));
            var cuisineBuckets = grid.Read<FacetCount<string>>().ToList();
            var tagBuckets = grid.Read<FacetCount<string>>().ToList();
            var priceBandBuckets = grid.Read<FacetCount<short>>().ToList();
            var openNowCount = grid.ReadSingle<int>();

            facets = new FacetBlock(cuisineBuckets, tagBuckets, priceBandBuckets, openNowCount);
        }
        else
        {
            facets = new FacetBlock(
                Array.Empty<FacetCount<string>>(),
                Array.Empty<FacetCount<string>>(),
                Array.Empty<FacetCount<short>>(),
                0);
        }

        return Result.Success(new UniversalSearchResponseDto(pageList, facets));
    }
}
