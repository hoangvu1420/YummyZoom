using Dapper;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.CustomizationGroups.Queries.GetCustomizationGroups;

public sealed class GetCustomizationGroupsQueryHandler : IRequestHandler<GetCustomizationGroupsQuery, Result<List<CustomizationGroupSummaryDto>>>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public GetCustomizationGroupsQueryHandler(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<Result<List<CustomizationGroupSummaryDto>>> Handle(GetCustomizationGroupsQuery request, CancellationToken cancellationToken)
    {
        using var conn = _dbConnectionFactory.CreateConnection();

        const string sql = """
            SELECT 
                cg."Id",
                cg."GroupName" AS Name,
                cg."MinSelections",
                cg."MaxSelections",
                (SELECT COUNT(*) FROM "CustomizationChoices" cc WHERE cc."CustomizationGroupId" = cg."Id") AS ChoiceCount
            FROM "CustomizationGroups" cg
            WHERE cg."RestaurantId" = @RestaurantId AND cg."IsDeleted" = false
            ORDER BY cg."GroupName" ASC
            """;

        var result = await conn.QueryAsync<CustomizationGroupSummaryDto>(
            new CommandDefinition(sql, new { request.RestaurantId }, cancellationToken: cancellationToken));

        return Result.Success(result.ToList());
    }
}
