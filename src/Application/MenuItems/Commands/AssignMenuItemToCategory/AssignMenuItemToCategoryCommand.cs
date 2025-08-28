using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.Errors;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.MenuItems.Commands.AssignMenuItemToCategory;

[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record AssignMenuItemToCategoryCommand(
    Guid RestaurantId,
    Guid MenuItemId,
    Guid NewCategoryId
) : IRequest<Result>, IRestaurantCommand
{
    RestaurantId IRestaurantCommand.RestaurantId => Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public sealed class AssignMenuItemToCategoryCommandHandler : IRequestHandler<AssignMenuItemToCategoryCommand, Result>
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IMenuCategoryRepository _menuCategoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AssignMenuItemToCategoryCommandHandler(
        IMenuItemRepository menuItemRepository,
        IMenuCategoryRepository menuCategoryRepository,
        IUnitOfWork unitOfWork)
    {
        _menuItemRepository = menuItemRepository ?? throw new ArgumentNullException(nameof(menuItemRepository));
        _menuCategoryRepository = menuCategoryRepository ?? throw new ArgumentNullException(nameof(menuCategoryRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public Task<Result> Handle(AssignMenuItemToCategoryCommand request, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var itemId = MenuItemId.Create(request.MenuItemId);
            var menuItem = await _menuItemRepository.GetByIdAsync(itemId, cancellationToken);
            if (menuItem is null)
            {
                return Result.Failure(AssignMenuItemToCategoryErrors.MenuItemNotFound(request.MenuItemId));
            }

            // Ensure the item belongs to the restaurant in scope; otherwise, forbid access
            if (menuItem.RestaurantId.Value != request.RestaurantId)
            {
                throw new ForbiddenAccessException();
            }

            var targetCategoryId = MenuCategoryId.Create(request.NewCategoryId);
            var category = await _menuCategoryRepository.GetByIdAsync(targetCategoryId, cancellationToken);
            if (category is null)
            {
                return Result.Failure(MenuItemErrors.CategoryNotFound(request.NewCategoryId));
            }

            // Validate that the target category belongs to the same restaurant
            var restaurantId = RestaurantId.Create(request.RestaurantId);
            var categoriesForRestaurant = await _menuCategoryRepository.GetByRestaurantIdAsync(restaurantId, cancellationToken);
            if (!categoriesForRestaurant.Any(c => c.Id == targetCategoryId))
            {
                return Result.Failure(MenuItemErrors.CategoryNotBelongsToRestaurant(request.NewCategoryId, request.RestaurantId));
            }

            var assignResult = menuItem.AssignToCategory(targetCategoryId);
            if (assignResult.IsFailure)
            {
                return Result.Failure(assignResult.Error);
            }

            _menuItemRepository.Update(menuItem);
            return Result.Success();
        }, cancellationToken);
    }
}

public static class AssignMenuItemToCategoryErrors
{
    public static Error MenuItemNotFound(Guid menuItemId) => Error.NotFound(
        "MenuItem.MenuItemNotFound",
        $"Menu item with ID '{menuItemId}' was not found.");
}

