using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.RoleAssignmentAggregate;
using YummyZoom.Domain.RoleAssignmentAggregate.ValueObjects;
using YummyZoom.Domain.RoleAssignmentAggregate.Enums;
using YummyZoom.Domain.RoleAssignmentAggregate.Errors;
using YummyZoom.Domain.RoleAssignmentAggregate.Events;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.RoleAssignmentAggregate;

[TestFixture]
public class RoleAssignmentTests
{
    private static readonly UserId DefaultUserId = UserId.CreateUnique();
    private static readonly RestaurantId DefaultRestaurantId = RestaurantId.CreateUnique();
    private const RestaurantRole DefaultRole = RestaurantRole.Owner;

    // Helper method to create a valid RoleAssignment for testing
    private static RoleAssignment CreateValidRoleAssignment(
        UserId? userId = null,
        RestaurantId? restaurantId = null,
        RestaurantRole role = DefaultRole)
    {
        var result = RoleAssignment.Create(
            userId ?? DefaultUserId,
            restaurantId ?? DefaultRestaurantId,
            role);
        
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    [Test]
    public void Create_WithValidInputs_ShouldSucceedAndInitializeRoleAssignmentCorrectly()
    {
        // Arrange
        var userId = UserId.CreateUnique();
        var restaurantId = RestaurantId.CreateUnique();
        var role = RestaurantRole.Owner;

        // Act
        var result = RoleAssignment.Create(userId, restaurantId, role);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        var roleAssignment = result.Value;

        roleAssignment.Id.Value.Should().NotBe(Guid.Empty, "because a unique RoleAssignmentId should be generated");
        roleAssignment.UserId.Should().Be(userId);
        roleAssignment.RestaurantId.Should().Be(restaurantId);
        roleAssignment.Role.Should().Be(role);
        roleAssignment.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        roleAssignment.UpdatedAt.Should().BeNull("because it's a new role assignment");
    }

    [Test]
    public void Create_WithStaffRole_ShouldSucceedAndSetRoleCorrectly()
    {
        // Arrange
        var userId = UserId.CreateUnique();
        var restaurantId = RestaurantId.CreateUnique();
        var role = RestaurantRole.Staff;

        // Act
        var result = RoleAssignment.Create(userId, restaurantId, role);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        var roleAssignment = result.Value;

        roleAssignment.UserId.Should().Be(userId);
        roleAssignment.RestaurantId.Should().Be(restaurantId);
        roleAssignment.Role.Should().Be(role);
    }

    [Test]
    public void Create_WithNullUserId_ShouldFailAndReturnInvalidUserIdError()
    {
        // Arrange
        UserId? userId = null;
        var restaurantId = RestaurantId.CreateUnique();
        var role = RestaurantRole.Owner;

        // Act
        var result = RoleAssignment.Create(userId!, restaurantId, role);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RoleAssignmentErrors.InvalidUserId("null"));
    }

