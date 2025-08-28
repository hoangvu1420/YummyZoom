using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.MenuItems.Commands.ChangeMenuItemAvailability;

[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record ChangeMenuItemAvailabilityCommand(
    Guid RestaurantId,
    Guid MenuItemId,
    bool IsAvailable
) : IRequest<Result>, IRestaurantCommand
{
    RestaurantId IRestaurantCommand.RestaurantId => Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public sealed class ChangeMenuItemAvailabilityCommandHandler : IRequestHandler<ChangeMenuItemAvailabilityCommand, Result>
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeMenuItemAvailabilityCommandHandler(
        IMenuItemRepository menuItemRepository,
        IUnitOfWork unitOfWork)
    {
        _menuItemRepository = menuItemRepository ?? throw new ArgumentNullException(nameof(menuItemRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public Task<Result> Handle(ChangeMenuItemAvailabilityCommand request, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var itemId = MenuItemId.Create(request.MenuItemId);
            var menuItem = await _menuItemRepository.GetByIdAsync(itemId, cancellationToken);

            if (menuItem is null)
            {
                return Result.Failure(ChangeMenuItemAvailabilityErrors.MenuItemNotFound(request.MenuItemId));
            }

            // Ensure the item belongs to the restaurant in scope; otherwise, forbid access
            if (menuItem.RestaurantId.Value != request.RestaurantId)
            {
                throw new ForbiddenAccessException();
            }

            // Update availability via aggregate (raises domain event only if changed)
            menuItem.ChangeAvailability(request.IsAvailable);

            _menuItemRepository.Update(menuItem);

            return Result.Success();
        }, cancellationToken);
    }
}

public static class ChangeMenuItemAvailabilityErrors
{
    public static Error MenuItemNotFound(Guid menuItemId) => Error.NotFound(
        "MenuItem.MenuItemNotFound",
        $"Menu item with ID '{menuItemId}' was not found.");
}

