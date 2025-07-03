using Dapper;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Domain.RoleAssignmentAggregate.Enums;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.RoleAssignments.Queries.GetUserRoleAssignments;

public record GetUserRoleAssignmentsQuery(Guid UserId) : IRequest<Result<GetUserRoleAssignmentsResponse>>;

public record GetUserRoleAssignmentsResponse(List<RoleAssignmentDto> RoleAssignments);

public record RoleAssignmentDto(
    Guid Id,
    Guid UserId,
    Guid RestaurantId,
    RestaurantRole Role,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

/// <summary>
/// Query handler that uses the new CQRS pattern with IDbConnectionFactory + Dapper.
/// This demonstrates the consistent application of the pattern across different query types.
/// </summary>
public class GetUserRoleAssignmentsQueryHandler : IRequestHandler<GetUserRoleAssignmentsQuery, Result<GetUserRoleAssignmentsResponse>>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public GetUserRoleAssignmentsQueryHandler(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<Result<GetUserRoleAssignmentsResponse>> Handle(GetUserRoleAssignmentsQuery request, CancellationToken cancellationToken)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        // Direct SQL query for user-specific role assignments
        const string sql = """
                           SELECT
                               ra."Id",
                               ra."UserId",
                               ra."RestaurantId",
                               ra."Role",
                               ra."CreatedAt",
                               ra."UpdatedAt"
                           FROM "RoleAssignments" AS ra
                           WHERE ra."UserId" = @UserId
                           """;

        var roleAssignments = await connection.QueryAsync<RoleAssignmentDto>(
            new CommandDefinition(sql, new { request.UserId }, cancellationToken: cancellationToken));

        return Result.Success(new GetUserRoleAssignmentsResponse(roleAssignments.AsList()));
    }
}
