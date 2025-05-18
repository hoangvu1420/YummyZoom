using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.UserAggregate.ValueObjects;

[TestFixture]
public class RoleAssignmentTests
{
    [Test]
    public void Create_WithValidRoleName_ShouldSucceedAndReturnRoleAssignment()
    {
        // Arrange
        var roleName = "Customer";

        // Act
        var result = RoleAssignment.Create(roleName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        var roleAssignment = result.Value;

        roleAssignment.RoleName.Should().Be(roleName);
        roleAssignment.TargetEntityId.Should().BeNull();
        roleAssignment.TargetEntityType.Should().BeNull();
    }

    [Test]
    public void Create_WithValidRoleNameAndTarget_ShouldSucceedAndReturnRoleAssignment()
    {
        // Arrange
        var roleName = "RestaurantOwner";
        var targetEntityId = Guid.NewGuid().ToString();
        var targetEntityType = "Restaurant";

        // Act
        var result = RoleAssignment.Create(roleName, targetEntityId, targetEntityType);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        var roleAssignment = result.Value;

        roleAssignment.RoleName.Should().Be(roleName);
        roleAssignment.TargetEntityId.Should().Be(targetEntityId);
        roleAssignment.TargetEntityType.Should().Be(targetEntityType);
    }

    [Test]
    public void Create_WithNullOrEmptyRoleName_ShouldFailAndReturnInvalidRoleNameError()
    {
        // Arrange
        string roleName = null!; // Test null
        string emptyRoleName = ""; // Test empty
        string whitespaceRoleName = "   "; // Test whitespace

        // Act & Assert for null
        var resultNull = RoleAssignment.Create(roleName);
        resultNull.IsFailure.Should().BeTrue();
        resultNull.Error.Code.Should().Be("RoleAssignment.InvalidRoleName");

        // Act & Assert for empty
        var resultEmpty = RoleAssignment.Create(emptyRoleName);
        resultEmpty.IsFailure.Should().BeTrue();
        resultEmpty.Error.Code.Should().Be("RoleAssignment.InvalidRoleName");

        // Act & Assert for whitespace
        var resultWhitespace = RoleAssignment.Create(whitespaceRoleName);
        resultWhitespace.IsFailure.Should().BeTrue();
        resultWhitespace.Error.Code.Should().Be("RoleAssignment.InvalidRoleName");
    }

    [Test]
    public void Create_WithTargetEntityIdButNoType_ShouldFailAndReturnInvalidTargetError()
    {
        // Arrange
        var roleName = "RestaurantStaff";
        var targetEntityId = Guid.NewGuid().ToString();
        string? targetEntityType = null;

        // Act
        var result = RoleAssignment.Create(roleName, targetEntityId, targetEntityType);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("RoleAssignment.InvalidTarget");
    }

    [Test]
    public void Create_WithTargetEntityTypeButNoId_ShouldFailAndReturnInvalidTargetError()
    {
        // Arrange
        var roleName = "RestaurantStaff";
        string? targetEntityId = null;
        var targetEntityType = "Restaurant";

        // Act
        var result = RoleAssignment.Create(roleName, targetEntityId, targetEntityType);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("RoleAssignment.InvalidTarget");
    }

    [Test]
    public void Equality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var roleName = "Customer";
        var assignment1 = RoleAssignment.Create(roleName).Value;
        var assignment2 = RoleAssignment.Create(roleName).Value;

        var roleNameTarget = "RestaurantStaff";
        var targetId = Guid.NewGuid().ToString();
        var targetType = "Restaurant";
        var assignmentWithTarget1 = RoleAssignment.Create(roleNameTarget, targetId, targetType).Value;
        var assignmentWithTarget2 = RoleAssignment.Create(roleNameTarget, targetId, targetType).Value;


        // Assert
        assignment1.Should().Be(assignment2);
        (assignment1 == assignment2).Should().BeTrue();
        assignment1.GetHashCode().Should().Be(assignment2.GetHashCode());

        assignmentWithTarget1.Should().Be(assignmentWithTarget2);
        (assignmentWithTarget1 == assignmentWithTarget2).Should().BeTrue();
        assignmentWithTarget1.GetHashCode().Should().Be(assignmentWithTarget2.GetHashCode());
    }

    [Test]
    public void Equality_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var assignment1 = RoleAssignment.Create("Customer").Value;
        var assignment2 = RoleAssignment.Create("Admin").Value; // Different role name

        var assignmentWithTarget1 = RoleAssignment.Create("RestaurantOwner", Guid.NewGuid().ToString(), "Restaurant").Value;
        var assignmentWithTarget2 = RoleAssignment.Create("RestaurantOwner", Guid.NewGuid().ToString(), "Restaurant").Value; // Different target ID

        var assignmentWithTarget3 = RoleAssignment.Create("RestaurantOwner", Guid.NewGuid().ToString(), "Restaurant").Value;
        var assignmentWithTarget4 = RoleAssignment.Create("RestaurantOwner", assignmentWithTarget3.TargetEntityId, "AnotherType").Value; // Different target type


        // Assert
        assignment1.Should().NotBe(assignment2);
        (assignment1 != assignment2).Should().BeTrue();

        assignmentWithTarget1.Should().NotBe(assignmentWithTarget2);
        (assignmentWithTarget1 != assignmentWithTarget2).Should().BeTrue();

        assignmentWithTarget3.Should().NotBe(assignmentWithTarget4);
        (assignmentWithTarget3 != assignmentWithTarget4).Should().BeTrue();
    }
}
