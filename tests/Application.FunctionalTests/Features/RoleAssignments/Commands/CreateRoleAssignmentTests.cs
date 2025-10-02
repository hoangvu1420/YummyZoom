using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Infrastructure;
using YummyZoom.Application.RoleAssignments.Commands.CreateRoleAssignment;
using YummyZoom.Domain.RoleAssignmentAggregate;
using YummyZoom.Domain.RoleAssignmentAggregate.Enums;
using YummyZoom.Domain.RoleAssignmentAggregate.ValueObjects;
using YummyZoom.SharedKernel.Constants;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.RoleAssignments.Commands;

public class CreateRoleAssignmentTests : BaseTestFixture
{
    private Guid _adminUserId;
    private Guid _userId;
    private Guid _restaurantId;

    [SetUp]
    public async Task SetUp()
    {
        // Ensure required roles exist in the database
        await EnsureRolesExistAsync(Roles.RestaurantOwner, Roles.Administrator);

        // Create a regular user and administrator for testing
        _userId = await RunAsUserAsync("user@example.com", TestConfiguration.DefaultUsers.CommonTestPassword, new[] { Roles.User });
        _adminUserId = await RunAsAdministratorAsync();
        _restaurantId = Guid.NewGuid();

        // Note: Restaurant entity might be required by domain in the future
        // Uncomment and adjust the following if you have a Restaurant aggregate/entity:
        // await AddAsync(new Restaurant(RestaurantId.Create(_restaurantId), ...));
    }

    [Test]
    public async Task CreateRoleAssignment_AsAdministrator_ShouldSucceedAndCreateAssignment()
    {
        // Arrange
        // 1. Simulate administrator login (required for CreateRoleAssignmentCommand authorization)
        await RunAsAdministratorAsync();

        // 2. Create command with valid data
        var command = new CreateRoleAssignmentCommand(_userId, _restaurantId, RestaurantRole.Owner);

        // Act
        // 3. Send the command
        var result = await SendAsync(command);

        // Assert
        // 4. Verify the command execution was successful
        result.ShouldBeSuccessful();
        var createdRoleAssignmentId = result.Value.RoleAssignmentId;
        createdRoleAssignmentId.Should().NotBeEmpty();

        // 5. Verify the role assignment was persisted correctly in the database
        var roleAssignmentId = RoleAssignmentId.Create(createdRoleAssignmentId);
        var assignment = await FindAsync<RoleAssignment>(roleAssignmentId);
        assignment.Should().NotBeNull();
        assignment!.UserId.Value.Should().Be(_userId);
        assignment.RestaurantId.Value.Should().Be(_restaurantId);
        assignment.Role.Should().Be(RestaurantRole.Owner);
    }

    [Test]
    public async Task CreateRoleAssignment_WithDuplicateAssignment_ShouldFailWithConflictError()
    {
        // Arrange
        // 1. Simulate administrator login
        await RunAsAdministratorAsync();

        // 2. Create the first role assignment
        var command = new CreateRoleAssignmentCommand(_userId, _restaurantId, RestaurantRole.Owner);
        await SendAsync(command);

        // Act
        // 3. Attempt to create a duplicate role assignment
        var duplicateResult = await SendAsync(command);

        // Assert
        // 4. Verify the duplicate assignment fails
        duplicateResult.ShouldBeFailure();
    }

    [Test]
    public async Task CreateRoleAssignment_WithInvalidRole_ShouldFailValidation()
    {
        // Arrange
        // 1. Simulate administrator login
        await RunAsAdministratorAsync();

        // 2. Create command with invalid role value
        var command = new CreateRoleAssignmentCommand(_userId, _restaurantId, (RestaurantRole)999);

        // Act
        // 3. Send the command and capture the action
        Func<Task> act = () => SendAsync(command);

        // Assert
        // 4. Verify that validation exception is thrown for invalid role
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task CreateRoleAssignment_WithNonExistentUser_ShouldFailWithNotFoundError()
    {
        // Arrange
        // 1. Simulate administrator login
        await RunAsAdministratorAsync();

        // 2. Create command with a non-existent user ID
        var nonExistentUserId = Guid.NewGuid();
        var command = new CreateRoleAssignmentCommand(nonExistentUserId, _restaurantId, RestaurantRole.Owner);

        // Act
        // 3. Send the command
        var result = await SendAsync(command);

        // Assert
        // 4. Verify the command fails due to non-existent user
        result.ShouldBeFailure();
    }
}
