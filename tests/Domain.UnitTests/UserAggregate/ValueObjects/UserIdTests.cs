using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.Errors;

namespace YummyZoom.Domain.UnitTests.UserAggregate.ValueObjects;

[TestFixture]
public class UserIdTests
{
    [Test]
    public void CreateUnique_ShouldReturnUserIdWithNonEmptyGuidValue()
    {
        // Act
        var userId = UserId.CreateUnique();

        // Assert
        userId.Should().NotBeNull();
        userId.Value.Should().NotBe(Guid.Empty);
    }

    [Test]
    public void Create_WithValidGuid_ShouldReturnUserIdWithCorrectValue()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var userId = UserId.Create(guid);

        // Assert
        userId.Should().NotBeNull();
        userId.Value.Should().Be(guid);
    }

    [Test]
    public void Create_WithValidGuidString_ShouldReturnSuccessResultWithUserId()
    {
        // Arrange
        var guidString = Guid.NewGuid().ToString();

        // Act
        var result = UserId.Create(guidString);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Value.ToString().Should().Be(guidString);
    }

    [Test]
    public void Create_WithInvalidGuidString_ShouldReturnFailureResultWithInvalidUserIdError()
    {
        // Arrange
        var invalidGuidString = "invalid-guid-string";

        // Act
        var result = UserId.Create(invalidGuidString);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.InvalidUserId(invalidGuidString));
    }

    [Test]
    public void Equality_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var userId1 = UserId.Create(guid);
        var userId2 = UserId.Create(guid);

        // Assert
        userId1.Should().Be(userId2);
        (userId1 == userId2).Should().BeTrue();
        userId1.GetHashCode().Should().Be(userId2.GetHashCode());
    }

    [Test]
    public void Equality_WithDifferentValue_ShouldNotBeEqual()
    {
        // Arrange
        var userId1 = UserId.CreateUnique();
        var userId2 = UserId.CreateUnique();

        // Assert
        userId1.Should().NotBe(userId2);
        (userId1 != userId2).Should().BeTrue();
    }
}
