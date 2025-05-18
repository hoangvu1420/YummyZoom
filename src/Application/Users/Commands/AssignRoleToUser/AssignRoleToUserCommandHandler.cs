using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Domain.UserAggregate.Errors;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Users.Commands.AssignRoleToUser;

public class AssignRoleToUserCommandHandler : IRequestHandler<AssignRoleToUserCommand, Result>
{
    private readonly IUserAggregateRepository _userAggregateRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AssignRoleToUserCommandHandler(
        IUserAggregateRepository userAggregateRepository,
        IUnitOfWork unitOfWork)
    {
        _userAggregateRepository = userAggregateRepository;
        _unitOfWork = unitOfWork;
    }

    public Task<Result> Handle(AssignRoleToUserCommand request, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var userId = UserId.Create(request.UserId);

            var userAggregate = await _userAggregateRepository.GetByIdAsync(userId, cancellationToken);
            if (userAggregate is null)
            {
                return Result.Failure(UserErrors.UserNotFound(request.UserId));
            }

            var roleAssignmentResult = RoleAssignment.Create(
                request.RoleName,
                request.TargetEntityId,
                request.TargetEntityType);

            if (roleAssignmentResult.IsFailure)
            {
                return Result.Failure(roleAssignmentResult.Error);
            }

            // Check if the role already exists in the domain aggregate
            if (userAggregate.UserRoles.Contains(roleAssignmentResult.Value))
            {
                // Role already assigned, consider this a success (no-op)
                return Result.Success();
            }

            var addRoleResult = userAggregate.AddRole(roleAssignmentResult.Value);
            if (addRoleResult.IsFailure)
            {
                return Result.Failure(addRoleResult.Error);
            }

            await _userAggregateRepository.UpdateAsync(userAggregate, cancellationToken);

            return Result.Success();
        }, cancellationToken);
    }
}
