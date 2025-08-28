using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.MenuItems.Commands.DeleteMenuItem;

[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record DeleteMenuItemCommand(
    Guid RestaurantId,
    Guid MenuItemId
) : IRequest<Result>, IRestaurantCommand
{
    Domain.RestaurantAggregate.ValueObjects.RestaurantId IRestaurantCommand.RestaurantId => Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public sealed class DeleteMenuItemCommandHandler : IRequestHandler<DeleteMenuItemCommand, Result>
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUser _currentUser;

    public DeleteMenuItemCommandHandler(
        IMenuItemRepository menuItemRepository,
        IUnitOfWork unitOfWork,
        IUser currentUser)
    {
        _menuItemRepository = menuItemRepository ?? throw new ArgumentNullException(nameof(menuItemRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    public Task<Result> Handle(DeleteMenuItemCommand request, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var itemId = MenuItemId.Create(request.MenuItemId);
            var menuItem = await _menuItemRepository.GetByIdAsync(itemId, cancellationToken);

            if (menuItem is null)
            {
                return Result.Failure(DeleteMenuItemErrors.MenuItemNotFound(request.MenuItemId));
            }

            // Ensure the item belongs to the restaurant in scope; otherwise, forbid access
            if (menuItem.RestaurantId.Value != request.RestaurantId)
            {
                throw new ForbiddenAccessException();
            }

            // Soft-delete the menu item via domain method
            var deletedBy = _currentUser.Id;
            var result = menuItem.MarkAsDeleted(DateTimeOffset.UtcNow, deletedBy);
            if (result.IsFailure)
            {
                return Result.Failure(result.Error);
            }

            _menuItemRepository.Update(menuItem);

            return Result.Success();
        }, cancellationToken);
    }
}

public static class DeleteMenuItemErrors
{
    public static Error MenuItemNotFound(Guid menuItemId) => Error.NotFound(
        "MenuItem.MenuItemNotFound",
        $"Menu item with ID '{menuItemId}' was not found.");
}

