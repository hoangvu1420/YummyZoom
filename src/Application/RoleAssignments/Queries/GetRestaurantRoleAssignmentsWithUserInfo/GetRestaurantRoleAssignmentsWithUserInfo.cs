using Dapper;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Domain.RoleAssignmentAggregate.Enums;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.RoleAssignments.Queries.GetRestaurantRoleAssignmentsWithUserInfo;

public record GetRestaurantRoleAssignmentsWithUserInfoQuery(Guid RestaurantId) : IRequest<Result<GetRestaurantRoleAssignmentsWithUserInfoResponse>>;

public record GetRestaurantRoleAssignmentsWithUserInfoResponse(List<RoleAssignmentWithUserInfoDto> RoleAssignments);

/// <summary>
/// DTO that demonstrates the power of Dapper queries - combining data from multiple aggregates
/// in a single optimized query without loading full domain objects.
/// </summary>
public record RoleAssignmentWithUserInfoDto(
    Guid Id,
    Guid UserId,
    string UserEmail,
    string? UserFirstName,
    string? UserLastName,
    Guid RestaurantId,
    RestaurantRole Role,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

/// <summary>
/// Example of a more complex query that demonstrates the true power of the Dapper + SQL approach.
/// This query joins across multiple aggregates (RoleAssignment and User) to create a rich DTO
/// that would be very difficult to achieve efficiently with repository patterns.
/// 
/// This showcases how queries can be optimized for specific read scenarios while commands
/// continue to use repositories for proper aggregate handling.
/// </summary>
public class GetRestaurantRoleAssignmentsWithUserInfoQueryHandler : IRequestHandler<GetRestaurantRoleAssignmentsWithUserInfoQuery, Result<GetRestaurantRoleAssignmentsWithUserInfoResponse>>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public GetRestaurantRoleAssignmentsWithUserInfoQueryHandler(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<Result<GetRestaurantRoleAssignmentsWithUserInfoResponse>> Handle(GetRestaurantRoleAssignmentsWithUserInfoQuery request, CancellationToken cancellationToken)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        // Complex query that joins RoleAssignments with AspNetUsers table
        // This demonstrates the flexibility of direct SQL for read operations
        const string sql = """
                           SELECT
                               ra."Id",
                               ra."UserId",
                               u."Email" as UserEmail,
                               u."FirstName" as UserFirstName,
                               u."LastName" as UserLastName,
                               ra."RestaurantId",
                               ra."Role",
                               ra."CreatedAt",
                               ra."UpdatedAt"
                           FROM "RoleAssignments" AS ra
                           INNER JOIN "AspNetUsers" AS u ON ra."UserId"::text = u."Id"
                           WHERE ra."RestaurantId" = @RestaurantId
                           ORDER BY ra."CreatedAt" DESC
                           """;

        var roleAssignments = await connection.QueryAsync<RoleAssignmentWithUserInfoDto>(
            new CommandDefinition(sql, new { request.RestaurantId }, cancellationToken: cancellationToken));

        return Result.Success(new GetRestaurantRoleAssignmentsWithUserInfoResponse(roleAssignments.AsList()));
    }
}
