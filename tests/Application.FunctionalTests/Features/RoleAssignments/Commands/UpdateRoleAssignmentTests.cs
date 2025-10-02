using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Infrastructure;
using YummyZoom.Application.RoleAssignments.Commands.CreateRoleAssignment;
using YummyZoom.Application.RoleAssignments.Commands.UpdateRoleAssignment;
using YummyZoom.Domain.RoleAssignmentAggregate;
using YummyZoom.Domain.RoleAssignmentAggregate.Enums;
using YummyZoom.Domain.RoleAssignmentAggregate.ValueObjects;
using YummyZoom.SharedKernel.Constants;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.RoleAssignments.Commands;

public class UpdateRoleAssignmentTests : BaseTestFixture
{
    private Guid _userId;
    private Guid _restaurantId;
    private Guid _roleAssignmentId;

    [SetUp]
    public async Task SetUp()
    {
        // Ensure required roles exist in the database
        await EnsureRolesExistAsync(Roles.RestaurantOwner, Roles.Administrator);

        // Create administrator user for testing (required for both create and update operations)
        _userId = await RunAsUserAsync("user2@example.com", TestConfiguration.DefaultUsers.CommonTestPassword, new[] { Roles.Administrator });
        _restaurantId = Guid.NewGuid();

        // Create a role assignment to use in update tests
        var createCommand = new CreateRoleAssignmentCommand(_userId, _restaurantId, RestaurantRole.Owner);
        var createResult = await SendAsync(createCommand);
        _roleAssignmentId = createResult.Value.RoleAssignmentId;
    }

    [Test]
    public async Task UpdateRoleAssignment_AsAdministrator_ShouldSucceedAndUpdateRole()
    {
        // Arrange
        // 1. Simulate administrator login (required for UpdateRoleAssignmentCommand authorization)
        await RunAsAdministratorAsync();

        // 2. Create command to update role from Owner to Staff
        var command = new UpdateRoleAssignmentCommand(_roleAssignmentId, RestaurantRole.Staff);

        // Act
        // 3. Send the update command
        var result = await SendAsync(command);

        // Assert
        // 4. Verify the command execution was successful
        result.ShouldBeSuccessful();

        // 5. Verify the role assignment was updated correctly in the database
        var roleAssignmentId = RoleAssignmentId.Create(_roleAssignmentId);
        var updatedAssignment = await FindAsync<RoleAssignment>(roleAssignmentId);
        updatedAssignment.Should().NotBeNull();
        updatedAssignment!.Role.Should().Be(RestaurantRole.Staff);
    }

    [Test]
    public async Task UpdateRoleAssignment_WithNonExistentAssignment_ShouldFailWithNotFoundError()
    {
        // Arrange
        // 1. Simulate administrator login
        await RunAsAdministratorAsync();

        // 2. Create command with a non-existent role assignment ID
        var nonExistentRoleAssignmentId = Guid.NewGuid();
        var command = new UpdateRoleAssignmentCommand(nonExistentRoleAssignmentId, RestaurantRole.Staff);

        // Act
        // 3. Send the update command
        var result = await SendAsync(command);

        // Assert
        // 4. Verify the command fails due to non-existent role assignment
        result.ShouldBeFailure();
    }

    [Test]
    public async Task UpdateRoleAssignment_WithInvalidRole_ShouldFailWithValidationError()
    {
        // Arrange
        // 1. Simulate administrator login
        await RunAsAdministratorAsync();

        // 2. Create command with invalid role value
        var command = new UpdateRoleAssignmentCommand(_roleAssignmentId, (RestaurantRole)999);

        // Act
        // 3. Send the update command
        Func<Task> act = () => SendAsync(command);

        // Assert
        // 4. Verify the command fails due to invalid role
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task UpdateRoleAssignment_WithSameRole_ShouldSucceedAsNoOperation()
    {
        // Arrange
        // 1. Simulate administrator login
        await RunAsAdministratorAsync();

        // 2. Create command to update role to the same value (Owner -> Owner)
        var command = new UpdateRoleAssignmentCommand(_roleAssignmentId, RestaurantRole.Owner);

        // Act
        // 3. Send the update command
        var result = await SendAsync(command);

        // Assert
        // 4. Verify the command succeeds (no-op is valid)
        result.ShouldBeSuccessful();

        // 5. Verify the role assignment remains unchanged in the database
        var roleAssignmentId = RoleAssignmentId.Create(_roleAssignmentId);
        var assignment = await FindAsync<RoleAssignment>(roleAssignmentId);
        assignment.Should().NotBeNull();
        assignment!.Role.Should().Be(RestaurantRole.Owner);
    }
}
