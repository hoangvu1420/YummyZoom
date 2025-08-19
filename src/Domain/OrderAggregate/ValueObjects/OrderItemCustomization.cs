using System.Text.Json.Serialization;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.OrderAggregate.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.OrderAggregate.ValueObjects;

public sealed class OrderItemCustomization : ValueObject
{
    public string Snapshot_CustomizationGroupName { get; private set; }
    public string Snapshot_ChoiceName { get; private set; }
    public Money Snapshot_ChoicePriceAdjustmentAtOrder { get; private set; }

    [JsonConstructor]
    private OrderItemCustomization(
        string snapshot_CustomizationGroupName,
        string snapshot_ChoiceName,
        Money snapshot_ChoicePriceAdjustmentAtOrder)
    {
        Snapshot_CustomizationGroupName = snapshot_CustomizationGroupName;
        Snapshot_ChoiceName = snapshot_ChoiceName;
        Snapshot_ChoicePriceAdjustmentAtOrder = snapshot_ChoicePriceAdjustmentAtOrder;
    }

    public static Result<OrderItemCustomization> Create(
        string snapshot_CustomizationGroupName,
        string snapshot_ChoiceName,
        Money snapshot_ChoicePriceAdjustmentAtOrder)
    {
        if (string.IsNullOrWhiteSpace(snapshot_CustomizationGroupName) ||
            string.IsNullOrWhiteSpace(snapshot_ChoiceName))
        {
            return Result.Failure<OrderItemCustomization>(OrderErrors.OrderItemCustomizationInvalid);
        }

        return new OrderItemCustomization(
            snapshot_CustomizationGroupName,
            snapshot_ChoiceName,
            snapshot_ChoicePriceAdjustmentAtOrder);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Snapshot_CustomizationGroupName;
        yield return Snapshot_ChoiceName;
        yield return Snapshot_ChoicePriceAdjustmentAtOrder;
    }

#pragma warning disable CS8618
    // Internal parameterless constructor for EF Core and JSON deserialization
    internal OrderItemCustomization() { }
#pragma warning restore CS8618
}
