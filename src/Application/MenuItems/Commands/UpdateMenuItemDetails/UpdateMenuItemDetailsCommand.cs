using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.MenuItems.Commands.UpdateMenuItemDetails;

[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record UpdateMenuItemDetailsCommand(
    Guid RestaurantId,
    Guid MenuItemId,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    string? ImageUrl = null
) : IRequest<Result>, IRestaurantCommand
{
    Domain.RestaurantAggregate.ValueObjects.RestaurantId IRestaurantCommand.RestaurantId => Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public sealed class UpdateMenuItemDetailsCommandHandler : IRequestHandler<UpdateMenuItemDetailsCommand, Result>
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateMenuItemDetailsCommandHandler(
        IMenuItemRepository menuItemRepository,
        IUnitOfWork unitOfWork)
    {
        _menuItemRepository = menuItemRepository ?? throw new ArgumentNullException(nameof(menuItemRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public Task<Result> Handle(UpdateMenuItemDetailsCommand request, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var itemId = MenuItemId.Create(request.MenuItemId);
            var menuItem = await _menuItemRepository.GetByIdAsync(itemId, cancellationToken);

            if (menuItem is null)
            {
                return Result.Failure(UpdateMenuItemDetailsErrors.MenuItemNotFound(request.MenuItemId));
            }

            // Ensure scoping to the restaurant
            if (menuItem.RestaurantId.Value != request.RestaurantId)
            {
                throw new ForbiddenAccessException();
            }

            var newPrice = new Money(request.Price, request.Currency);
            var updateResult = menuItem.UpdateDetails(request.Name, request.Description, newPrice, request.ImageUrl);
            if (updateResult.IsFailure)
            {
                return Result.Failure(updateResult.Error);
            }

            _menuItemRepository.Update(menuItem);
            return Result.Success();
        }, cancellationToken);
    }
}

public static class UpdateMenuItemDetailsErrors
{
    public static Error MenuItemNotFound(Guid menuItemId) => Error.NotFound(
        "MenuItem.MenuItemNotFound",
        $"Menu item with ID '{menuItemId}' was not found.");
}

