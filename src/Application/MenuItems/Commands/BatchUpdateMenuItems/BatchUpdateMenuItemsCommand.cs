using System.Text.Json;
using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.MenuItems.Commands.BatchUpdateMenuItems;

[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record BatchUpdateMenuItemsCommand(
    Guid RestaurantId,
    IReadOnlyList<MenuItemBatchUpdateOperation> Operations
) : IRequest<Result<BatchUpdateMenuItemsResult>>, IRestaurantCommand
{
    Domain.RestaurantAggregate.ValueObjects.RestaurantId IRestaurantCommand.RestaurantId =>
        Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

public sealed record MenuItemBatchUpdateOperation(Guid ItemId, string Field, JsonElement Value);

public sealed record BatchUpdateMenuItemsResult(
    int SuccessCount,
    int FailedCount,
    IReadOnlyList<BatchUpdateMenuItemError> Errors);

public sealed record BatchUpdateMenuItemError(Guid ItemId, string Field, string Message);

public sealed class BatchUpdateMenuItemsCommandHandler : IRequestHandler<BatchUpdateMenuItemsCommand, Result<BatchUpdateMenuItemsResult>>
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public BatchUpdateMenuItemsCommandHandler(
        IMenuItemRepository menuItemRepository,
        IUnitOfWork unitOfWork)
    {
        _menuItemRepository = menuItemRepository ?? throw new ArgumentNullException(nameof(menuItemRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public Task<Result<BatchUpdateMenuItemsResult>> Handle(BatchUpdateMenuItemsCommand request, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var errors = new List<BatchUpdateMenuItemError>();
            var successCount = 0;
            var totalOps = request.Operations.Count;

            // Fetch all involved menu items in one round-trip to reduce DB queries.
            var distinctIds = request.Operations
                .Select(o => MenuItemId.Create(o.ItemId))
                .Distinct()
                .ToList();

            var menuItems = await _menuItemRepository.GetByIdsAsync(distinctIds, cancellationToken);
            var menuItemLookup = menuItems.ToDictionary(m => m.Id.Value, m => m);

            foreach (var op in request.Operations)
            {
                if (!menuItemLookup.TryGetValue(op.ItemId, out var menuItem))
                {
                    errors.Add(new BatchUpdateMenuItemError(
                        op.ItemId,
                        op.Field,
                        $"Menu item '{op.ItemId}' was not found."));
                    continue;
                }

                // Enforce restaurant tenancy per item to avoid leaking cross-restaurant data
                if (menuItem.RestaurantId.Value != request.RestaurantId)
                {
                    throw new ForbiddenAccessException();
                }

                var updateResult = ApplyOperation(menuItem, op);
                if (updateResult.IsFailure)
                {
                    errors.Add(new BatchUpdateMenuItemError(
                        op.ItemId,
                        op.Field,
                        updateResult.Error.Description));
                    continue;
                }

                _menuItemRepository.Update(menuItem);
                successCount++;
            }

            var result = new BatchUpdateMenuItemsResult(
                successCount,
                FailedCount: totalOps - successCount,
                Errors: errors);

            return Result.Success(result);
        }, cancellationToken);
    }

    private static Result ApplyOperation(MenuItem menuItem, MenuItemBatchUpdateOperation op)
    {
        var field = Normalize(op.Field);

        return field switch
        {
            "isavailable" => ApplyAvailability(menuItem, op.Value),
            "price" => ApplyPrice(menuItem, op.Value),
            _ => Result.Failure(Error.Validation(
                "MenuItem.UnsupportedField",
                $"Field '{op.Field}' is not supported for batch updates."))
        };
    }

    private static Result ApplyAvailability(MenuItem menuItem, JsonElement value)
    {
        if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            return Result.Failure(Error.Validation(
                "MenuItem.InvalidAvailabilityValue",
                "Value must be a boolean."));
        }

        var isAvailable = value.GetBoolean();
        menuItem.ChangeAvailability(isAvailable);
        return Result.Success();
    }

    private static Result ApplyPrice(MenuItem menuItem, JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDecimal(out var price))
        {
            return Result.Failure(Error.Validation(
                "MenuItem.InvalidPriceValue",
                "Value must be a numeric price."));
        }

        var newPrice = new Money(price, menuItem.BasePrice.Currency);
        var updateResult = menuItem.UpdatePrice(newPrice);
        return updateResult;
    }

    private static string Normalize(string field) => field.Trim().ToLowerInvariant();
}
