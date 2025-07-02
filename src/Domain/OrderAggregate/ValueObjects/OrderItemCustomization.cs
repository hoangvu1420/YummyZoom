using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.OrderAggregate.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.OrderAggregate.ValueObjects;

public sealed class OrderItemCustomization : ValueObject
{
    public string Snapshot_CustomizationGroupName { get; private set; }
    public string Snapshot_ChoiceName { get; private set; }
    public Money Snapshot_ChoicePriceAdjustmentAtOrder { get; private set; }

    private OrderItemCustomization(
        string snapshotCustomizationGroupName,
        string snapshotChoiceName,
        Money snapshotChoicePriceAdjustmentAtOrder)
    {
        Snapshot_CustomizationGroupName = snapshotCustomizationGroupName;
        Snapshot_ChoiceName = snapshotChoiceName;
        Snapshot_ChoicePriceAdjustmentAtOrder = snapshotChoicePriceAdjustmentAtOrder;
    }

    public static Result<OrderItemCustomization> Create(
        string snapshotCustomizationGroupName,
        string snapshotChoiceName,
        Money snapshotChoicePriceAdjustmentAtOrder)
    {
        if (string.IsNullOrWhiteSpace(snapshotCustomizationGroupName) ||
            string.IsNullOrWhiteSpace(snapshotChoiceName))
        {
            return Result.Failure<OrderItemCustomization>(OrderErrors.OrderItemCustomizationInvalid);
        }

        return new OrderItemCustomization(
            snapshotCustomizationGroupName,
            snapshotChoiceName,
            snapshotChoicePriceAdjustmentAtOrder);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Snapshot_CustomizationGroupName;
        yield return Snapshot_ChoiceName;
        yield return Snapshot_ChoicePriceAdjustmentAtOrder;
    }

#pragma warning disable CS8618
    private OrderItemCustomization() { }
#pragma warning restore CS8618
}
