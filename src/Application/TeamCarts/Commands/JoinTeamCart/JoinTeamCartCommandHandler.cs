using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.SharedKernel;
using YummyZoom.Domain.TeamCartAggregate.Errors;

namespace YummyZoom.Application.TeamCarts.Commands.JoinTeamCart;

public sealed class JoinTeamCartCommandHandler : IRequestHandler<JoinTeamCartCommand, Result<Unit>>
{
    private readonly ITeamCartRepository _teamCartRepository;
    private readonly IUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<JoinTeamCartCommandHandler> _logger;

    public JoinTeamCartCommandHandler(
        ITeamCartRepository teamCartRepository,
        IUser currentUser,
        IUnitOfWork unitOfWork,
        ILogger<JoinTeamCartCommandHandler> logger)
    {
        _teamCartRepository = teamCartRepository ?? throw new ArgumentNullException(nameof(teamCartRepository));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<Unit>> Handle(JoinTeamCartCommand request, CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var guestUserId = _currentUser.DomainUserId!;
            var teamCartId = TeamCartId.Create(request.TeamCartId);

            var cart = await _teamCartRepository.GetByIdAsync(teamCartId, cancellationToken);
            if (cart is null)
            {
                _logger.LogWarning("TeamCart not found: {TeamCartId}", request.TeamCartId);
                return Result.Failure<Unit>(TeamCartErrors.TeamCartNotFound);
            }

            // Validate token
            var tokenResult = cart.ValidateJoinToken(request.ShareToken);
            if (tokenResult.IsFailure)
            {
                _logger.LogWarning("Invalid join token for TeamCart {TeamCartId}: {Reason}", request.TeamCartId, tokenResult.Error.Code);
                return Result.Failure<Unit>(tokenResult.Error);
            }

            // Add member as guest
            var addMemberResult = cart.AddMember(guestUserId, request.GuestName, MemberRole.Guest);
            if (addMemberResult.IsFailure)
            {
                _logger.LogWarning("Failed to add member to TeamCart {TeamCartId}: {Reason}", request.TeamCartId, addMemberResult.Error.Code);
                return Result.Failure<Unit>(addMemberResult.Error);
            }

            await _teamCartRepository.UpdateAsync(cart, cancellationToken);

            _logger.LogInformation("User {UserId} joined TeamCart {TeamCartId}", guestUserId.Value, request.TeamCartId);

            return Result.Success(Unit.Value);
        }, cancellationToken);
    }
}

