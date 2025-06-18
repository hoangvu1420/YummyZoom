using System.Reflection;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.Common.Authorization;
using Microsoft.AspNetCore.Authorization;
using YummyZoom.Application.Common.Interfaces.IServices;

namespace YummyZoom.Application.Common.Behaviours;

public class AuthorizationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private readonly IUser _user;
    private readonly IAuthorizationService _authorizationService;

    public AuthorizationBehaviour(
        IUser user,
        IAuthorizationService authorizationService)
    {
        _user = user;
        _authorizationService = authorizationService;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var authorizeAttributes = request.GetType().GetCustomAttributes<Security.AuthorizeAttribute>();

        IEnumerable<Security.AuthorizeAttribute> attributes = authorizeAttributes as Security.AuthorizeAttribute[] ?? authorizeAttributes.ToArray();
        if (attributes.Any())
        {
            // Get the current user's claims principal
            var principal = _user.Principal;
            
            // Must be authenticated user
            if (principal == null || !principal.Identity?.IsAuthenticated == true)
            {
                throw new UnauthorizedAccessException();
            }

            // Role-based authorization
            var authorizeAttributesWithRoles = attributes.Where(a => !string.IsNullOrWhiteSpace(a.Roles));

            if (authorizeAttributesWithRoles.Any())
            {
                var authorized = false;

                foreach (var roles in authorizeAttributesWithRoles.Select(a => a.Roles.Split(',')))
                {
                    foreach (var role in roles)
                    {
                        var isInRole = principal.IsInRole(role.Trim());
                        if (isInRole)
                        {
                            authorized = true;
                            break;
                        }
                    }
                }

                // Must be a member of at least one role in roles
                if (!authorized)
                {
                    throw new ForbiddenAccessException();
                }
            }

            // Policy-based authorization
            var authorizeAttributesWithPolicies = attributes.Where(a => !string.IsNullOrWhiteSpace(a.Policy));
            if (authorizeAttributesWithPolicies.Any())
            {
                foreach (var policy in authorizeAttributesWithPolicies.Select(a => a.Policy))
                {
                    // Pass the request as a resource for policy-based authorization if it implements IContextualCommand
                    object? resource = request is IContextualCommand ? request : null;
                    
                    var result = resource != null 
                        ? await _authorizationService.AuthorizeAsync(principal, resource, policy)
                        : await _authorizationService.AuthorizeAsync(principal, policy);

                    if (!result.Succeeded)
                    {
                        throw new ForbiddenAccessException();
                    }
                }
            }
        }

        // User is authorized / authorization not required
        return await next();
    }
}
