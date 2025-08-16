using YummyZoom.Domain.OrderAggregate.ValueObjects;

namespace YummyZoom.Application.Common.Authorization;

/// <summary>
/// Contextual interface for order-scoped queries. Introduced preemptively to allow future
/// fine-grained order policies (e.g., MustBeOrderOwner) without modifying existing query records.
/// Currently no explicit policies reference ResourceType = "Order" but the interface allows the
/// authorization pipeline to receive a consistent resource shape when such policies are added.
/// </summary>
public interface IOrderQuery : IContextualCommand
{
    /// <summary>
    /// Strongly-typed order identifier associated with the query.
    /// </summary>
    OrderId OrderId { get; }

    string IContextualCommand.ResourceType => "Order";
    string IContextualCommand.ResourceId => OrderId.Value.ToString();
}
