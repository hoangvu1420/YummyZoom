using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.TagEntity.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.MenuItems.Commands.UpdateMenuItemDietaryTags;

[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record UpdateMenuItemDietaryTagsCommand(
    Guid RestaurantId,
    Guid MenuItemId,
    List<Guid>? DietaryTagIds
) : IRequest<Result>, IRestaurantCommand
{
    Domain.RestaurantAggregate.ValueObjects.RestaurantId IRestaurantCommand.RestaurantId => Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public sealed class UpdateMenuItemDietaryTagsCommandHandler : IRequestHandler<UpdateMenuItemDietaryTagsCommand, Result>
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateMenuItemDietaryTagsCommandHandler(
        IMenuItemRepository menuItemRepository,
        IUnitOfWork unitOfWork)
    {
        _menuItemRepository = menuItemRepository ?? throw new ArgumentNullException(nameof(menuItemRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public Task<Result> Handle(UpdateMenuItemDietaryTagsCommand request, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var itemId = MenuItemId.Create(request.MenuItemId);
            var menuItem = await _menuItemRepository.GetByIdAsync(itemId, cancellationToken);

            if (menuItem is null)
            {
                return Result.Failure(UpdateMenuItemDietaryTagsErrors.MenuItemNotFound(request.MenuItemId));
            }

            if (menuItem.RestaurantId.Value != request.RestaurantId)
            {
                throw new ForbiddenAccessException();
            }

            // Convert tag Ids to TagId VOs (null or empty clears tags)
            List<TagId>? tagIds = null;
            if (request.DietaryTagIds is { Count: > 0 })
            {
                tagIds = request.DietaryTagIds.Select(TagId.Create).ToList();
            }

            var result = menuItem.SetDietaryTags(tagIds);
            if (result.IsFailure)
            {
                return Result.Failure(result.Error);
            }

            _menuItemRepository.Update(menuItem);
            return Result.Success();
        }, cancellationToken);
    }
}

public static class UpdateMenuItemDietaryTagsErrors
{
    public static Error MenuItemNotFound(Guid menuItemId) => Error.NotFound(
        "MenuItem.MenuItemNotFound",
        $"Menu item with ID '{menuItemId}' was not found.");
}

