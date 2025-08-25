using YummyZoom.Domain.OrderAggregate.ValueObjects;

namespace YummyZoom.Application.Common.Authorization;

/// <summary>
/// Marker interface for commands that operate on a specific order resource.
/// Enables resource-based authorization using the order ID.
/// </summary>
public interface IOrderCommand : IContextualCommand
{
    OrderId OrderId { get; }
    
    string IContextualCommand.ResourceType => "Order";
    string IContextualCommand.ResourceId => OrderId.Value.ToString();
}
