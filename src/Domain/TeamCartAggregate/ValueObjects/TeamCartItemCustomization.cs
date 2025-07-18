using YummyZoom.Domain.Common.Models;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.TeamCartAggregate.ValueObjects;

/// <summary>
/// Represents a snapshot of a customization choice for a team cart item.
/// This value object captures customization data at the time of adding to cart
/// to ensure price consistency and proper conversion to orders.
/// </summary>
public sealed class TeamCartItemCustomization : ValueObject
{
    /// <summary>
    /// Gets the snapshot of the customization group name at the time of adding to cart.
    /// </summary>
    public string Snapshot_CustomizationGroupName { get; private set; }

    /// <summary>
    /// Gets the snapshot of the choice name at the time of adding to cart.
    /// </summary>
    public string Snapshot_ChoiceName { get; private set; }

    /// <summary>
    /// Gets the snapshot of the choice price adjustment at the time of adding to cart.
    /// </summary>
    public Money Snapshot_ChoicePriceAdjustmentAtOrder { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TeamCartItemCustomization"/> class.
    /// </summary>
    /// <param name="snapshotCustomizationGroupName">The snapshot of the customization group name.</param>
    /// <param name="snapshotChoiceName">The snapshot of the choice name.</param>
    /// <param name="snapshotChoicePriceAdjustmentAtOrder">The snapshot of the price adjustment.</param>
    private TeamCartItemCustomization(
        string snapshotCustomizationGroupName,
        string snapshotChoiceName,
        Money snapshotChoicePriceAdjustmentAtOrder)
    {
        Snapshot_CustomizationGroupName = snapshotCustomizationGroupName;
        Snapshot_ChoiceName = snapshotChoiceName;
        Snapshot_ChoicePriceAdjustmentAtOrder = snapshotChoicePriceAdjustmentAtOrder;
    }

    /// <summary>
    /// Required for ORM (e.g., Entity Framework Core) and deserialization.
    /// </summary>
#pragma warning disable CS8618
    private TeamCartItemCustomization() { }
#pragma warning restore CS8618

    /// <summary>
    /// Creates a new team cart item customization.
    /// </summary>
    /// <param name="snapshotCustomizationGroupName">The snapshot of the customization group name.</param>
    /// <param name="snapshotChoiceName">The snapshot of the choice name.</param>
    /// <param name="snapshotChoicePriceAdjustmentAtOrder">The snapshot of the price adjustment.</param>
    /// <returns>A result containing the new customization if successful, or an error if validation fails.</returns>
    public static Result<TeamCartItemCustomization> Create(
        string snapshotCustomizationGroupName,
        string snapshotChoiceName,
        Money snapshotChoicePriceAdjustmentAtOrder)
    {
        if (string.IsNullOrWhiteSpace(snapshotCustomizationGroupName))
        {
            return Result.Failure<TeamCartItemCustomization>(TeamCartErrors.InvalidCustomization);
        }

        if (string.IsNullOrWhiteSpace(snapshotChoiceName))
        {
            return Result.Failure<TeamCartItemCustomization>(TeamCartErrors.InvalidCustomization);
        }

        return Result.Success(new TeamCartItemCustomization(
            snapshotCustomizationGroupName,
            snapshotChoiceName,
            snapshotChoicePriceAdjustmentAtOrder));
    }

    /// <summary>
    /// Converts this team cart item customization to an order item customization.
    /// This method is used during the conversion from team cart to order.
    /// </summary>
    /// <returns>An <see cref="OrderItemCustomization"/> representing this customization.</returns>
    public OrderItemCustomization ToOrderItemCustomization()
    {
        return OrderItemCustomization.Create(
            Snapshot_CustomizationGroupName,
            Snapshot_ChoiceName,
            Snapshot_ChoicePriceAdjustmentAtOrder).Value;
    }

    /// <summary>
    /// Gets the equality components for value object comparison.
    /// </summary>
    /// <returns>An enumerable of objects used for equality comparison.</returns>
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Snapshot_CustomizationGroupName;
        yield return Snapshot_ChoiceName;
        yield return Snapshot_ChoicePriceAdjustmentAtOrder;
    }
}
