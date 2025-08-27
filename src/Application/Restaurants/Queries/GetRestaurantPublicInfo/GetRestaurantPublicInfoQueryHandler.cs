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
                r."Id"                  AS RestaurantId,
                r."Name"                AS Name,
                r."LogoUrl"             AS LogoUrl,
                '[]'::text              AS CuisineTagsJson,
                r."IsAcceptingOrders"   AS IsAcceptingOrders,
                r."Location_City"       AS City
            FROM "Restaurants" r
            WHERE r."Id" = @RestaurantId AND r."IsDeleted" = false
            """;

        // TODO: retrieve CuisineTags from a model in the future.

        var row = await connection.QuerySingleOrDefaultAsync<RestaurantPublicInfoRow>(
            new CommandDefinition(sql, new { request.RestaurantId }, cancellationToken: cancellationToken));

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

        var dto = new RestaurantPublicInfoDto(
            row.RestaurantId,
            row.Name,
            row.LogoUrl,
            cuisine,
            row.IsAcceptingOrders,
            row.City);

        return Result.Success(dto);
    }
}

file sealed class RestaurantPublicInfoRow
{
    public Guid RestaurantId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? LogoUrl { get; init; }
    public string? CuisineTagsJson { get; init; }
    public bool IsAcceptingOrders { get; init; }
    public string? City { get; init; }
}
