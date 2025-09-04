using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.MenuItems.Commands.RemoveCustomizationGroupFromMenuItem;

[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record RemoveCustomizationGroupFromMenuItemCommand(
    Guid RestaurantId,
    Guid MenuItemId,
    Guid CustomizationGroupId
) : IRequest<Result>, IRestaurantCommand
{
    Domain.RestaurantAggregate.ValueObjects.RestaurantId IRestaurantCommand.RestaurantId => Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public sealed class RemoveCustomizationGroupFromMenuItemCommandHandler : IRequestHandler<RemoveCustomizationGroupFromMenuItemCommand, Result>
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveCustomizationGroupFromMenuItemCommandHandler(
        IMenuItemRepository menuItemRepository,
        IUnitOfWork unitOfWork)
    {
        _menuItemRepository = menuItemRepository ?? throw new ArgumentNullException(nameof(menuItemRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public Task<Result> Handle(RemoveCustomizationGroupFromMenuItemCommand request, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var itemId = MenuItemId.Create(request.MenuItemId);
            var menuItem = await _menuItemRepository.GetByIdAsync(itemId, cancellationToken);

            if (menuItem is null)
            {
                return Result.Failure(RemoveCustomizationGroupFromMenuItemErrors.MenuItemNotFound(request.MenuItemId));
            }

            // Enforce restaurant tenancy
            if (menuItem.RestaurantId.Value != request.RestaurantId)
            {
                throw new ForbiddenAccessException();
            }

            var groupId = CustomizationGroupId.Create(request.CustomizationGroupId);
            var result = menuItem.RemoveCustomizationGroup(groupId);
            if (result.IsFailure)
            {
                return Result.Failure(result.Error);
            }

            _menuItemRepository.Update(menuItem);
            return Result.Success();
        }, cancellationToken);
    }
}

public static class RemoveCustomizationGroupFromMenuItemErrors
{
    public static Error MenuItemNotFound(Guid menuItemId) => Error.NotFound(
        "MenuItem.MenuItemNotFound",
        $"Menu item with ID '{menuItemId}' was not found.");
}

