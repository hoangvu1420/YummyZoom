using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.MenuItems.Commands.AssignCustomizationGroupToMenuItem;

[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record AssignCustomizationGroupToMenuItemCommand(
    Guid RestaurantId,
    Guid MenuItemId,
    Guid CustomizationGroupId,
    string DisplayTitle,
    int? DisplayOrder
) : IRequest<Result>, IRestaurantCommand
{
    Domain.RestaurantAggregate.ValueObjects.RestaurantId IRestaurantCommand.RestaurantId => Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public sealed class AssignCustomizationGroupToMenuItemCommandHandler : IRequestHandler<AssignCustomizationGroupToMenuItemCommand, Result>
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly ICustomizationGroupRepository _customizationGroupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AssignCustomizationGroupToMenuItemCommandHandler(
        IMenuItemRepository menuItemRepository,
        ICustomizationGroupRepository customizationGroupRepository,
        IUnitOfWork unitOfWork)
    {
        _menuItemRepository = menuItemRepository ?? throw new ArgumentNullException(nameof(menuItemRepository));
        _customizationGroupRepository = customizationGroupRepository ?? throw new ArgumentNullException(nameof(customizationGroupRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public Task<Result> Handle(AssignCustomizationGroupToMenuItemCommand request, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var itemId = MenuItemId.Create(request.MenuItemId);
            var menuItem = await _menuItemRepository.GetByIdAsync(itemId, cancellationToken);

            if (menuItem is null)
            {
                return Result.Failure(AssignCustomizationGroupToMenuItemErrors.MenuItemNotFound(request.MenuItemId));
            }

            // Enforce restaurant tenancy
            if (menuItem.RestaurantId.Value != request.RestaurantId)
            {
                throw new ForbiddenAccessException();
            }

            var groupId = CustomizationGroupId.Create(request.CustomizationGroupId);
            var groups = await _customizationGroupRepository.GetByIdsAsync(new[] { groupId }, cancellationToken);
            var group = groups.FirstOrDefault();
            if (group is null)
            {
                return Result.Failure(AssignCustomizationGroupToMenuItemErrors.CustomizationGroupNotFound(request.CustomizationGroupId));
            }
            if (group.RestaurantId.Value != request.RestaurantId)
            {
                return Result.Failure(AssignCustomizationGroupToMenuItemErrors.CustomizationGroupNotBelongsToRestaurant(request.CustomizationGroupId, request.RestaurantId));
            }

            // Determine display order
            var order = request.DisplayOrder ??
                        (menuItem.AppliedCustomizations.Count > 0
                            ? menuItem.AppliedCustomizations.Max(c => c.DisplayOrder) + 1
                            : 1);

            var applied = AppliedCustomization.Create(groupId, request.DisplayTitle.Trim(), order);
            var assignResult = menuItem.AssignCustomizationGroup(applied);
            if (assignResult.IsFailure)
            {
                return Result.Failure(assignResult.Error);
            }

            _menuItemRepository.Update(menuItem);
            return Result.Success();
        }, cancellationToken);
    }
}

public static class AssignCustomizationGroupToMenuItemErrors
{
    public static Error MenuItemNotFound(Guid menuItemId) => Error.NotFound(
        "MenuItem.MenuItemNotFound",
        $"Menu item with ID '{menuItemId}' was not found.");

    public static Error CustomizationGroupNotFound(Guid groupId) => Error.NotFound(
        "CustomizationGroup.NotFound",
        $"Customization group with ID '{groupId}' was not found.");

    public static Error CustomizationGroupNotBelongsToRestaurant(Guid groupId, Guid restaurantId) => Error.Validation(
        "CustomizationGroup.NotBelongsToRestaurant",
        $"Customization group '{groupId}' does not belong to restaurant '{restaurantId}'.");
}

