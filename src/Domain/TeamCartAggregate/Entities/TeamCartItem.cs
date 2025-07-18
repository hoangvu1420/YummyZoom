using YummyZoom.Domain.Common.Models;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate.Errors;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.TeamCartAggregate.Entities;

/// <summary>
/// Represents an item in a team cart with its customizations and snapshot data.
/// This entity captures menu item data at the time of adding to ensure price consistency.
/// </summary>
public sealed class TeamCartItem : Entity<TeamCartItemId>
{
    private readonly List<TeamCartItemCustomization> _selectedCustomizations = [];

    /// <summary>
    /// Gets the ID of the user who added this item to the cart.
    /// </summary>
    public UserId AddedByUserId { get; private set; }

    /// <summary>
    /// Gets the snapshot of the menu item ID at the time of adding to cart.
    /// </summary>
    public MenuItemId Snapshot_MenuItemId { get; private set; }

    /// <summary>
    /// Gets the snapshot of the menu category ID at the time of adding to cart.
    /// </summary>
    public MenuCategoryId Snapshot_MenuCategoryId { get; private set; }

    /// <summary>
    /// Gets the snapshot of the item name at the time of adding to cart.
    /// </summary>
    public string Snapshot_ItemName { get; private set; }

    /// <summary>
    /// Gets the snapshot of the base price at the time of adding to cart.
    /// </summary>
    public Money Snapshot_BasePriceAtOrder { get; private set; }

    /// <summary>
    /// Gets the quantity of this item.
    /// </summary>
    public int Quantity { get; private set; }

    /// <summary>
    /// Gets the total price for this line item (base price + customizations) * quantity.
    /// </summary>
    public Money LineItemTotal { get; private set; }

    /// <summary>
    /// Gets a read-only list of selected customizations for this item.
    /// </summary>
    public IReadOnlyList<TeamCartItemCustomization> SelectedCustomizations => _selectedCustomizations.AsReadOnly();

    /// <summary>
    /// Initializes a new instance of the <see cref="TeamCartItem"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the item.</param>
    /// <param name="addedByUserId">The ID of the user who added this item.</param>
    /// <param name="snapshotMenuItemId">The snapshot of the menu item ID.</param>
    /// <param name="snapshotMenuCategoryId">The snapshot of the menu category ID.</param>
    /// <param name="snapshotItemName">The snapshot of the item name.</param>
    /// <param name="snapshotBasePriceAtOrder">The snapshot of the base price.</param>
    /// <param name="quantity">The quantity of this item.</param>
    /// <param name="selectedCustomizations">The list of selected customizations.</param>
    private TeamCartItem(
        TeamCartItemId id,
        UserId addedByUserId,
        MenuItemId snapshotMenuItemId,
        MenuCategoryId snapshotMenuCategoryId,
        string snapshotItemName,
        Money snapshotBasePriceAtOrder,
        int quantity,
        List<TeamCartItemCustomization> selectedCustomizations)
        : base(id)
    {
        AddedByUserId = addedByUserId;
        Snapshot_MenuItemId = snapshotMenuItemId;
        Snapshot_MenuCategoryId = snapshotMenuCategoryId;
        Snapshot_ItemName = snapshotItemName;
        Snapshot_BasePriceAtOrder = snapshotBasePriceAtOrder;
        Quantity = quantity;
        _selectedCustomizations.AddRange(selectedCustomizations);

        // Calculate line item total
        var customizationTotal = selectedCustomizations
            .Sum(c => c.Snapshot_ChoicePriceAdjustmentAtOrder.Amount);
        
        var itemTotal = snapshotBasePriceAtOrder.Amount + customizationTotal;
        LineItemTotal = new Money(itemTotal * quantity, snapshotBasePriceAtOrder.Currency);
    }

    /// <summary>
    /// Required for ORM (e.g., Entity Framework Core) and deserialization.
    /// </summary>
#pragma warning disable CS8618
    private TeamCartItem() { }
#pragma warning restore CS8618

    /// <summary>
    /// Creates a new team cart item.
    /// </summary>
    /// <param name="addedByUserId">The ID of the user adding this item.</param>
    /// <param name="snapshotMenuItemId">The snapshot of the menu item ID.</param>
    /// <param name="snapshotMenuCategoryId">The snapshot of the menu category ID.</param>
    /// <param name="snapshotItemName">The snapshot of the item name.</param>
    /// <param name="snapshotBasePriceAtOrder">The snapshot of the base price.</param>
    /// <param name="quantity">The quantity of this item.</param>
    /// <param name="selectedCustomizations">The list of selected customizations.</param>
    /// <returns>A result containing the new team cart item if successful, or an error if validation fails.</returns>
    public static Result<TeamCartItem> Create(
        UserId addedByUserId,
        MenuItemId snapshotMenuItemId,
        MenuCategoryId snapshotMenuCategoryId,
        string snapshotItemName,
        Money snapshotBasePriceAtOrder,
        int quantity,
        List<TeamCartItemCustomization>? selectedCustomizations = null)
    {
        if (addedByUserId is null)
        {
            return Result.Failure<TeamCartItem>(TeamCartErrors.ItemUserIdRequired);
        }

        if (snapshotMenuItemId is null)
        {
            return Result.Failure<TeamCartItem>(TeamCartErrors.MenuItemRequired);
        }

        if (string.IsNullOrWhiteSpace(snapshotItemName))
        {
            return Result.Failure<TeamCartItem>(TeamCartErrors.ItemNameRequired);
        }

        if (quantity <= 0)
        {
            return Result.Failure<TeamCartItem>(TeamCartErrors.InvalidQuantity);
        }

        var customizations = selectedCustomizations ?? [];

        var item = new TeamCartItem(
            TeamCartItemId.CreateUnique(),
            addedByUserId,
            snapshotMenuItemId,
            snapshotMenuCategoryId,
            snapshotItemName,
            snapshotBasePriceAtOrder,
            quantity,
            customizations);

        return Result.Success(item);
    }

    /// <summary>
    /// Updates the quantity of this item and recalculates the line item total.
    /// </summary>
    /// <param name="newQuantity">The new quantity.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result UpdateQuantity(int newQuantity)
    {
        if (newQuantity <= 0)
        {
            return Result.Failure(TeamCartErrors.InvalidQuantity);
        }

        Quantity = newQuantity;

        // Recalculate line item total
        var customizationTotal = _selectedCustomizations
            .Sum(c => c.Snapshot_ChoicePriceAdjustmentAtOrder.Amount);
        
        var itemTotal = Snapshot_BasePriceAtOrder.Amount + customizationTotal;
        LineItemTotal = new Money(itemTotal * newQuantity, Snapshot_BasePriceAtOrder.Currency);

        return Result.Success();
    }
}
