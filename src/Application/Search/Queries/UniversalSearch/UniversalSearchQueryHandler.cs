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
    string[]? EntityTypes = null,
    string? Bbox = null,
    string? Sort = null,
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
        : this(term, latitude, longitude, openNow, cuisines, null, null, null, null, null, false, pageNumber, pageSize)
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

public sealed record SearchBadgeDto(
    string Code,
    string Label,
    object? Data = null
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

        // Entity types filter (restaurant|menu_item|tag)
        string[]? types = null;
        if (request.EntityTypes is { Length: > 0 })
        {
            types = request.EntityTypes
                .Select(t => t?.Trim().ToLowerInvariant().Replace("-", "_")!)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct()
                .ToArray();
            if (types.Length == 0) types = null;
        }
        // Defer adding @types until we decide to include the predicate; avoids untyped NULLs

        if (!string.IsNullOrWhiteSpace(request.Term))
        {
            var termLength = request.Term.Length;
            if (termLength <= 2)
            {
                // For very short queries include prefix matching on the name plus a light-weight
                // full-text lookup so that accented cuisines/tags still match.
                where.Add("(unaccent(s.\"Name\") ILIKE unaccent(@prefix) OR s.\"TsAll\" @@ plainto_tsquery('simple', unaccent(@q)))");
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
            where.Add("s.\"Cuisine\" = ANY(@cuisines::text[])");
            p.Add("cuisines", request.Cuisines);
        }

        if (request.Tags is { Length: > 0 })
        {
            where.Add("s.\"Tags\" && @tags::text[]");
            p.Add("tags", request.Tags);
        }

        if (request.PriceBands is { Length: > 0 })
        {
            where.Add("s.\"PriceBand\" = ANY(@priceBands::smallint[])");
            p.Add("priceBands", request.PriceBands);
        }

        p.Add("pageNumber", request.PageNumber);
        p.Add("pageSize", request.PageSize);

        // BBox parsing and validation (minLon,minLat,maxLon,maxLat)
        double? minLon = null, minLat = null, maxLon = null, maxLat = null;
        if (!string.IsNullOrWhiteSpace(request.Bbox))
        {
            var parts = request.Bbox.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 4
                && double.TryParse(parts[0], out var a)
                && double.TryParse(parts[1], out var b)
                && double.TryParse(parts[2], out var c)
                && double.TryParse(parts[3], out var d))
            {
                (minLon, minLat, maxLon, maxLat) = (a, b, c, d);
                if (minLon < -180 || maxLon > 180 || minLat < -90 || maxLat > 90 || minLon >= maxLon || minLat >= maxLat)
                {
                    minLon = minLat = maxLon = maxLat = null; // ignore invalid
                }
            }
        }
        // Only add bbox filter and parameters when valid; avoid untyped NULL parameters
        if (minLon.HasValue && minLat.HasValue && maxLon.HasValue && maxLat.HasValue)
        {
            where.Add("(s.\"Geo\" IS NOT NULL AND ST_Intersects(s.\"Geo\", ST_MakeEnvelope(@minLon, @minLat, @maxLon, @maxLat, 4326)::geography))");
            p.Add("minLon", minLon, System.Data.DbType.Double);
            p.Add("minLat", minLat, System.Data.DbType.Double);
            p.Add("maxLon", maxLon, System.Data.DbType.Double);
            p.Add("maxLat", maxLat, System.Data.DbType.Double);
        }

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

        // Type filter (only when provided)
        if (types is not null)
        {
            where.Add("s.\"Type\" = ANY(@types::text[])");
            p.Add("types", types);
        }

        // BBox filter added above only when a valid bbox was provided

        var fromWhere = $"FROM \"SearchIndexItems\" s WHERE {string.Join(" AND ", where)}";

        // Sorting
        string orderBy;
        var sort = (request.Sort ?? "relevance").Trim().ToLowerInvariant();
        if (sort == "distance" && request.Latitude.HasValue && request.Longitude.HasValue)
        {
            orderBy = "DistanceKm ASC NULLS LAST, Score DESC, s.\"UpdatedAt\" DESC, s.\"Id\" ASC";
        }
        else if (sort == "rating")
        {
            orderBy = "COALESCE(s.\"AvgRating\",0) DESC NULLS LAST, COALESCE(s.\"ReviewCount\",0) DESC, s.\"UpdatedAt\" DESC, s.\"Id\" ASC";
        }
        else if (sort == "priceband")
        {
            orderBy = "COALESCE(s.\"PriceBand\", 100) ASC, COALESCE(s.\"AvgRating\",0) DESC, s.\"UpdatedAt\" DESC, s.\"Id\" ASC";
        }
        else if (sort == "popularity")
        {
            orderBy = "COALESCE(s.\"ReviewCount\",0) DESC, COALESCE(s.\"AvgRating\",0) DESC, s.\"UpdatedAt\" DESC, s.\"Id\" ASC";
        }
        else
        {
            orderBy = "Score DESC, s.\"UpdatedAt\" DESC, s.\"Id\" ASC"; // relevance (default)
        }

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
