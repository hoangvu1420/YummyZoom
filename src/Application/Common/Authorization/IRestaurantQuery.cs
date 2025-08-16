using YummyZoom.Domain.RestaurantAggregate.ValueObjects;

namespace YummyZoom.Application.Common.Authorization;

/// <summary>
/// Contextual interface for restaurant-scoped queries enabling policy-based authorization
/// via AuthorizationBehaviour. Mirrors IRestaurantCommand to provide a unified resource model
/// (ResourceType = "Restaurant").
/// </summary>
public interface IRestaurantQuery : IContextualCommand
{
    /// <summary>
    /// Strongly-typed restaurant identifier associated with the query.
    /// </summary>
    RestaurantId RestaurantId { get; }

    // Explicit interface resource mapping
    string IContextualCommand.ResourceType => "Restaurant";
    string IContextualCommand.ResourceId => RestaurantId.Value.ToString();
}
