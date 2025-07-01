using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.CustomizationGroupAggregate;

[TestFixture]
public class CustomizationGroupIdTests
{
    #region CreateUnique() Method Tests

    [Test]
    public void CreateUnique_ShouldGenerateUniqueGuids()
    {
        // Arrange & Act
        var groupId1 = CustomizationGroupId.CreateUnique();
        var groupId2 = CustomizationGroupId.CreateUnique();
        var groupId3 = CustomizationGroupId.CreateUnique();

        // Assert
        groupId1.Value.Should().NotBe(Guid.Empty);
        groupId2.Value.Should().NotBe(Guid.Empty);
        groupId3.Value.Should().NotBe(Guid.Empty);
        groupId1.Value.Should().NotBe(groupId2.Value);
        groupId1.Value.Should().NotBe(groupId3.Value);
        groupId2.Value.Should().NotBe(groupId3.Value);
    }

    #endregion

    #region Create(Guid) Method Tests

    [Test]
    public void Create_WithValidGuid_ShouldReturnCustomizationGroupIdWithCorrectValue()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var groupId = CustomizationGroupId.Create(guid);

        // Assert
        groupId.Value.Should().Be(guid);
    }

    [Test]
    public void Create_WithEmptyGuid_ShouldReturnCustomizationGroupIdWithEmptyGuid()
    {
        // Arrange
        var emptyGuid = Guid.Empty;

        // Act
        var groupId = CustomizationGroupId.Create(emptyGuid);

        // Assert
        groupId.Value.Should().Be(emptyGuid);
    }

    #endregion

    #region Equality Tests

    [Test]
    public void Equals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var groupId1 = CustomizationGroupId.Create(guid);
        var groupId2 = CustomizationGroupId.Create(guid);

        // Act & Assert
        groupId1.Should().Be(groupId2);
        groupId1.Equals(groupId2).Should().BeTrue();
        (groupId1 == groupId2).Should().BeTrue();
        (groupId1 != groupId2).Should().BeFalse();
    }

    [Test]
    public void Equals_WithDifferentValues_ShouldReturnFalse()
    {
        // Arrange
        var groupId1 = CustomizationGroupId.CreateUnique();
        var groupId2 = CustomizationGroupId.CreateUnique();

        // Act & Assert
        groupId1.Should().NotBe(groupId2);
        groupId1.Equals(groupId2).Should().BeFalse();
        (groupId1 == groupId2).Should().BeFalse();
        (groupId1 != groupId2).Should().BeTrue();
    }

    [Test]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var groupId = CustomizationGroupId.CreateUnique();

        // Act & Assert
        groupId.Equals(null).Should().BeFalse();
    }

    [Test]
    public void GetHashCode_WithSameValues_ShouldReturnSameHashCode()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var groupId1 = CustomizationGroupId.Create(guid);
        var groupId2 = CustomizationGroupId.Create(guid);

        // Act
        var hashCode1 = groupId1.GetHashCode();
        var hashCode2 = groupId2.GetHashCode();

        // Assert
        hashCode1.Should().Be(hashCode2);
    }

    [Test]
    public void GetHashCode_WithDifferentValues_ShouldReturnDifferentHashCodes()
    {
        // Arrange
        var groupId1 = CustomizationGroupId.CreateUnique();
        var groupId2 = CustomizationGroupId.CreateUnique();

        // Act
        var hashCode1 = groupId1.GetHashCode();
        var hashCode2 = groupId2.GetHashCode();

        // Assert
        hashCode1.Should().NotBe(hashCode2);
    }

    #endregion
}
