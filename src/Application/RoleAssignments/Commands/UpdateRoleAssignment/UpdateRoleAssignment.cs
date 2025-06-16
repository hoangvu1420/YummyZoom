using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Domain.RoleAssignmentAggregate.ValueObjects;
using YummyZoom.Domain.RoleAssignmentAggregate.Enums;
using YummyZoom.Domain.RoleAssignmentAggregate.Errors;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;
using Microsoft.AspNetCore.Authorization;

namespace YummyZoom.Application.RoleAssignments.Commands.UpdateRoleAssignment;

[Authorize(Roles = Roles.Administrator)]
public record UpdateRoleAssignmentCommand(
    Guid RoleAssignmentId,
    RestaurantRole NewRole) : IRequest<Result<Unit>>;

public class UpdateRoleAssignmentCommandHandler : IRequestHandler<UpdateRoleAssignmentCommand, Result<Unit>>
{
    private readonly IRoleAssignmentRepository _roleAssignmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateRoleAssignmentCommandHandler(
        IRoleAssignmentRepository roleAssignmentRepository,
        IUnitOfWork unitOfWork)
    {
        _roleAssignmentRepository = roleAssignmentRepository;
        _unitOfWork = unitOfWork;
    }

    public Task<Result<Unit>> Handle(UpdateRoleAssignmentCommand request, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // 1) Get existing role assignment
            var roleAssignmentId = RoleAssignmentId.Create(request.RoleAssignmentId);
            var roleAssignment = await _roleAssignmentRepository.GetByIdAsync(roleAssignmentId, cancellationToken);

            if (roleAssignment is null)
            {
                return Result.Failure<Unit>(RoleAssignmentErrors.RoleAssignmentNotFound(request.RoleAssignmentId));
            }

            // 2) Update the role
            var updateResult = roleAssignment.UpdateRole(request.NewRole);
            if (updateResult.IsFailure)
            {
                return Result.Failure<Unit>(updateResult.Error);
            }

            // 3) Persist the changes
            await _roleAssignmentRepository.UpdateAsync(roleAssignment, cancellationToken);

            return Result.Success(Unit.Value);
        }, cancellationToken);
    }
}
