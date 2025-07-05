using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.RoleAssignmentAggregate.ValueObjects;
using YummyZoom.Domain.RoleAssignmentAggregate.Enums;
using YummyZoom.Domain.RoleAssignmentAggregate.Errors;
using YummyZoom.Domain.RoleAssignmentAggregate.Events;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.RoleAssignmentAggregate;

/// <summary>
/// RoleAssignment Aggregate Root
/// 
/// A dedicated aggregate that explicitly links a User to a Restaurant with a specific role. 
/// This is the authoritative source for determining a user's permissions and responsibilities 
/// within the context of a restaurant.
/// 
/// Invariants:
/// - The combination of UserID, RestaurantID, and Role must be unique 
///   (A user cannot be assigned the same role twice for the same restaurant)
/// - A RoleAssignment must contain a valid, non-null UserID and RestaurantID
/// - The Role must be a valid value from the RestaurantRole enum
/// </summary>
public sealed class RoleAssignment : AggregateRoot<RoleAssignmentId, Guid>
{
    public UserId UserId { get; private set; }
    public RestaurantId RestaurantId { get; private set; }
    public RestaurantRole Role { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private RoleAssignment(
        RoleAssignmentId id,
        UserId userId,
        RestaurantId restaurantId,
        RestaurantRole role,
        DateTime createdAt)
        : base(id)
    {
        UserId = userId;
        RestaurantId = restaurantId;
        Role = role;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Creates a new role assignment for a user in a restaurant
    /// </summary>
    /// <param name="userId">The ID of the user to assign the role to</param>
    /// <param name="restaurantId">The ID of the restaurant</param>
    /// <param name="role">The role to assign</param>
    /// <returns>A Result containing the new RoleAssignment or an error</returns>
    public static Result<RoleAssignment> Create(
        UserId userId,
        RestaurantId restaurantId,
        RestaurantRole role)
    {
        if (userId is null)
        {
            return Result.Failure<RoleAssignment>(RoleAssignmentErrors.InvalidUserId("null"));
        }

        if (restaurantId is null)
        {
            return Result.Failure<RoleAssignment>(RoleAssignmentErrors.InvalidRestaurantId("null"));
        }

        if (!Enum.IsDefined(role))
        {
            return Result.Failure<RoleAssignment>(RoleAssignmentErrors.InvalidRole(role.ToString()));
        }

        var roleAssignment = new RoleAssignment(
            RoleAssignmentId.CreateUnique(),
            userId,
            restaurantId,
            role,
            DateTime.UtcNow);

        // Raise domain event
        roleAssignment.AddDomainEvent(new RoleAssignmentCreated(
            (RoleAssignmentId)roleAssignment.Id,
            userId,
            restaurantId,
            role));

        return Result.Success(roleAssignment);
    }

    /// <summary>
    /// Factory method for creating RoleAssignment with specific ID (for persistence/reconstruction)
    /// </summary>
    public static Result<RoleAssignment> Create(
        RoleAssignmentId id,
        UserId userId,
        RestaurantId restaurantId,
        RestaurantRole role,
        DateTime createdAt,
        DateTime? updatedAt = null)
    {
        if (id is null)
        {
            return Result.Failure<RoleAssignment>(RoleAssignmentErrors.InvalidRoleAssignmentId("null"));
        }

        if (userId is null)
        {
            return Result.Failure<RoleAssignment>(RoleAssignmentErrors.InvalidUserId("null"));
        }

        if (restaurantId is null)
        {
            return Result.Failure<RoleAssignment>(RoleAssignmentErrors.InvalidRestaurantId("null"));
        }

        if (!Enum.IsDefined(role))
        {
            return Result.Failure<RoleAssignment>(RoleAssignmentErrors.InvalidRole(role.ToString()));
        }

        var roleAssignment = new RoleAssignment(id, userId, restaurantId, role, createdAt)
        {
            UpdatedAt = updatedAt
        };

        return Result.Success(roleAssignment);
    }

    /// <summary>
    /// Updates the role for this assignment
    /// </summary>
    /// <param name="newRole">The new role to assign</param>
    /// <returns>A Result indicating success or failure</returns>
    public Result UpdateRole(RestaurantRole newRole)
    {
        if (!Enum.IsDefined(typeof(RestaurantRole), newRole))
        {
            return Result.Failure(RoleAssignmentErrors.InvalidRole(newRole.ToString()));
        }

        if (Role == newRole)
        {
            // No change needed
            return Result.Success();
        }

        var oldRole = Role;
        Role = newRole;
        UpdatedAt = DateTime.UtcNow;        // Raise domain event for the update
        AddDomainEvent(new RoleAssignmentRemoved((RoleAssignmentId)Id, UserId, RestaurantId, oldRole));
        AddDomainEvent(new RoleAssignmentCreated((RoleAssignmentId)Id, UserId, RestaurantId, newRole));

        return Result.Success();
    }

    /// <summary>
    /// Marks this role assignment for removal (used when removing the role assignment)
    /// </summary>
    public void MarkForRemoval()
    {
        AddDomainEvent(new RoleAssignmentRemoved((RoleAssignmentId)Id, UserId, RestaurantId, Role));
    }

    /// <summary>
    /// Checks if this role assignment matches the given criteria
    /// </summary>
    public bool Matches(UserId userId, RestaurantId restaurantId, RestaurantRole role)
    {
        return UserId.Value == userId.Value &&
               RestaurantId.Value == restaurantId.Value &&
               Role == role;
    }

    /// <summary>
    /// Checks if this role assignment is for the given user and restaurant
    /// </summary>
    public bool IsForUserAndRestaurant(UserId userId, RestaurantId restaurantId)
    {
        return UserId.Value == userId.Value && RestaurantId.Value == restaurantId.Value;
    }

#pragma warning disable CS8618
    // For EF Core
    private RoleAssignment()
    {
    }
#pragma warning restore CS8618
}
