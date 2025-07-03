using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.MenuAggregate.ValueObjects;
using YummyZoom.Domain.OrderAggregate.Errors;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.OrderAggregate.Entities;

public sealed class OrderItem : Entity<OrderItemId>
{
    private readonly List<OrderItemCustomization> _selectedCustomizations = [];
    
    public MenuCategoryId Snapshot_MenuCategoryId { get; private set; }
    public MenuItemId Snapshot_MenuItemId { get; private set; }
    public string Snapshot_ItemName { get; private set; }
    public Money Snapshot_BasePriceAtOrder { get; private set; }
    public int Quantity { get; private set; }
    public Money LineItemTotal { get; private set; }

    public IReadOnlyList<OrderItemCustomization> SelectedCustomizations => _selectedCustomizations.AsReadOnly();

    private OrderItem(
        OrderItemId orderItemId,
        MenuCategoryId snapshotMenuCategoryId,
        MenuItemId snapshotMenuItemId,
        string snapshotItemName,
        Money snapshotBasePriceAtOrder,
        int quantity,
        List<OrderItemCustomization> selectedCustomizations)
        : base(orderItemId)
    {
        Snapshot_MenuCategoryId = snapshotMenuCategoryId;
        Snapshot_MenuItemId = snapshotMenuItemId;
        Snapshot_ItemName = snapshotItemName;
        Snapshot_BasePriceAtOrder = snapshotBasePriceAtOrder;
        Quantity = quantity;
        _selectedCustomizations = selectedCustomizations;
        LineItemTotal = CalculateLineItemTotal();
    }

    public static Result<OrderItem> Create(
        MenuCategoryId snapshotMenuCategoryId,
        MenuItemId snapshotMenuItemId,
        string snapshotItemName,
        Money snapshotBasePriceAtOrder,
        int quantity,
        List<OrderItemCustomization>? selectedCustomizations = null)
    {
        if (quantity <= 0)
        {
            return Result.Failure<OrderItem>(OrderErrors.OrderItemInvalidQuantity);
        }

        if (string.IsNullOrWhiteSpace(snapshotItemName))
        {
            return Result.Failure<OrderItem>(OrderErrors.OrderItemInvalidName);
        }

        return new OrderItem(
            OrderItemId.CreateUnique(),
            snapshotMenuCategoryId,
            snapshotMenuItemId,
            snapshotItemName,
            snapshotBasePriceAtOrder,
            quantity,
            selectedCustomizations ?? new List<OrderItemCustomization>());
    }

    private Money CalculateLineItemTotal()
    {
        var customizationTotal = _selectedCustomizations.Sum(c => c.Snapshot_ChoicePriceAdjustmentAtOrder.Amount);
        return new Money((Snapshot_BasePriceAtOrder.Amount + customizationTotal) * Quantity);
    }

#pragma warning disable CS8618
    private OrderItem() { }
#pragma warning restore CS8618
}
