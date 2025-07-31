using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Infrastructure;
using YummyZoom.Application.RoleAssignments.Commands.CreateRoleAssignment;
using YummyZoom.Application.RoleAssignments.Queries.GetRestaurantRoleAssignments;
using YummyZoom.Domain.RoleAssignmentAggregate.Enums;
using YummyZoom.SharedKernel.Constants;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.RoleAssignments.Queries;

public class GetRestaurantRoleAssignmentsTests : BaseTestFixture
{
    private Guid _userId1;
    private Guid _userId2;
    private Guid _restaurantId;

    [SetUp]
    public async Task SetUp()
    {
        // Ensure required roles exist in the database
        await EnsureRolesExistAsync(Roles.RestaurantOwner, Roles.Administrator);
        
        // Create administrator users for testing
        _userId1 = await RunAsUserAsync("user5@example.com", TestConfiguration.DefaultUsers.CommonTestPassword, new[] { Roles.Administrator });
        _userId2 = await RunAsUserAsync("user6@example.com", TestConfiguration.DefaultUsers.CommonTestPassword, new[] { Roles.Administrator });
        _restaurantId = Guid.NewGuid();
        
        // Create role assignments for multiple users in the same restaurant
        await SendAsync(new CreateRoleAssignmentCommand(_userId1, _restaurantId, RestaurantRole.Owner));
        await SendAsync(new CreateRoleAssignmentCommand(_userId2, _restaurantId, RestaurantRole.Staff));
    }

    [Test]
    public async Task GetRestaurantRoleAssignments_WithExistingAssignments_ShouldReturnAllAssignmentsForRestaurant()
    {
        // Arrange
        // 1. Simulate any authenticated user (query doesn't require specific authorization)
        await RunAsUserAsync("query@example.com", TestConfiguration.DefaultUsers.CommonTestPassword, new[] { Roles.User });
        
        // 2. Create query for the restaurant that has role assignments
        var query = new GetRestaurantRoleAssignmentsQuery(_restaurantId);

        // Act
        // 3. Send the query
        var result = await SendAsync(query);

        // Assert
        // 4. Verify the query execution was successful
        result.ShouldBeSuccessful();
        
        // 5. Verify all role assignments for the restaurant are returned
        result.Value.RoleAssignments.Should().HaveCount(2);
        result.Value.RoleAssignments.Select(x => x.UserId).Should().Contain(_userId1);
        result.Value.RoleAssignments.Select(x => x.UserId).Should().Contain(_userId2);
        result.Value.RoleAssignments.Select(x => x.RestaurantId).Should().AllBeEquivalentTo(_restaurantId);
        
        // 6. Verify the specific roles are correct
        var ownerAssignment = result.Value.RoleAssignments.First(x => x.UserId == _userId1);
        ownerAssignment.Role.Should().Be(RestaurantRole.Owner);
        
        var staffAssignment = result.Value.RoleAssignments.First(x => x.UserId == _userId2);
        staffAssignment.Role.Should().Be(RestaurantRole.Staff);
    }

    [Test]
    public async Task GetRestaurantRoleAssignments_WithNoAssignments_ShouldReturnEmptyList()
    {
        // Arrange
        // 1. Simulate any authenticated user
        await RunAsUserAsync("query2@example.com", TestConfiguration.DefaultUsers.CommonTestPassword, new[] { Roles.User });
        
        // 2. Create query for a restaurant with no role assignments
        var restaurantWithNoAssignments = Guid.NewGuid();
        var query = new GetRestaurantRoleAssignmentsQuery(restaurantWithNoAssignments);

        // Act
        // 3. Send the query
        var result = await SendAsync(query);

        // Assert
        // 4. Verify the query execution was successful
        result.ShouldBeSuccessful();
        
        // 5. Verify an empty list is returned for restaurant with no assignments
        result.Value.RoleAssignments.Should().BeEmpty();
    }
}
