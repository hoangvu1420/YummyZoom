using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Domain.RoleAssignmentAggregate.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Infrastructure.Identity;

namespace YummyZoom.Application.FunctionalTests.Authorization;

using static Testing;

/// <summary>
/// Debug tests to verify the authorization system components are working correctly
/// </summary>
public class DebugAuthorizationTests : BaseTestFixture
{
    [Test]
    public async Task Debug_RoleAssignmentCreation_ShouldWork()
    {
        // Arrange
        await SetupForAuthorizationTestsAsync();
        var restaurantId = Guid.NewGuid();
        
        // Create a user as admin
        await RunAsAdministratorAsync();
        var userId = await RunAsUserAsync("test@test.com", "Password123!", Array.Empty<string>());
        
        // Switch back to admin to create role assignment
        await RunAsAdministratorAsync();
        
        // Act - Create role assignment
        var roleAssignmentId = await CreateRoleAssignmentAsync(userId, restaurantId, RestaurantRole.Owner);
        
        // Assert
        roleAssignmentId.Should().NotBeEmpty();
        
        // Verify role assignment exists in database
        using var scope = CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRoleAssignmentRepository>();
        var domainUserId = YummyZoom.Domain.UserAggregate.ValueObjects.UserId.Create(userId);
        var roleAssignments = await repository.GetByUserIdAsync(domainUserId);
        
        roleAssignments.Should().HaveCount(1);
        roleAssignments[0].Role.Should().Be(RestaurantRole.Owner);
        roleAssignments[0].RestaurantId.Value.Should().Be(restaurantId);
    }

    [Test]
    public async Task Debug_ClaimsGeneration_ShouldWork()
    {
        // Arrange
        await SetupForAuthorizationTestsAsync();
        var restaurantId = Guid.NewGuid();
        
        var userId = await RunAsRestaurantOwnerAsync("owner@test.com", restaurantId);
        
        // Act - Generate claims
        using var scope = CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var claimsFactory = scope.ServiceProvider.GetRequiredService<IUserClaimsPrincipalFactory<ApplicationUser>>();
        
        var user = await userManager.FindByIdAsync(userId.ToString());
        user.Should().NotBeNull();
        
        var principal = await claimsFactory.CreateAsync(user!);
        
        // Assert - Check permission claims
        var permissionClaims = principal.Claims.Where(c => c.Type == "permission").ToList();
        permissionClaims.Should().HaveCount(2, "User should have 1 restaurant role claim + 1 user ownership claim");
        permissionClaims.Should().Contain(c => c.Value == $"RestaurantOwner:{restaurantId}");
        permissionClaims.Should().Contain(c => c.Value == $"UserOwner:{userId}");
    }

    [Test]
    public async Task Debug_PolicyExistsAndHandlerRegistered_ShouldWork()
    {
        // Arrange
        using var scope = CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Authorization.IAuthorizationService>();
        var policyProvider = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider>();
        
        // Act
        var policy = await policyProvider.GetPolicyAsync("MustBeRestaurantOwner");
        
        // Assert
        policy.Should().NotBeNull("MustBeRestaurantOwner policy should be registered");
        policy!.Requirements.Should().HaveCount(1);
    }
} 
