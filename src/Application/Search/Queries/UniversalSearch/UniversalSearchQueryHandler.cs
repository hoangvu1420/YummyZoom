using Dapper;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Orders.Queries.Common;
using YummyZoom.SharedKernel;
using Result = YummyZoom.SharedKernel.Result;

namespace YummyZoom.Application.Search.Queries.UniversalSearch;

// Request + DTO
public sealed record UniversalSearchQuery(
    string? Term,
    double? Latitude,
    double? Longitude,
    bool? OpenNow,
    string[]? Cuisines,
    int PageNumber,
    int PageSize
) : IRequest<Result<PaginatedList<SearchResultDto>>>;

public sealed record SearchResultDto(
    Guid Id,
    string Type,
    Guid? RestaurantId,
    string Name,
    string? DescriptionSnippet,
    string? Cuisine,
    double Score,
    double? DistanceKm
);

// Validator
public sealed class UniversalSearchQueryValidator : AbstractValidator<UniversalSearchQuery>
{
    public UniversalSearchQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Term).MaximumLength(256);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue);
        RuleFor(x => x.Cuisines).Must(c => c!.All(s => s.Length <= 100)).When(x => x.Cuisines is { Length: > 0 });
    }
}

// Handler
public sealed class UniversalSearchQueryHandler
    : IRequestHandler<UniversalSearchQuery, Result<PaginatedList<SearchResultDto>>>
{
    private readonly IDbConnectionFactory _db;

    public UniversalSearchQueryHandler(IDbConnectionFactory db) => _db = db;

    private sealed record SearchResultRow(
        Guid Id,
        string Type,
        Guid? RestaurantId,
        string Name,
        string? Description,
        string? Cuisine,
        double Score,
        double? DistanceKm);

    public async Task<Result<PaginatedList<SearchResultDto>>> Handle(UniversalSearchQuery request, CancellationToken ct)
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
                where.Add("s.\"Name\" ILIKE @prefix");
                p.Add("prefix", request.Term + "%");
            }
            else
            {
                where.Add("(s.\"TsAll\" @@ websearch_to_tsquery('simple', @q) OR s.\"Name\" ILIKE '%' || @q || '%')");
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

        p.Add("pageNumber", request.PageNumber);
        p.Add("pageSize", request.PageSize);

        const string selectCols = """
            s."Id"          AS Id,
            s."Type"        AS Type,
            s."RestaurantId" AS RestaurantId,
            s."Name"        AS Name,
            s."Description" AS Description,
            s."Cuisine"     AS Cuisine,
            (
              0.6 * (CASE WHEN @q IS NOT NULL THEN ts_rank_cd(s."TsAll", websearch_to_tsquery('simple', @q)) ELSE 0 END)
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
            new SearchResultDto(r.Id, r.Type, r.RestaurantId, r.Name, r.Description, r.Cuisine, r.Score, r.DistanceKm)
        ).ToList();

        return Result.Success(new PaginatedList<SearchResultDto>(mapped, page.TotalCount, page.PageNumber, request.PageSize));
    }
}

