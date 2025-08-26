using Dapper;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Restaurants.Queries.Common;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Restaurants.Queries.GetFullMenu;

public sealed class GetFullMenuQueryHandler : IRequestHandler<GetFullMenuQuery, Result<GetFullMenuResponse>>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public GetFullMenuQueryHandler(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<Result<GetFullMenuResponse>> Handle(GetFullMenuQuery request, CancellationToken cancellationToken)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        const string sql = """
            SELECT
                "MenuJson"       AS MenuJson,
                "LastRebuiltAt"  AS LastRebuiltAt
            FROM "FullMenuViews"
            WHERE "RestaurantId" = @RestaurantId
            """;

        var row = await connection.QuerySingleOrDefaultAsync<FullMenuViewRow>(
            new CommandDefinition(sql, new { request.RestaurantId }, cancellationToken: cancellationToken));

        if (row is null)
        {
            return Result.Failure<GetFullMenuResponse>(GetFullMenuErrors.NotFound);
        }

        // Dapper maps timestamptz to DateTime (Kind=Utc); convert to DateTimeOffset preserving instant
        var rebuiltAt = row.LastRebuiltAt.Kind == DateTimeKind.Unspecified
            ? new DateTimeOffset(DateTime.SpecifyKind(row.LastRebuiltAt, DateTimeKind.Utc))
            : new DateTimeOffset(row.LastRebuiltAt);
        return Result.Success(new GetFullMenuResponse(row.MenuJson, rebuiltAt));
    }
}
