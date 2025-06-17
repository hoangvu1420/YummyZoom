using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.RoleAssignmentAggregate;
using YummyZoom.Domain.RoleAssignmentAggregate.ValueObjects;
using YummyZoom.Domain.RoleAssignmentAggregate.Enums;
using YummyZoom.Domain.RoleAssignmentAggregate.Errors;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.RoleAssignments.Commands.CreateRoleAssignment;

[Authorize(Roles = Roles.Administrator)]
public record CreateRoleAssignmentCommand(
    Guid UserId,
    Guid RestaurantId,
    RestaurantRole Role) : IRequest<Result<CreateRoleAssignmentResponse>>;

public record CreateRoleAssignmentResponse(Guid RoleAssignmentId);

public class CreateRoleAssignmentCommandHandler : IRequestHandler<CreateRoleAssignmentCommand, Result<CreateRoleAssignmentResponse>>
{
    private readonly IRoleAssignmentRepository _roleAssignmentRepository;
    private readonly IIdentityService _identityService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateRoleAssignmentCommandHandler(
        IRoleAssignmentRepository roleAssignmentRepository,
        IIdentityService identityService,
        IUnitOfWork unitOfWork)
    {
        _roleAssignmentRepository = roleAssignmentRepository;
        _identityService = identityService;
        _unitOfWork = unitOfWork;
    }

    public Task<Result<CreateRoleAssignmentResponse>> Handle(CreateRoleAssignmentCommand request, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // 1) Validate user exists
            var userId = UserId.Create(request.UserId);
            var userExists = await _identityService.UserExistsAsync(request.UserId);
            if (!userExists)
            {
                return Result.Failure<CreateRoleAssignmentResponse>(RoleAssignmentErrors.UserNotFound(request.UserId));
            }

            // 2) Validate no duplicate role assignment exists
            var restaurantId = RestaurantId.Create(request.RestaurantId);
            var existingAssignment = await _roleAssignmentRepository.GetByUserRestaurantRoleAsync(
                userId, restaurantId, request.Role, cancellationToken);

            if (existingAssignment is not null)
            {
                return Result.Failure<CreateRoleAssignmentResponse>(
                    RoleAssignmentErrors.DuplicateRoleAssignment(request.UserId, request.RestaurantId, request.Role.ToString()));
            }

            // 3) Create new role assignment
            var roleAssignmentResult = RoleAssignment.Create(userId, restaurantId, request.Role);
            if (roleAssignmentResult.IsFailure)
            {
                return Result.Failure<CreateRoleAssignmentResponse>(roleAssignmentResult.Error);
            }

            // 4) Persist the role assignment
            await _roleAssignmentRepository.AddAsync(roleAssignmentResult.Value, cancellationToken);

            return Result.Success(new CreateRoleAssignmentResponse(roleAssignmentResult.Value.Id.Value));
        }, cancellationToken);
    }
}
