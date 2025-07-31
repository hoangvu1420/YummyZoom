using Dapper;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Domain.RoleAssignmentAggregate.Enums;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.RoleAssignments.Queries.GetRestaurantRoleAssignments;

public record GetRestaurantRoleAssignmentsQuery(Guid RestaurantId) : IRequest<Result<GetRestaurantRoleAssignmentsResponse>>;

public record GetRestaurantRoleAssignmentsResponse(List<RoleAssignmentDto> RoleAssignments);

public record RoleAssignmentDto(
    Guid Id,
    Guid UserId,
    Guid RestaurantId,
    RestaurantRole Role,
    DateTime Created, 
    string? CreatedBy);

public class GetRestaurantRoleAssignmentsQueryHandler : IRequestHandler<GetRestaurantRoleAssignmentsQuery, Result<GetRestaurantRoleAssignmentsResponse>>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public GetRestaurantRoleAssignmentsQueryHandler(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<Result<GetRestaurantRoleAssignmentsResponse>> Handle(GetRestaurantRoleAssignmentsQuery request, CancellationToken cancellationToken)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        const string sql = """
            SELECT
                ra."Id",
                ra."UserId",
                ra."RestaurantId",
                ra."Role",
                ra."Created",
                ra."CreatedBy"
            FROM "RoleAssignments" AS ra
            WHERE ra."RestaurantId" = @RestaurantId
            """;

        var roleAssignments = await connection.QueryAsync<RoleAssignmentDto>(
            new CommandDefinition(sql, new { request.RestaurantId }, cancellationToken: cancellationToken));

        return Result.Success(new GetRestaurantRoleAssignmentsResponse(roleAssignments.AsList()));
    }
}
