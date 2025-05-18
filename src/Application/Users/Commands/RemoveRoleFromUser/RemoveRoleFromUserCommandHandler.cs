using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Users.Commands.RemoveRoleFromUser;

public class RemoveRoleFromUserCommandHandler : IRequestHandler<RemoveRoleFromUserCommand, Result>
{
    private readonly IUserAggregateRepository _userAggregateRepository;
    private readonly IIdentityService _identityService;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveRoleFromUserCommandHandler(
        IUserAggregateRepository userAggregateRepository,
        IIdentityService identityService,
        IUnitOfWork unitOfWork)
    {
        _userAggregateRepository = userAggregateRepository;
        _identityService = identityService;
        _unitOfWork = unitOfWork;
    }

    public Task<Result> Handle(RemoveRoleFromUserCommand request, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var userId = UserId.Create(request.UserId);

            var userAggregate = await _userAggregateRepository.GetByIdAsync(userId, cancellationToken);
            if (userAggregate is null)
            {
                return Result.Failure(Domain.UserAggregate.Errors.UserErrors.UserNotFound(request.UserId));
            }

            var removeRoleResult = userAggregate.RemoveRole(
                request.RoleName,
                request.TargetEntityId,
                request.TargetEntityType);

            if (removeRoleResult.IsFailure)
            {
                return Result.Failure(removeRoleResult.Error);
            }

            await _userAggregateRepository.UpdateAsync(userAggregate, cancellationToken);

            // Synchronize role removal with ASP.NET Core Identity
            var identityResult = await _identityService.RemoveUserFromRoleAsync(request.UserId, request.RoleName);
            if (identityResult.IsFailure)
            {
                return Result.Failure(identityResult.Error);
            }

            return Result.Success();
        }, cancellationToken);
    }
}
