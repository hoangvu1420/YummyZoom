using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.Common.Authorization;

public class PermissionAuthorizationHandler : AuthorizationHandler<HasPermissionRequirement, IContextualCommand>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        HasPermissionRequirement requirement,
        IContextualCommand resource)
    {
        var requiredPermission = $"{requirement.Role}:{resource.ResourceId}";

        // Check if user has the exact required permission
        if (context.User.HasClaim("permission", requiredPermission))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Apply resource-type-specific business rules
        switch (resource.ResourceType)
        {
            case "Restaurant":
                HandleRestaurantAuthorization(context, requirement, resource);
                break;

            case "User":
                HandleUserAuthorization(context, requirement, resource);
                break;

            case "Order":
                HandleOrderAuthorization(context, requirement, resource);
                break;

            case "TeamCart":
                HandleTeamCartAuthorization(context, requirement, resource);
                break;
        }

        return Task.CompletedTask;
    }

    private void HandleRestaurantAuthorization(
        AuthorizationHandlerContext context,
        HasPermissionRequirement requirement,
        IContextualCommand resource)
    {
        // Business rule: Restaurant owners can perform staff actions
        if (requirement.Role == Roles.RestaurantStaff)
        {
            var ownerPermission = $"{Roles.RestaurantOwner}:{resource.ResourceId}";
            if (context.User.HasClaim("permission", ownerPermission))
            {
                context.Succeed(requirement);
            }
        }
    }

    private void HandleUserAuthorization(
        AuthorizationHandlerContext context,
        HasPermissionRequirement requirement,
        IContextualCommand resource)
    {
        // Business rule: Users can access their own data
        if (requirement.Role == Roles.UserOwner)
        {
            var currentUserId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == resource.ResourceId)
            {
                context.Succeed(requirement);
            }
        }

        // Business rule: Admins can access any user data
        if (requirement.Role == Roles.UserOwner &&
            context.User.HasClaim("permission", $"{Roles.UserAdmin}:*"))
        {
            context.Succeed(requirement);
        }
    }

    private void HandleOrderAuthorization(
        AuthorizationHandlerContext context,
        HasPermissionRequirement requirement,
        IContextualCommand resource)
    {
        // Business rule: Order managers can perform owner actions
        if (requirement.Role == Roles.OrderOwner)
        {
            var managerPermission = $"{Roles.OrderManager}:{resource.ResourceId}";
            if (context.User.HasClaim("permission", managerPermission))
            {
                context.Succeed(requirement);
            }
        }
    }

    private void HandleTeamCartAuthorization(
        AuthorizationHandlerContext context,
        HasPermissionRequirement requirement,
        IContextualCommand resource)
    {
        // Business rule: TeamCart hosts can perform member actions
        if (requirement.Role == Roles.TeamCartMember)
        {
            var hostPermission = $"{Roles.TeamCartHost}:{resource.ResourceId}";
            if (context.User.HasClaim("permission", hostPermission))
            {
                context.Succeed(requirement);
            }
        }
    }
}
