using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using YummyZoom.SharedKernel;
using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.RoleAssignmentAggregate;
using YummyZoom.Domain.RoleAssignmentAggregate.Enums;
using YummyZoom.Domain.UserAggregate;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.Infrastructure.Identity;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Infrastructure.Persistence.EfCore.Seeding.Seeders.IdentitySeeders;

public class RestaurantRoleSeeder : ISeeder
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IIdentityService _identityService;
    private readonly IUserAggregateRepository _userRepository;
    private readonly IRoleAssignmentRepository _roleAssignmentRepository;

    public RestaurantRoleSeeder(
        UserManager<ApplicationUser> userManager,
        IIdentityService identityService,
        IUserAggregateRepository userRepository,
        IRoleAssignmentRepository roleAssignmentRepository)
    {
        _userManager = userManager;
        _identityService = identityService;
        _userRepository = userRepository;
        _roleAssignmentRepository = roleAssignmentRepository;
    }

    public string Name => "RestaurantRole";
    public int Order => 200; // Must run after RestaurantBundleSeeder (112)

    public async Task<bool> CanSeedAsync(SeedingContext context, CancellationToken cancellationToken = default)
        => !await context.DbContext.RoleAssignments.AnyAsync(cancellationToken);

    public async Task<Result> SeedAsync(SeedingContext context, CancellationToken cancellationToken = default)
    {
        // 1. Get seeded restaurants from SharedData
        if (!context.SharedData.TryGetValue("RestaurantSlugMap", out var rawMap) ||
            rawMap is not Dictionary<string, Guid> slugMap ||
            slugMap.Count == 0)
        {
            context.Logger.LogWarning("[RestaurantRole] No RestaurantSlugMap found in SharedData. Skipping role seeding.");
            return Result.Success();
        }

        // 2. Select a subset of restaurants to seed roles for (e.g. top 3)
        var targetRestaurants = slugMap.OrderBy(x => x.Key).Take(3).ToList();
        var rolesSeeded = 0;

        foreach (var (slug, restaurantIdGuid) in targetRestaurants)
        {
            var restaurantId = RestaurantId.Create(restaurantIdGuid);

            // Seed Owner
            var ownerResult = await EnsureUserAndRoleAsync(
                context,
                restaurantId,
                role: RestaurantRole.Owner,
                email: $"owner.{slug}@yummyzoom.vn",
                name: $"Owner of {slug}",
                phone: GenerateDummyPhone(slug, "01"),
                cancellationToken);

            if (ownerResult) rolesSeeded++;

            // Seed Staff
            var staffResult = await EnsureUserAndRoleAsync(
                context,
                restaurantId,
                role: RestaurantRole.Staff,
                email: $"staff.{slug}@yummyzoom.vn",
                name: $"Staff of {slug}",
                phone: GenerateDummyPhone(slug, "02"),
                cancellationToken);

            if (staffResult) rolesSeeded++;
        }

        context.Logger.LogInformation("[RestaurantRole] Seeding completed: {Count} roles assigned across {ResCount} restaurants",
            rolesSeeded, targetRestaurants.Count);

        return Result.Success();
    }

    private async Task<bool> EnsureUserAndRoleAsync(
        SeedingContext context,
        RestaurantId restaurantId,
        RestaurantRole role,
        string email,
        string name,
        string phone,
        CancellationToken cancellationToken)
    {
        try
        {
            // A. Ensure Identity User
            var identityUser = await _userManager.FindByEmailAsync(email);
            if (identityUser is null)
            {
                identityUser = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
                var createResult = await _userManager.CreateAsync(identityUser, "Password123!");
                if (!createResult.Succeeded)
                {
                    context.Logger.LogWarning("Failed to create identity user {Email}: {Errors}", email, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                    return false;
                }

                // Assign basic User role so they can login
                await _userManager.AddToRoleAsync(identityUser, Roles.User);
                
                // Do not assign scoped restaurant roles as Identity roles.
            }

            Guid identityUserId = identityUser.Id;
            UserId userId = UserId.Create(identityUserId);

            // B. Ensure Domain User
            var domainUser = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (domainUser is null)
            {
                var createDomain = User.Create(userId, name, email, phone, isActive: true);
                if (createDomain.IsFailure)
                {
                    context.Logger.LogWarning("Failed to create domain user {Email}: {Error}", email, createDomain.Error.Description);
                    return false;
                }
                domainUser = createDomain.Value;
                domainUser.ClearDomainEvents();
                await _userRepository.AddAsync(domainUser, cancellationToken);
                await context.DbContext.SaveChangesAsync(cancellationToken);
            }

            // C. Ensure Role Assignment
            var existingAssignment = await _roleAssignmentRepository.GetByUserRestaurantRoleAsync(userId, restaurantId, role, cancellationToken);
            if (existingAssignment is null)
            {
                var assignmentResult = RoleAssignment.Create(userId, restaurantId, role);
                if (assignmentResult.IsFailure)
                {
                    context.Logger.LogWarning("Failed to create assignment {Role} for {Email}: {Error}", role, email, assignmentResult.Error.Description);
                    return false;
                }

                var assignment = assignmentResult.Value;
                assignment.ClearDomainEvents(); // Seeder doesn't need to trigger side effects typically
                await _roleAssignmentRepository.AddAsync(assignment, cancellationToken);
                await context.DbContext.SaveChangesAsync(cancellationToken);
                return true;
            }

            return false; // Already existed
        }
        catch (Exception ex)
        {
            context.Logger.LogError(ex, "Error seeding role {Role} for {Email}", role, email);
            return false;
        }
    }

    private static string GenerateDummyPhone(string slug, string suffix)
    {
        // Simple hash to get quasi-unique 7 digits
        var hash = Math.Abs(slug.GetHashCode() % 10000000);
        return $"+8499{hash:D7}{suffix}";
    }
}
