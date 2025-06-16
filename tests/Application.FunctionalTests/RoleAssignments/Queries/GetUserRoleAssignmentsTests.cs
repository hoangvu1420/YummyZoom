using YummyZoom.Application.RoleAssignments.Commands.CreateRoleAssignment;
using YummyZoom.Application.RoleAssignments.Queries.GetUserRoleAssignments;
using YummyZoom.Domain.RoleAssignmentAggregate.Enums;
using YummyZoom.SharedKernel.Constants;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.RoleAssignments.Queries;

public class GetUserRoleAssignmentsTests : BaseTestFixture
{
    private Guid _userId;
    private Guid _restaurantId1;
    private Guid _restaurantId2;

    [SetUp]
    public async Task SetUp()
    {
        // Ensure required roles exist in the database
        await EnsureRolesExistAsync(Roles.RestaurantOwner, Roles.Administrator);
        
        // Create administrator user for testing
        _userId = await RunAsUserAsync("user4@example.com", "Password123!", new[] { Roles.Administrator });
        _restaurantId1 = Guid.NewGuid();
        _restaurantId2 = Guid.NewGuid();
        
        // Create role assignments for the user across multiple restaurants
        await SendAsync(new CreateRoleAssignmentCommand(_userId, _restaurantId1, RestaurantRole.Owner));
        await SendAsync(new CreateRoleAssignmentCommand(_userId, _restaurantId2, RestaurantRole.Staff));
    }

    [Test]
    public async Task GetUserRoleAssignments_WithExistingAssignments_ShouldReturnAllAssignmentsForUser()
    {
        // Arrange
        // 1. Simulate any authenticated user (query doesn't require specific authorization)
        await RunAsUserAsync("query@example.com", "Password123!", new[] { Roles.User });
        
        // 2. Create query for the user who has role assignments
        var query = new GetUserRoleAssignmentsQuery(_userId);

        // Act
        // 3. Send the query
        var result = await SendAsync(query);

        // Assert
        // 4. Verify the query execution was successful
        result.ShouldBeSuccessful();
        
        // 5. Verify all role assignments for the user are returned
        result.Value.RoleAssignments.Should().HaveCount(2);
        result.Value.RoleAssignments.Select(x => x.RestaurantId).Should().Contain(_restaurantId1);
        result.Value.RoleAssignments.Select(x => x.RestaurantId).Should().Contain(_restaurantId2);
        result.Value.RoleAssignments.Select(x => x.UserId).Should().AllBeEquivalentTo(_userId);
        
        // 6. Verify the specific roles are correct
        var ownerAssignment = result.Value.RoleAssignments.First(x => x.RestaurantId == _restaurantId1);
        ownerAssignment.Role.Should().Be(RestaurantRole.Owner);
        
        var staffAssignment = result.Value.RoleAssignments.First(x => x.RestaurantId == _restaurantId2);
        staffAssignment.Role.Should().Be(RestaurantRole.Staff);
    }

    [Test]
    public async Task GetUserRoleAssignments_WithNoAssignments_ShouldReturnEmptyList()
    {
        // Arrange
        // 1. Simulate any authenticated user
        await RunAsUserAsync("query2@example.com", "Password123!", new[] { Roles.User });
        
        // 2. Create query for a user with no role assignments
        var userWithNoAssignments = Guid.NewGuid();
        var query = new GetUserRoleAssignmentsQuery(userWithNoAssignments);

        // Act
        // 3. Send the query
        var result = await SendAsync(query);

        // Assert
        // 4. Verify the query execution was successful
        result.ShouldBeSuccessful();
        
        // 5. Verify an empty list is returned for user with no assignments
        result.Value.RoleAssignments.Should().BeEmpty();
    }
} 
