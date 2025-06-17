using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Domain.RoleAssignmentAggregate.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace YummyZoom.Application.FunctionalTests.Authorization;

using static Testing;

[TestFixture]
public class MixedResourceAuthorizationTests : BaseTestFixture
{
    private Guid _restaurantId1;
    private Guid _restaurantId2;
    private Guid _testUserId1;
    private Guid _testUserId2;

    [SetUp]
    public async Task SetUp()
    {
        await ResetState();
        await SetupForAuthorizationTestsAsync();
        
        // Create test restaurant and user IDs
        _restaurantId1 = Guid.NewGuid();
        _restaurantId2 = Guid.NewGuid();
        _testUserId1 = Guid.NewGuid();
        _testUserId2 = Guid.NewGuid();
    }

    #region Mixed Command Authorization Tests

    [Test]
    public async Task MixedCommands_UserWithRestaurantRole_ShouldHaveCorrectPermissions()
    {
        // Arrange - User has restaurant role but accessing both restaurant and user resources
        var userId = await RunAsRestaurantOwnerAsync("mixed@test.com", _restaurantId1);
        
        // Create commands for different resource types
        var restaurantCommand = new TestRestaurantOwnerCommand(_restaurantId1);     // Should succeed
        var wrongRestaurantCommand = new TestRestaurantOwnerCommand(_restaurantId2); // Should fail
        var ownUserCommand = new TestUserOwnerCommand(userId);                     // Should succeed
        var otherUserCommand = new TestUserOwnerCommand(_testUserId1);             // Should fail

        // Act & Assert - Restaurant command on owned restaurant
        var result1 = await SendAsync(restaurantCommand);
        result1.ShouldBeSuccessful();

        // Act & Assert - Restaurant command on unowned restaurant
        Func<Task> act1 = () => SendAsync(wrongRestaurantCommand);
        await act1.Should().ThrowAsync<ForbiddenAccessException>();

        // Act & Assert - User command for own data
        var result2 = await SendAsync(ownUserCommand);
        result2.ShouldBeSuccessful();

        // Act & Assert - User command for other user's data
        Func<Task> act2 = () => SendAsync(otherUserCommand);
        await act2.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task MixedCommands_AdminUser_ShouldHaveAllPermissions()
    {
        // Arrange - Admin should have access to all resources
        var adminUserId = await RunAsAdministratorAsync();
        
        // Create commands for different resource types
        var userCommand1 = new TestUserOwnerCommand(_testUserId1);
        var userCommand2 = new TestUserOwnerCommand(_testUserId2);
        var ownUserCommand = new TestUserOwnerCommand(adminUserId);

        // Act & Assert - Admin should access any user data
        var result1 = await SendAsync(userCommand1);
        result1.ShouldBeSuccessful();

        var result2 = await SendAsync(userCommand2);
        result2.ShouldBeSuccessful();

        var result3 = await SendAsync(ownUserCommand);
        result3.ShouldBeSuccessful();
    }

    [Test]
    public async Task MixedCommands_RegularUser_ShouldOnlyAccessOwnData()
    {
        // Arrange - Regular user with no special roles
        var userId = await RunAsDefaultUserAsync();
        
        // Create commands
        var ownUserCommand = new TestUserOwnerCommand(userId);        // Should succeed
        var otherUserCommand = new TestUserOwnerCommand(_testUserId1); // Should fail
        var restaurantCommand = new TestRestaurantOwnerCommand(_restaurantId1); // Should fail

        // Act & Assert - Own user data should succeed
        var result1 = await SendAsync(ownUserCommand);
        result1.ShouldBeSuccessful();

        // Act & Assert - Other user data should fail
        Func<Task> act1 = () => SendAsync(otherUserCommand);
        await act1.Should().ThrowAsync<ForbiddenAccessException>();

        // Act & Assert - Restaurant data should fail
        Func<Task> act2 = () => SendAsync(restaurantCommand);
        await act2.Should().ThrowAsync<ForbiddenAccessException>();
    }

    #endregion

    #region Complex Multi-Role Scenarios

    [Test]
    public async Task ComplexScenario_UserWithMultipleRolesAndUserAccess_ShouldWorkCorrectly()
    {
        // Arrange - User with multiple restaurant roles + user access
        var userId = await RunAsUserWithMultipleRestaurantRolesAsync("complex@test.com", new[]
        {
            (_restaurantId1, RestaurantRole.Owner),
            (_restaurantId2, RestaurantRole.Staff)
        });

        // Act & Assert - Restaurant permissions
        
        // Should succeed - owner of restaurant 1
        var ownerCommand1 = new TestRestaurantOwnerCommand(_restaurantId1);
        var result1 = await SendAsync(ownerCommand1);
        result1.ShouldBeSuccessful();

        // Should succeed - owner can do staff actions in restaurant 1
        var staffCommand1 = new TestRestaurantStaffCommand(_restaurantId1);
        var result2 = await SendAsync(staffCommand1);
        result2.ShouldBeSuccessful();

        // Should fail - only staff of restaurant 2, not owner
        var ownerCommand2 = new TestRestaurantOwnerCommand(_restaurantId2);
        Func<Task> act1 = () => SendAsync(ownerCommand2);
        await act1.Should().ThrowAsync<ForbiddenAccessException>();

        // Should succeed - staff of restaurant 2
        var staffCommand2 = new TestRestaurantStaffCommand(_restaurantId2);
        var result3 = await SendAsync(staffCommand2);
        result3.ShouldBeSuccessful();

        // Act & Assert - User permissions
        
        // Should succeed - own user data
        var ownUserCommand = new TestUserOwnerCommand(userId);
        var result4 = await SendAsync(ownUserCommand);
        result4.ShouldBeSuccessful();

        // Should fail - other user data
        var otherUserCommand = new TestUserOwnerCommand(_testUserId1);
        Func<Task> act2 = () => SendAsync(otherUserCommand);
        await act2.Should().ThrowAsync<ForbiddenAccessException>();
    }

    #endregion

    #region Performance and Edge Cases

    [Test]
    public async Task MixedCommands_LargeNumberOfClaims_ShouldPerformWell()
    {
        // This test ensures the generic handler performs well with many different claim types
        
        // Arrange - User with both restaurant roles and user permissions
        var userId = await RunAsUserWithMultipleRestaurantRolesAsync("performance@test.com", new[]
        {
            (_restaurantId1, RestaurantRole.Owner),
            (_restaurantId2, RestaurantRole.Staff)
        });

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act - Execute multiple commands of different types
        var tasks = new List<Task>
        {
            SendAsync(new TestRestaurantOwnerCommand(_restaurantId1)),
            SendAsync(new TestRestaurantStaffCommand(_restaurantId1)),
            SendAsync(new TestRestaurantStaffCommand(_restaurantId2)),
            SendAsync(new TestUserOwnerCommand(userId)),
            SendAsync(new TestUnprotectedUserCommand(userId)),
            SendAsync(new TestUnprotectedCommand(_restaurantId1))
        };

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert - Should complete in reasonable time (less than 5 seconds for all commands)
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, 
            "Mixed authorization commands should execute efficiently");
    }

    #endregion
} 
