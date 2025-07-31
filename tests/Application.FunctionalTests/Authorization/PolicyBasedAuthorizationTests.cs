using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Domain.RoleAssignmentAggregate.Enums;

namespace YummyZoom.Application.FunctionalTests.Authorization;

using static Testing;

/// <summary>
/// Tests for policy-based authorization using contextual restaurant permissions.
/// These tests verify that the claims-based authorization system properly enforces
/// restaurant-scoped permissions through MediatR pipeline authorization.
/// </summary>
public class PolicyBasedAuthorizationTests : BaseTestFixture
{
    private Guid _restaurantId1;
    private Guid _restaurantId2;

    [SetUp]
    public async Task SetUp()
    {
        await SetupForAuthorizationTestsAsync();
        
        // Create test restaurants
        _restaurantId1 = Guid.NewGuid();
        _restaurantId2 = Guid.NewGuid();
    }

    #region Restaurant Owner Policy Tests

    [Test]
    public async Task RestaurantOwnerCommand_WithOwnerRole_ShouldSucceed()
    {
        // Arrange
        await RunAsRestaurantOwnerAsync("owner@test.com", _restaurantId1);
        var command = new TestRestaurantOwnerCommand(_restaurantId1);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
    }

    [Test]
    public async Task RestaurantOwnerCommand_WithStaffRole_ShouldFail()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@test.com", _restaurantId1);
        var command = new TestRestaurantOwnerCommand(_restaurantId1);

        // Act
        Func<Task> act = () => SendAsync(command);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task RestaurantOwnerCommand_WithNoRole_ShouldFail()
    {
        // Arrange
        await RunAsDefaultUserAsync();
        var command = new TestRestaurantOwnerCommand(_restaurantId1);

        // Act
        Func<Task> act = () => SendAsync(command);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task RestaurantOwnerCommand_WithWrongRestaurant_ShouldFail()
    {
        // Arrange
        // User is owner of restaurant 1, but trying to access restaurant 2
        await RunAsRestaurantOwnerAsync("owner@test.com", _restaurantId1);
        var command = new TestRestaurantOwnerCommand(_restaurantId2);

        // Act
        Func<Task> act = () => SendAsync(command);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task RestaurantOwnerCommand_WithMultipleRestaurants_ShouldSucceedForCorrectRestaurant()
    {
        // Arrange
        var userId = await RunAsUserWithMultipleRestaurantRolesAsync("multiowner@test.com", new[]
        {
            (_restaurantId1, RestaurantRole.Owner),
            (_restaurantId2, RestaurantRole.Staff)
        });

        var command1 = new TestRestaurantOwnerCommand(_restaurantId1);
        var command2 = new TestRestaurantOwnerCommand(_restaurantId2);

        // Act & Assert
        var result1 = await SendAsync(command1);
        result1.ShouldBeSuccessful(); // Should succeed - user is owner of restaurant 1

        Func<Task> act2 = () => SendAsync(command2);
        await act2.Should().ThrowAsync<ForbiddenAccessException>(); // Should fail - user is only staff of restaurant 2
    }

    #endregion

    #region Restaurant Staff Policy Tests

    [Test]
    public async Task RestaurantStaffCommand_WithOwnerRole_ShouldSucceed()
    {
        // Arrange - Owners should be able to perform staff actions
        await RunAsRestaurantOwnerAsync("owner@test.com", _restaurantId1);
        var command = new TestRestaurantStaffCommand(_restaurantId1);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
    }

    [Test]
    public async Task RestaurantStaffCommand_WithStaffRole_ShouldSucceed()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@test.com", _restaurantId1);
        var command = new TestRestaurantStaffCommand(_restaurantId1);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
    }

    [Test]
    public async Task RestaurantStaffCommand_WithNoRole_ShouldFail()
    {
        // Arrange
        await RunAsDefaultUserAsync();
        var command = new TestRestaurantStaffCommand(_restaurantId1);

        // Act
        Func<Task> act = () => SendAsync(command);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task RestaurantStaffCommand_WithWrongRestaurant_ShouldFail()
    {
        // Arrange
        // User is staff of restaurant 1, but trying to access restaurant 2
        await RunAsRestaurantStaffAsync("staff@test.com", _restaurantId1);
        var command = new TestRestaurantStaffCommand(_restaurantId2);

        // Act
        Func<Task> act = () => SendAsync(command);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    #endregion

    #region Unauthenticated Access Tests

    [Test]
    public async Task RestaurantOwnerCommand_WithoutAuthentication_ShouldFail()
    {
        // Arrange - No user authentication
        var command = new TestRestaurantOwnerCommand(_restaurantId1);

        // Act
        Func<Task> act = () => SendAsync(command);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Test]
    public async Task RestaurantStaffCommand_WithoutAuthentication_ShouldFail()
    {
        // Arrange - No user authentication
        var command = new TestRestaurantStaffCommand(_restaurantId1);

        // Act
        Func<Task> act = () => SendAsync(command);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Test]
    public async Task UnprotectedCommand_WithoutAuthentication_ShouldSucceed()
    {
        // Arrange - No user authentication, no authorization required
        var command = new TestUnprotectedCommand(_restaurantId1);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
    }

    #endregion

    #region Complex Authorization Scenarios

    [Test]
    public async Task ComplexScenario_UserWithMultipleRoles_ShouldHaveCorrectPermissions()
    {
        // Arrange
        var userId = await RunAsUserWithMultipleRestaurantRolesAsync("complex@test.com", new[]
        {
            (_restaurantId1, RestaurantRole.Owner),
            (_restaurantId2, RestaurantRole.Staff)
        });

        // Act & Assert
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
    }

    [Test]
    public async Task Authorization_AfterRoleAssignmentCreation_ShouldWorkImmediately()
    {
        // Arrange
        var userId = await RunAsDefaultUserAsync();
        var command = new TestRestaurantOwnerCommand(_restaurantId1);

        // Verify initial state - should fail
        Func<Task> actBefore = () => SendAsync(command);
        await actBefore.Should().ThrowAsync<ForbiddenAccessException>();

        // Act - Create role assignment as admin
        await RunAsAdministratorAsync();
        await CreateRoleAssignmentAsync(userId, _restaurantId1, RestaurantRole.Owner);

        // Switch back to the user and refresh claims from database
        SetUserId(userId);
        await RefreshUserClaimsAsync();

        // Assert - Should now succeed
        var result = await SendAsync(command);
        result.ShouldBeSuccessful();
    }

    #endregion
} 
