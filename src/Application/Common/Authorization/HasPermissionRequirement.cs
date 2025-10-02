using Microsoft.AspNetCore.Authorization;

namespace YummyZoom.Application.Common.Authorization;

public class HasPermissionRequirement : IAuthorizationRequirement
{
    public string Role { get; }

    public HasPermissionRequirement(string role)
    {
        Role = role;
    }
}
