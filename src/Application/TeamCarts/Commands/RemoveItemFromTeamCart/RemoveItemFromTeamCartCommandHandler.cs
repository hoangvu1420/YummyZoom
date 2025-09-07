using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.Errors;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TeamCarts.Commands.RemoveItemFromTeamCart;

public sealed class RemoveItemFromTeamCartCommandHandler : IRequestHandler<RemoveItemFromTeamCartCommand, Result<Unit>>
{
    private readonly ITeamCartRepository _teamCartRepository;
    private readonly IUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RemoveItemFromTeamCartCommandHandler> _logger;

    public RemoveItemFromTeamCartCommandHandler(
        ITeamCartRepository teamCartRepository,
        IUser currentUser,
        IUnitOfWork unitOfWork,
        ILogger<RemoveItemFromTeamCartCommandHandler> logger)
    {
        _teamCartRepository = teamCartRepository ?? throw new ArgumentNullException(nameof(teamCartRepository));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<Unit>> Handle(RemoveItemFromTeamCartCommand request, CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            if (_currentUser.DomainUserId is null)
            {
                throw new UnauthorizedAccessException();
            }

            var userId = _currentUser.DomainUserId!;
            var cartId = TeamCartId.Create(request.TeamCartId);
            var itemId = TeamCartItemId.Create(request.TeamCartItemId);

            var cart = await _teamCartRepository.GetByIdAsync(cartId, cancellationToken);
            if (cart is null)
            {
                _logger.LogWarning("TeamCart not found: {TeamCartId}", request.TeamCartId);
                return Result.Failure<Unit>(TeamCartErrors.TeamCartNotFound);
            }

            if (cart.Status != TeamCartStatus.Open)
            {
                _logger.LogWarning("Cannot remove item when cart not open. CartId={CartId} Status={Status}", request.TeamCartId, cart.Status);
                return Result.Failure<Unit>(TeamCartErrors.CannotModifyCartOnceLocked);
            }

            var item = cart.Items.FirstOrDefault(i => i.Id == itemId);
            if (item is null)
            {
                _logger.LogWarning("TeamCart item not found. CartId={CartId} ItemId={ItemId}", request.TeamCartId, request.TeamCartItemId);
                return Result.Failure<Unit>(RemoveItemFromTeamCartErrors.ItemNotFound(request.TeamCartItemId));
            }

            if (item.AddedByUserId != userId)
            {
                _logger.LogWarning("User is not the owner of TeamCart item. CartId={CartId} ItemId={ItemId} UserId={UserId}", request.TeamCartId, request.TeamCartItemId, userId.Value);
                return Result.Failure<Unit>(RemoveItemFromTeamCartErrors.NotItemOwner(userId.Value, request.TeamCartItemId));
            }

            var removeResult = cart.RemoveItem(userId, itemId);
            if (removeResult.IsFailure)
            {
                _logger.LogWarning("Failed to remove item. CartId={CartId} ItemId={ItemId} Error={Error}", request.TeamCartId, request.TeamCartItemId, removeResult.Error.Code);
                return Result.Failure<Unit>(removeResult.Error);
            }

            await _teamCartRepository.UpdateAsync(cart, cancellationToken);

            _logger.LogInformation("Removed TeamCart item. CartId={CartId} ItemId={ItemId} UserId={UserId}", request.TeamCartId, request.TeamCartItemId, userId.Value);

            return Result.Success(Unit.Value);
        }, cancellationToken);
    }
}

