using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.RoleAssignmentAggregate.Enums;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Infrastructure.Identity;

public class YummyZoomClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole<Guid>>
{
    private readonly IRoleAssignmentRepository _roleAssignmentRepository;

    public YummyZoomClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IOptions<IdentityOptions> optionsAccessor,
        IRoleAssignmentRepository roleAssignmentRepository)
        : base(userManager, roleManager, optionsAccessor)
    {
        _roleAssignmentRepository = roleAssignmentRepository;
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        // Add restaurant permissions
        var domainUserId = UserId.Create(user.Id);
        var roleAssignments = await _roleAssignmentRepository.GetByUserIdAsync(domainUserId);
        foreach (var assignment in roleAssignments)
        {
            // Map domain enum to role constants
            var roleConstant = assignment.Role switch
            {
                RestaurantRole.Owner => Roles.RestaurantOwner,
                RestaurantRole.Staff => Roles.RestaurantStaff,
                _ => assignment.Role.ToString()
            };
            
            var claimValue = $"{roleConstant}:{assignment.RestaurantId.Value}";
            identity.AddClaim(new Claim("permission", claimValue));
        }

        // Add user self-ownership permission
        identity.AddClaim(new Claim("permission", $"{Roles.UserOwner}:{user.Id}"));
        
        // Add admin permissions if user is administrator
        if (await UserManager.IsInRoleAsync(user, Roles.Administrator))
        {
            identity.AddClaim(new Claim("permission", $"{Roles.UserAdmin}:*"));
        }

        return identity;
    }
} 
