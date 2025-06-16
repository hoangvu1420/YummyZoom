using YummyZoom.Application.RoleAssignments.Commands.CreateRoleAssignment;
using YummyZoom.Application.RoleAssignments.Commands.DeleteRoleAssignment;
using YummyZoom.Domain.RoleAssignmentAggregate;
using YummyZoom.Domain.RoleAssignmentAggregate.Enums;
using YummyZoom.Domain.RoleAssignmentAggregate.ValueObjects;
using YummyZoom.SharedKernel.Constants;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.RoleAssignments.Commands;

public class DeleteRoleAssignmentTests : BaseTestFixture
{
    private Guid _userId;
    private Guid _restaurantId;
    private Guid _roleAssignmentId;

    [SetUp]
    public async Task SetUp()
    {
        // Ensure required roles exist in the database
        await EnsureRolesExistAsync(Roles.RestaurantOwner, Roles.Administrator);
        
        // Create administrator user for testing (required for both create and delete operations)
        _userId = await RunAsUserAsync("user3@example.com", "Password123!", new[] { Roles.Administrator });
        _restaurantId = Guid.NewGuid();
        
        // Create a role assignment to use in delete tests
        var createCommand = new CreateRoleAssignmentCommand(_userId, _restaurantId, RestaurantRole.Owner);
        var createResult = await SendAsync(createCommand);
        _roleAssignmentId = createResult.Value.RoleAssignmentId;
    }

    [Test]
    public async Task DeleteRoleAssignment_AsAdministrator_ShouldSucceedAndRemoveAssignment()
    {
        // Arrange
        // 1. Simulate administrator login (required for DeleteRoleAssignmentCommand authorization)
        await RunAsAdministratorAsync();
        
        // 2. Create delete command with existing role assignment ID
        var command = new DeleteRoleAssignmentCommand(_roleAssignmentId);

        // Act
        // 3. Send the delete command
        var result = await SendAsync(command);

        // Assert
        // 4. Verify the command execution was successful
        result.ShouldBeSuccessful();

        // 5. Verify the role assignment was removed from the database
        var roleAssignmentId = RoleAssignmentId.Create(_roleAssignmentId);
        var deletedAssignment = await FindAsync<RoleAssignment>(roleAssignmentId);
        deletedAssignment.Should().BeNull();
    }

    [Test]
    public async Task DeleteRoleAssignment_WithNonExistentAssignment_ShouldFailWithNotFoundError()
    {
        // Arrange
        // 1. Simulate administrator login
        await RunAsAdministratorAsync();
        
        // 2. Create command with a non-existent role assignment ID
        var nonExistentRoleAssignmentId = Guid.NewGuid();
        var command = new DeleteRoleAssignmentCommand(nonExistentRoleAssignmentId);

        // Act
        // 3. Send the delete command
        var result = await SendAsync(command);

        // Assert
        // 4. Verify the command fails due to non-existent role assignment
        result.ShouldBeFailure();
    }
} 
