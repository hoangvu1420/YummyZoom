using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Infrastructure.Persistence.EfCore.Seeding.Seeders.IdentitySeeders;

public class RoleSeeder : ISeeder
{
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public RoleSeeder(RoleManager<IdentityRole<Guid>> roleManager)
    {
        _roleManager = roleManager;
    }

    public string Name => "Role";
    public int Order => 10;

    public Task<bool> CanSeedAsync(SeedingContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public async Task<Result> SeedAsync(SeedingContext context, CancellationToken cancellationToken = default)
    {
        string[] roles = [
            Roles.Administrator,
            Roles.User,
            Roles.RestaurantOwner,
            Roles.RestaurantStaff,
            Roles.TeamCartHost,
            Roles.TeamCartMember,
            Roles.OrderOwner,
            Roles.OrderManager,
            Roles.UserOwner
        ];

        foreach (var roleName in roles.Distinct())
        {
            if (_roleManager.Roles.All(r => r.Name != roleName))
            {
                var res = await _roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                if (!res.Succeeded)
                {
                    context.Logger.LogWarning("Failed to create role {Role}: {Error}", roleName, string.Join(",", res.Errors.Select(e => e.Description)));
                }
            }
        }

        return Result.Success();
    }
}
