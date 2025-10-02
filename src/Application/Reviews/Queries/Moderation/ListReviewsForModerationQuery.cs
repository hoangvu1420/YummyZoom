using System.Data;
using Dapper;
using YummyZoom.Application.Common.Caching;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Models;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Reviews.Queries.Moderation;

public enum AdminReviewListSort
{
    Newest = 0,
    HighestRating = 1,
    LowestRating = 2
}

public sealed record ListReviewsForModerationQuery(
    int PageNumber = 1,
    int PageSize = 25,
    bool? IsModerated = null,
    bool? IsHidden = null,
    int? MinRating = null,
    int? MaxRating = null,
    bool? HasTextOnly = null,
    Guid? RestaurantId = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    string? Search = null,
    AdminReviewListSort SortBy = AdminReviewListSort.Newest

) : IRequest<Result<PaginatedList<AdminModerationReviewDto>>>, ICacheableQuery<Result<PaginatedList<AdminModerationReviewDto>>>
{
    public string CacheKey
    {
        get
        {
            var normalizedSearch = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();
            var key = $"cache:admin:review-moderation:list:v1:pn={PageNumber}:ps={PageSize}:mod={IsModerated}:hid={IsHidden}:min={MinRating}:max={MaxRating}:txt={HasTextOnly}:rest={(RestaurantId?.ToString("N") ?? "null")}:from={(FromUtc?.ToString("O") ?? "null")}:to={(ToUtc?.ToString("O") ?? "null")}:q={(normalizedSearch ?? "null")}:sort={(int)SortBy}";
            return key;
        }
    }

    public CachePolicy Policy => CachePolicy.WithTtl(TimeSpan.FromSeconds(30), "cache:admin:review-moderation:list");
}

public sealed class ListReviewsForModerationQueryHandler : IRequestHandler<ListReviewsForModerationQuery, Result<PaginatedList<AdminModerationReviewDto>>>
{
    private readonly IDbConnectionFactory _db;

    public ListReviewsForModerationQueryHandler(IDbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<Result<PaginatedList<AdminModerationReviewDto>>> Handle(ListReviewsForModerationQuery request, CancellationToken cancellationToken)
    {
        using var connection = _db.CreateConnection();

        var where = new List<string>();
        var parameters = new DynamicParameters();

        if (request.IsModerated is not null)
        {
            where.Add(@"r.""IsModerated"" = @IsModerated");
            parameters.Add("IsModerated", request.IsModerated);
        }

        if (request.IsHidden is not null)
        {
            where.Add(@"r.""IsHidden"" = @IsHidden");
            parameters.Add("IsHidden", request.IsHidden);
        }

        if (request.MinRating is not null)
        {
            where.Add(@"r.""Rating"" >= @MinRating");
            parameters.Add("MinRating", request.MinRating);
        }

        if (request.MaxRating is not null)
        {
            where.Add(@"r.""Rating"" <= @MaxRating");
            parameters.Add("MaxRating", request.MaxRating);
        }

        if (request.HasTextOnly == true)
        {
            where.Add(@"coalesce(nullif(trim(r.""Comment""), ''), null) is not null");
        }

        if (request.RestaurantId is not null)
        {
            where.Add(@"r.""RestaurantId"" = @RestaurantId");
            parameters.Add("RestaurantId", request.RestaurantId);
        }

        if (request.FromUtc is not null)
        {
            where.Add(@"r.""SubmissionTimestamp"" >= @FromUtc");
            parameters.Add("FromUtc", request.FromUtc);
        }

        if (request.ToUtc is not null)
        {
            where.Add(@"r.""SubmissionTimestamp"" <= @ToUtc");
            parameters.Add("ToUtc", request.ToUtc);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            where.Add(@"(r.""Comment"" ILIKE @Search OR rest.""Name"" ILIKE @Search)");
            parameters.Add("Search", $"%{request.Search.Trim()}%");
        }

        var whereClause = where.Count > 0 ? $"WHERE {string.Join(" AND ", where)}" : string.Empty;

        var orderBy = request.SortBy switch
        {
            AdminReviewListSort.HighestRating => "r.\"Rating\" DESC, r.\"SubmissionTimestamp\" DESC, r.\"Id\" DESC",
            AdminReviewListSort.LowestRating => "r.\"Rating\" ASC, r.\"SubmissionTimestamp\" DESC, r.\"Id\" DESC",
            _ => "r.\"SubmissionTimestamp\" DESC, r.\"Id\" DESC"
        };

        var offset = (request.PageNumber - 1) * request.PageSize;

        var sql = $"""
SELECT r."Id" AS ReviewId,
       r."RestaurantId" AS RestaurantId,
       rest."Name" AS RestaurantName,
       r."CustomerId" AS CustomerId,
       r."Rating" AS Rating,
       r."Comment" AS Comment,
       r."SubmissionTimestamp" AS SubmissionTimestamp,
       r."IsModerated" AS IsModerated,
       r."IsHidden" AS IsHidden,
       NULL::timestamp AS LastActionAtUtc
FROM "Reviews" r
JOIN "Restaurants" rest ON rest."Id" = r."RestaurantId"
{whereClause}
ORDER BY {orderBy}
LIMIT @Limit OFFSET @Offset;
""";

        var countSql = $"""SELECT COUNT(1) FROM "Reviews" r JOIN "Restaurants" rest ON rest."Id" = r."RestaurantId" {whereClause};""";

        parameters.Add("Limit", request.PageSize);
        parameters.Add("Offset", offset);

        var items = await connection.QueryAsync<AdminModerationReviewDto>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
        var total = await connection.ExecuteScalarAsync<int>(new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));

        var page = PaginatedList<AdminModerationReviewDto>.Create(items.ToList(), total, request.PageNumber, request.PageSize);
        return YummyZoom.SharedKernel.Result.Success(page);
    }
}
