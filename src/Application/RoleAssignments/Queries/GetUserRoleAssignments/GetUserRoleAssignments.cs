using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.UserAggregate.ValueObjects;
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

public class GetUserRoleAssignmentsQueryHandler : IRequestHandler<GetUserRoleAssignmentsQuery, Result<GetUserRoleAssignmentsResponse>>
{
    private readonly IRoleAssignmentRepository _roleAssignmentRepository;

    public GetUserRoleAssignmentsQueryHandler(IRoleAssignmentRepository roleAssignmentRepository)
    {
        _roleAssignmentRepository = roleAssignmentRepository;
    }

    public async Task<Result<GetUserRoleAssignmentsResponse>> Handle(GetUserRoleAssignmentsQuery request, CancellationToken cancellationToken)
    {
        var userId = UserId.Create(request.UserId);
        var roleAssignments = await _roleAssignmentRepository.GetByUserIdAsync(userId, cancellationToken);

        var roleAssignmentDtos = roleAssignments.Select(ra => new RoleAssignmentDto(
            ra.Id.Value,
            ra.UserId.Value,
            ra.RestaurantId.Value,
            ra.Role,
            ra.CreatedAt,
            ra.UpdatedAt)).ToList();

        return Result.Success(new GetUserRoleAssignmentsResponse(roleAssignmentDtos));
    }
}
