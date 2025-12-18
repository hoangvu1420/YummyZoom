using Dapper;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.CustomizationGroups.Queries.GetCustomizationGroupDetails;

public sealed class GetCustomizationGroupDetailsQueryHandler : IRequestHandler<GetCustomizationGroupDetailsQuery, Result<CustomizationGroupDetailsDto>>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public GetCustomizationGroupDetailsQueryHandler(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<Result<CustomizationGroupDetailsDto>> Handle(GetCustomizationGroupDetailsQuery request, CancellationToken cancellationToken)
    {
        using var conn = _dbConnectionFactory.CreateConnection();

        const string sql = """
            SELECT 
                cg."Id",
                cg."GroupName" AS Name,
                cg."MinSelections",
                cg."MaxSelections",
                cg."RestaurantId"
            FROM "CustomizationGroups" cg
            WHERE cg."Id" = @GroupId AND cg."IsDeleted" = false;

            SELECT 
                cc."ChoiceId" AS Id,
                cc."Name",
                cc."PriceAdjustment_Amount" AS PriceAmount,
                cc."PriceAdjustment_Currency" AS PriceCurrency,
                cc."IsDefault",
                cc."DisplayOrder"
            FROM "CustomizationChoices" cc
            WHERE cc."CustomizationGroupId" = @GroupId
            ORDER BY cc."DisplayOrder" ASC, cc."Name" ASC;
            """;

        using var multi = await conn.QueryMultipleAsync(new CommandDefinition(sql, new { request.GroupId }, cancellationToken: cancellationToken));

        var groupRow = await multi.ReadSingleOrDefaultAsync<GroupRow>();
        if (groupRow is null)
        {
            return Result.Failure<CustomizationGroupDetailsDto>(CustomizationGroupErrors.NotFound);
        }

        if (groupRow.RestaurantId != request.RestaurantId)
        {
            return Result.Failure<CustomizationGroupDetailsDto>(CustomizationGroupErrors.NotFound);
        }

        var choices = (await multi.ReadAsync<CustomizationChoiceDetailsDto>()).ToList();

        return Result.Success(new CustomizationGroupDetailsDto(
            groupRow.Id,
            groupRow.Name,
            groupRow.MinSelections,
            groupRow.MaxSelections,
            choices));
    }

    private sealed record GroupRow(Guid Id, string Name, int MinSelections, int MaxSelections, Guid RestaurantId);
}
