namespace YummyZoom.Domain.RoleAssignmentAggregate.Enums;

/// <summary>
/// Defines the roles that can be assigned to users within the context of a restaurant.
/// Based on the design specification, these map to the defined system roles.
/// </summary>
public enum RestaurantRole
{
    /// <summary>
    /// Owner of the restaurant with full management privileges
    /// </summary>
    Owner = 1,

    /// <summary>
    /// Staff member with operational privileges
    /// </summary>
    Staff = 2
}