    [Test]
    public void Create_WithNullRestaurantId_ShouldFailAndReturnInvalidRestaurantIdError()
    {
        // Arrange
        var userId = UserId.CreateUnique();
        RestaurantId? restaurantId = null;
        var role = RestaurantRole.Owner;

        // Act
        var result = RoleAssignment.Create(userId, restaurantId!, role);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RoleAssignmentErrors.InvalidRestaurantId("null"));
    }

    [Test]
    public void Create_WithInvalidRole_ShouldFailAndReturnInvalidRoleError()
    {
        // Arrange
        var userId = UserId.CreateUnique();
        var restaurantId = RestaurantId.CreateUnique();
        var invalidRole = (RestaurantRole)999; // Invalid enum value

        // Act
        var result = RoleAssignment.Create(userId, restaurantId, invalidRole);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RoleAssignmentErrors.InvalidRole(invalidRole.ToString()));
    }

    [Test]
    public void Create_WithSpecificId_ShouldSucceedAndUseProvidedId()
    {
        // Arrange
        var roleAssignmentId = RoleAssignmentId.CreateUnique();
        var userId = UserId.CreateUnique();
        var restaurantId = RestaurantId.CreateUnique();
        var role = RestaurantRole.Owner;
        var createdAt = DateTime.UtcNow.AddDays(-1);
        var updatedAt = DateTime.UtcNow;

        // Act
        var result = RoleAssignment.Create(roleAssignmentId, userId, restaurantId, role, createdAt, updatedAt);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        var roleAssignment = result.Value;

        roleAssignment.Id.Should().Be(roleAssignmentId);
        roleAssignment.UserId.Should().Be(userId);
        roleAssignment.RestaurantId.Should().Be(restaurantId);
        roleAssignment.Role.Should().Be(role);
        roleAssignment.CreatedAt.Should().Be(createdAt);
        roleAssignment.UpdatedAt.Should().Be(updatedAt);
    }

    [Test]
    public void Create_WithSpecificIdButNullId_ShouldFailAndReturnInvalidRoleAssignmentIdError()
    {
        // Arrange
        RoleAssignmentId? roleAssignmentId = null;
        var userId = UserId.CreateUnique();
        var restaurantId = RestaurantId.CreateUnique();
        var role = RestaurantRole.Owner;
        var createdAt = DateTime.UtcNow;

        // Act
        var result = RoleAssignment.Create(roleAssignmentId!, userId, restaurantId, role, createdAt);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RoleAssignmentErrors.InvalidRoleAssignmentId("null"));
    }

    [Test]
    public void UpdateRole_WithValidNewRole_ShouldUpdateRoleAndSetUpdatedAt()
    {
        // Arrange
        var roleAssignment = CreateValidRoleAssignment(role: RestaurantRole.Owner);
        var originalCreatedAt = roleAssignment.CreatedAt;
        var newRole = RestaurantRole.Staff;

        // Act
        var result = roleAssignment.UpdateRole(newRole);

        // Assert
        result.IsSuccess.Should().BeTrue();
        roleAssignment.Role.Should().Be(newRole);
        roleAssignment.UpdatedAt.Should().NotBeNull();
        roleAssignment.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        roleAssignment.CreatedAt.Should().Be(originalCreatedAt, "CreatedAt should not change");
    }

    [Test]
    public void UpdateRole_WithSameRole_ShouldSucceedWithoutChanges()
    {
        // Arrange
        var roleAssignment = CreateValidRoleAssignment(role: RestaurantRole.Owner);
        var originalUpdatedAt = roleAssignment.UpdatedAt;
        var sameRole = RestaurantRole.Owner;

        // Act
        var result = roleAssignment.UpdateRole(sameRole);

        // Assert
        result.IsSuccess.Should().BeTrue();
        roleAssignment.Role.Should().Be(sameRole);
        roleAssignment.UpdatedAt.Should().Be(originalUpdatedAt, "UpdatedAt should not change when role is the same");
    }

    [Test]
    public void UpdateRole_WithInvalidRole_ShouldFailAndReturnInvalidRoleError()
    {
        // Arrange
        var roleAssignment = CreateValidRoleAssignment();
        var originalRole = roleAssignment.Role;
        var originalUpdatedAt = roleAssignment.UpdatedAt;
        var invalidRole = (RestaurantRole)999;

        // Act
        var result = roleAssignment.UpdateRole(invalidRole);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RoleAssignmentErrors.InvalidRole(invalidRole.ToString()));
        roleAssignment.Role.Should().Be(originalRole, "Role should not change on failure");
        roleAssignment.UpdatedAt.Should().Be(originalUpdatedAt, "UpdatedAt should not change on failure");
    }

    [Test]
    public void Matches_WithMatchingCriteria_ShouldReturnTrue()
    {
        // Arrange
        var userId = UserId.CreateUnique();
        var restaurantId = RestaurantId.CreateUnique();
        var role = RestaurantRole.Staff;
        var roleAssignment = CreateValidRoleAssignment(userId, restaurantId, role);

        // Act
        var result = roleAssignment.Matches(userId, restaurantId, role);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void Matches_WithDifferentUserId_ShouldReturnFalse()
    {
        // Arrange
        var userId = UserId.CreateUnique();
        var differentUserId = UserId.CreateUnique();
        var restaurantId = RestaurantId.CreateUnique();
        var role = RestaurantRole.Staff;
        var roleAssignment = CreateValidRoleAssignment(userId, restaurantId, role);

        // Act
        var result = roleAssignment.Matches(differentUserId, restaurantId, role);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void Matches_WithDifferentRestaurantId_ShouldReturnFalse()
    {
        // Arrange
        var userId = UserId.CreateUnique();
        var restaurantId = RestaurantId.CreateUnique();
        var differentRestaurantId = RestaurantId.CreateUnique();
        var role = RestaurantRole.Staff;
        var roleAssignment = CreateValidRoleAssignment(userId, restaurantId, role);

        // Act
        var result = roleAssignment.Matches(userId, differentRestaurantId, role);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void Matches_WithDifferentRole_ShouldReturnFalse()
    {
        // Arrange
        var userId = UserId.CreateUnique();
        var restaurantId = RestaurantId.CreateUnique();
        var role = RestaurantRole.Owner;
        var differentRole = RestaurantRole.Staff;
        var roleAssignment = CreateValidRoleAssignment(userId, restaurantId, role);

        // Act
        var result = roleAssignment.Matches(userId, restaurantId, differentRole);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void IsForUserAndRestaurant_WithMatchingUserAndRestaurant_ShouldReturnTrue()
    {
        // Arrange
        var userId = UserId.CreateUnique();
        var restaurantId = RestaurantId.CreateUnique();
        var roleAssignment = CreateValidRoleAssignment(userId, restaurantId);

        // Act
        var result = roleAssignment.IsForUserAndRestaurant(userId, restaurantId);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void IsForUserAndRestaurant_WithDifferentUser_ShouldReturnFalse()
    {
        // Arrange
        var userId = UserId.CreateUnique();
        var differentUserId = UserId.CreateUnique();
        var restaurantId = RestaurantId.CreateUnique();
        var roleAssignment = CreateValidRoleAssignment(userId, restaurantId);

        // Act
        var result = roleAssignment.IsForUserAndRestaurant(differentUserId, restaurantId);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void IsForUserAndRestaurant_WithDifferentRestaurant_ShouldReturnFalse()
    {
        // Arrange
        var userId = UserId.CreateUnique();
        var restaurantId = RestaurantId.CreateUnique();
        var differentRestaurantId = RestaurantId.CreateUnique();
        var roleAssignment = CreateValidRoleAssignment(userId, restaurantId);

        // Act
        var result = roleAssignment.IsForUserAndRestaurant(userId, differentRestaurantId);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void MarkForRemoval_ShouldRaiseDomainEvent()
    {
        // Arrange
        var roleAssignment = CreateValidRoleAssignment();
        var initialEventCount = roleAssignment.DomainEvents.Count;

        // Act
        roleAssignment.MarkForRemoval();

        // Assert
        roleAssignment.DomainEvents.Should().HaveCount(initialEventCount + 1);
        var removedEvent = roleAssignment.DomainEvents.Last() as RoleAssignmentRemoved;
        removedEvent.Should().NotBeNull();
        removedEvent!.RoleAssignmentId.Should().Be((RoleAssignmentId)roleAssignment.Id);
        removedEvent.UserId.Should().Be(roleAssignment.UserId);
        removedEvent.RestaurantId.Should().Be(roleAssignment.RestaurantId);
        removedEvent.Role.Should().Be(roleAssignment.Role);
    }

    [Test]
    public void Create_ShouldRaiseRoleAssignmentCreatedEvent()
    {
        // Arrange
        var userId = UserId.CreateUnique();
        var restaurantId = RestaurantId.CreateUnique();
        var role = RestaurantRole.Owner;

        // Act
        var result = RoleAssignment.Create(userId, restaurantId, role);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var roleAssignment = result.Value;
        
        roleAssignment.DomainEvents.Should().ContainSingle();
        var createdEvent = roleAssignment.DomainEvents.First() as RoleAssignmentCreated;
        createdEvent.Should().NotBeNull();
        createdEvent!.RoleAssignmentId.Should().Be((RoleAssignmentId)roleAssignment.Id);
        createdEvent.UserId.Should().Be(userId);
        createdEvent.RestaurantId.Should().Be(restaurantId);
        createdEvent.Role.Should().Be(role);
    }

    [Test]
    public void UpdateRole_ShouldRaiseBothRemovedAndCreatedEvents()
    {
        // Arrange
        var roleAssignment = CreateValidRoleAssignment(role: RestaurantRole.Owner);
        var initialEventCount = roleAssignment.DomainEvents.Count;
        var newRole = RestaurantRole.Staff;

        // Act
        var result = roleAssignment.UpdateRole(newRole);

        // Assert
        result.IsSuccess.Should().BeTrue();
        roleAssignment.DomainEvents.Should().HaveCount(initialEventCount + 2);
        
        var events = roleAssignment.DomainEvents.Skip(initialEventCount).ToList();
        var removedEvent = events[0] as RoleAssignmentRemoved;
        var createdEvent = events[1] as RoleAssignmentCreated;
        
        removedEvent.Should().NotBeNull();
        removedEvent!.Role.Should().Be(RestaurantRole.Owner, "because that was the old role");
        
        createdEvent.Should().NotBeNull();
        createdEvent!.Role.Should().Be(newRole, "because that's the new role");
    }
}
