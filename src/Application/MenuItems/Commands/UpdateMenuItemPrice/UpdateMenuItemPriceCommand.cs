using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.MenuItems.Commands.UpdateMenuItemPrice;

[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record UpdateMenuItemPriceCommand(
    Guid RestaurantId,
    Guid MenuItemId,
    decimal Price,
    string Currency
) : IRequest<Result>, IRestaurantCommand
{
    Domain.RestaurantAggregate.ValueObjects.RestaurantId IRestaurantCommand.RestaurantId => Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public sealed class UpdateMenuItemPriceCommandHandler : IRequestHandler<UpdateMenuItemPriceCommand, Result>
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateMenuItemPriceCommandHandler(
        IMenuItemRepository menuItemRepository,
        IUnitOfWork unitOfWork)
    {
        _menuItemRepository = menuItemRepository ?? throw new ArgumentNullException(nameof(menuItemRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public Task<Result> Handle(UpdateMenuItemPriceCommand request, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var itemId = MenuItemId.Create(request.MenuItemId);
            var menuItem = await _menuItemRepository.GetByIdAsync(itemId, cancellationToken);

            if (menuItem is null)
            {
                return Result.Failure(UpdateMenuItemPriceErrors.MenuItemNotFound(request.MenuItemId));
            }

            // Enforce restaurant tenancy
            if (menuItem.RestaurantId.Value != request.RestaurantId)
            {
                throw new ForbiddenAccessException();
            }

            var newPrice = new Money(request.Price, request.Currency);
            var update = menuItem.UpdatePrice(newPrice);
            if (update.IsFailure)
            {
                return Result.Failure(update.Error);
            }

            _menuItemRepository.Update(menuItem);
            return Result.Success();
        }, cancellationToken);
    }
}

public static class UpdateMenuItemPriceErrors
{
    public static Error MenuItemNotFound(Guid menuItemId) => Error.NotFound(
        "MenuItem.MenuItemNotFound",
        $"Menu item with ID '{menuItemId}' was not found.");
}

