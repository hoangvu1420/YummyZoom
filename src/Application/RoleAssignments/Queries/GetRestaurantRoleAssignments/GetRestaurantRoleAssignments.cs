using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.RoleAssignmentAggregate.ValueObjects;
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
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public class GetRestaurantRoleAssignmentsQueryHandler : IRequestHandler<GetRestaurantRoleAssignmentsQuery, Result<GetRestaurantRoleAssignmentsResponse>>
{
    private readonly IRoleAssignmentRepository _roleAssignmentRepository;

    public GetRestaurantRoleAssignmentsQueryHandler(IRoleAssignmentRepository roleAssignmentRepository)
    {
        _roleAssignmentRepository = roleAssignmentRepository;
    }

    public async Task<Result<GetRestaurantRoleAssignmentsResponse>> Handle(GetRestaurantRoleAssignmentsQuery request, CancellationToken cancellationToken)
    {
        var restaurantId = RestaurantId.Create(request.RestaurantId);
        var roleAssignments = await _roleAssignmentRepository.GetByRestaurantIdAsync(restaurantId, cancellationToken);

        var roleAssignmentDtos = roleAssignments.Select(ra => new RoleAssignmentDto(
            ra.Id.Value,
            ra.UserId.Value,
            ra.RestaurantId.Value,
            ra.Role,
            ra.CreatedAt,
            ra.UpdatedAt)).ToList();

        return Result.Success(new GetRestaurantRoleAssignmentsResponse(roleAssignmentDtos));
    }
}
