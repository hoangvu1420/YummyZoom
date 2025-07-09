using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.RoleAssignmentAggregate.ValueObjects;
using YummyZoom.Domain.RoleAssignmentAggregate.Errors;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.RoleAssignments.Commands.DeleteRoleAssignment;

[Authorize(Roles = Roles.Administrator)]
public record DeleteRoleAssignmentCommand(
    Guid RoleAssignmentId) : IRequest<Result<Unit>>;

public class DeleteRoleAssignmentCommandHandler : IRequestHandler<DeleteRoleAssignmentCommand, Result<Unit>>
{
    private readonly IRoleAssignmentRepository _roleAssignmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteRoleAssignmentCommandHandler(
        IRoleAssignmentRepository roleAssignmentRepository,
        IUnitOfWork unitOfWork)
    {
        _roleAssignmentRepository = roleAssignmentRepository;
        _unitOfWork = unitOfWork;
    }

    public Task<Result<Unit>> Handle(DeleteRoleAssignmentCommand request, CancellationToken cancellationToken)
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

            // 2) Delete the role assignment
            await _roleAssignmentRepository.DeleteAsync(roleAssignment, cancellationToken);

            return Result.Success(Unit.Value);
        }, cancellationToken);
    }
}
