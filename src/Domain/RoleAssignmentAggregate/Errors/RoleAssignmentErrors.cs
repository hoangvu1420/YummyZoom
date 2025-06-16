using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.RoleAssignmentAggregate.Errors;

public static class RoleAssignmentErrors
{
    public static Error InvalidRoleAssignmentId(string value) => Error.Validation(
        "RoleAssignment.InvalidId",
        $"The role assignment ID '{value}' is not a valid GUID.");

    public static Error InvalidRestaurantId(string value) => Error.Validation(
        "RoleAssignment.InvalidRestaurantId", 
        $"The restaurant ID '{value}' is not a valid GUID.");

    public static Error InvalidUserId(string value) => Error.Validation(
        "RoleAssignment.InvalidUserId",
        $"The user ID '{value}' is not a valid GUID.");

    public static Error RoleAssignmentNotFound(Guid roleAssignmentId) => Error.NotFound(
        "RoleAssignment.NotFound",
        $"Role assignment with ID '{roleAssignmentId}' was not found.");

    public static Error DuplicateRoleAssignment(Guid userId, Guid restaurantId, string role) => Error.Conflict(
        "RoleAssignment.DuplicateAssignment",
        $"User '{userId}' is already assigned the role '{role}' for restaurant '{restaurantId}'.");

    public static Error InvalidRole(string role) => Error.Validation(
        "RoleAssignment.InvalidRole",
        $"The role '{role}' is not a valid restaurant role.");

    public static Error UserNotFound(Guid userId) => Error.NotFound(
        "RoleAssignment.UserNotFound",
        $"User with ID '{userId}' was not found.");

    public static Error RestaurantNotFound(Guid restaurantId) => Error.NotFound(
        "RoleAssignment.RestaurantNotFound",
        $"Restaurant with ID '{restaurantId}' was not found.");
}
