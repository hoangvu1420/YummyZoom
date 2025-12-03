using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.CustomizationGroupAggregate;
using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate.Errors;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;

public sealed class AddItemToTeamCartCommandHandler : IRequestHandler<AddItemToTeamCartCommand, Result>
{
    private readonly ITeamCartRepository _teamCartRepository;
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly ICustomizationGroupRepository _customizationGroupRepository;
    private readonly IUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddItemToTeamCartCommandHandler> _logger;

    public AddItemToTeamCartCommandHandler(
        ITeamCartRepository teamCartRepository,
        IMenuItemRepository menuItemRepository,
        ICustomizationGroupRepository customizationGroupRepository,
        IUser currentUser,
        IUnitOfWork unitOfWork,
        ILogger<AddItemToTeamCartCommandHandler> logger)
    {
        _teamCartRepository = teamCartRepository ?? throw new ArgumentNullException(nameof(teamCartRepository));
        _menuItemRepository = menuItemRepository ?? throw new ArgumentNullException(nameof(menuItemRepository));
        _customizationGroupRepository = customizationGroupRepository ?? throw new ArgumentNullException(nameof(customizationGroupRepository));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(AddItemToTeamCartCommand request, CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // Authorization handled by pipeline - user guaranteed to be authenticated and have TeamCart access
            var userId = _currentUser.DomainUserId!;
            var teamCartId = TeamCartId.Create(request.TeamCartId);
            var menuItemId = MenuItemId.Create(request.MenuItemId);

            // Load TeamCart
            var cart = await _teamCartRepository.GetByIdAsync(teamCartId, cancellationToken);
            if (cart is null)
            {
                _logger.LogWarning("TeamCart not found: {TeamCartId}", request.TeamCartId);
                return Result.Failure(TeamCartErrors.TeamCartNotFound);
            }

            // Load MenuItem
            var menuItem = await _menuItemRepository.GetByIdAsync(menuItemId, cancellationToken);
            if (menuItem is null)
            {
                return Result.Failure(AddItemToTeamCartErrors.MenuItemNotFound(request.MenuItemId));
            }

            // Validate MenuItem belongs to the same restaurant as TeamCart
            if (menuItem.RestaurantId != cart.RestaurantId)
            {
                _logger.LogWarning("MenuItem from different restaurant. MenuItemRestaurant={MenuItemRestaurantId}, CartRestaurant={CartRestaurantId}. MenuItemId={MenuItemId}, CartId={TeamCartId}", menuItem.RestaurantId.Value, cart.RestaurantId.Value, request.MenuItemId, request.TeamCartId);
                return Result.Failure(AddItemToTeamCartErrors.MenuItemNotBelongsToRestaurant(request.MenuItemId, cart.RestaurantId.Value));
            }

            // Validate availability
            if (!menuItem.IsAvailable)
            {
                _logger.LogWarning("MenuItem unavailable: {MenuItemId} for TeamCart {TeamCartId}", request.MenuItemId, request.TeamCartId);
                return Result.Failure(AddItemToTeamCartErrors.MenuItemUnavailable(request.MenuItemId));
            }

            // Build and validate snapshot customizations from request selections with cardinality rules
            List<TeamCartItemCustomization>? selectedCustomizations = null;
            var requestedSelections = (IReadOnlyList<AddItemToTeamCartCustomizationSelection>)(request.SelectedCustomizations ?? Array.Empty<AddItemToTeamCartCustomizationSelection>());

            // Determine all groups applied to this menu item
            var appliedGroupIds = menuItem.AppliedCustomizations
                .Select(ac => ac.CustomizationGroupId.Value)
                .Distinct()
                .ToList();

            // Load definitions for applied and requested groups
            var requestedGroupIds = requestedSelections.Select(s => s.GroupId).Distinct().ToList();
            var allGroupIdsToLoad = appliedGroupIds
                .Union(requestedGroupIds)
                .Select(CustomizationGroupId.Create)
                .ToList();

            var allGroups = await _customizationGroupRepository.GetByIdsAsync(allGroupIdsToLoad, cancellationToken);
            var groupsById = allGroups.ToDictionary(g => g.Id.Value, g => g);

            // Enforce required groups (MinSelections > 0) that are applied to this item
            foreach (var appliedGroupId in appliedGroupIds)
            {
                if (!groupsById.TryGetValue(appliedGroupId, out var appliedGroup))
                {
                    _logger.LogWarning("Applied customization group definition not found. GroupId={GroupId} MenuItemId={MenuItemId}", appliedGroupId, request.MenuItemId);
                    continue;
                }

                if (appliedGroup.MinSelections > 0)
                {
                    var count = requestedSelections.Count(s => s.GroupId == appliedGroupId);
                    if (count < appliedGroup.MinSelections)
                    {
                        _logger.LogWarning("Customization group minimum not satisfied. GroupId={GroupId} Min={Min} Actual={Actual} MenuItemId={MenuItemId} CartId={CartId}", appliedGroupId, appliedGroup.MinSelections, count, request.MenuItemId, request.TeamCartId);
                        return Result.Failure(TeamCartErrors.InvalidCustomization);
                    }
                }
            }

            if (requestedSelections.Count > 0)
            {
                selectedCustomizations = new List<TeamCartItemCustomization>(requestedSelections.Count);

                foreach (var groupSelections in requestedSelections.GroupBy(s => s.GroupId))
                {
                    var groupId = groupSelections.Key;
                    if (!groupsById.TryGetValue(groupId, out var group))
                    {
                        _logger.LogWarning("Requested customization group not found. GroupId={GroupId} MenuItemId={MenuItemId} CartId={CartId}", groupId, request.MenuItemId, request.TeamCartId);
                        return Result.Failure(AddItemToTeamCartErrors.CustomizationGroupNotFound(groupId));
                    }

                    // Ensure the group is applied to menu item
                    var isAppliedToItem = menuItem.AppliedCustomizations.Any(ac => ac.CustomizationGroupId.Value == groupId);
                    if (!isAppliedToItem)
                    {
                        _logger.LogWarning("Customization group not applied to menu item. GroupId={GroupId} MenuItemId={MenuItemId}", groupId, request.MenuItemId);
                        return Result.Failure(AddItemToTeamCartErrors.CustomizationGroupNotAppliedToMenuItem(groupId, request.MenuItemId));
                    }

                    // Enforce MaxSelections and duplicates
                    var distinctChoiceIds = groupSelections.Select(s => s.ChoiceId).Distinct().ToList();
                    if (distinctChoiceIds.Count != groupSelections.Count())
                    {
                        _logger.LogWarning("Duplicate customization choice detected. GroupId={GroupId} MenuItemId={MenuItemId}", groupId, request.MenuItemId);
                        return Result.Failure(TeamCartErrors.InvalidCustomization);
                    }
                    if (group.MaxSelections >= 0 && distinctChoiceIds.Count > group.MaxSelections)
                    {
                        _logger.LogWarning("Customization group maximum exceeded. GroupId={GroupId} Max={Max} Actual={Actual} MenuItemId={MenuItemId}", groupId, group.MaxSelections, distinctChoiceIds.Count, request.MenuItemId);
                        return Result.Failure(TeamCartErrors.InvalidCustomization);
                    }

                    foreach (var choiceId in distinctChoiceIds)
                    {
                        var choice = group.Choices.FirstOrDefault(c => c.Id.Value == choiceId);
                        if (choice is null)
                        {
                            _logger.LogWarning("Customization choice not found in group. GroupId={GroupId} ChoiceId={ChoiceId} MenuItemId={MenuItemId}", groupId, choiceId, request.MenuItemId);
                            return Result.Failure(AddItemToTeamCartErrors.CustomizationChoiceNotFound(groupId, choiceId));
                        }

                        var custResult = TeamCartItemCustomization.Create(
                            group.GroupName,
                            choice.Name,
                            choice.PriceAdjustment.Copy()); // Use copy to avoid EF Core tracking conflicts

                        if (custResult.IsFailure)
                        {
                            _logger.LogWarning("Customization snapshot creation failed. GroupId={GroupId} ChoiceId={ChoiceId} Error={Error}", groupId, choiceId, custResult.Error.Code);
                            return Result.Failure(custResult.Error);
                        }

                        selectedCustomizations.Add(custResult.Value);
                    }
                }
            }

            // Add item to cart via aggregate
            var addResult = cart.AddItem(
                userId,
                menuItem.Id,
                menuItem.MenuCategoryId,
                menuItem.Name,
                menuItem.BasePrice.Copy(), // Use copy to avoid EF Core tracking conflicts
                request.Quantity,
                selectedCustomizations);

            if (addResult.IsFailure)
            {
                _logger.LogWarning("Failed to add item to TeamCart {TeamCartId}: {Reason}", request.TeamCartId, addResult.Error.Code);
                return Result.Failure(addResult.Error);
            }

            await _teamCartRepository.UpdateAsync(cart, cancellationToken);

            _logger.LogInformation(
                "TeamCart item added. CartId={CartId} UserId={UserId} RestaurantId={RestaurantId} MenuItemId={MenuItemId} Qty={Qty} Customizations={CustomizationCount}",
                request.TeamCartId,
                userId.Value,
                cart.RestaurantId.Value,
                request.MenuItemId,
                request.Quantity,
                selectedCustomizations?.Count ?? 0);

            // Real-time store update occurs via domain event handlers (ItemAddedToTeamCart).
            return Result.Success();
        }, cancellationToken);
    }
}
