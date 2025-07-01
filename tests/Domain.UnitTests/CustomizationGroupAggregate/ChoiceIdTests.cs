using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.CustomizationGroupAggregate.Errors;
using YummyZoom.Domain.CustomizationGroupAggregate.ValueObjects;

namespace YummyZoom.Domain.UnitTests.CustomizationGroupAggregate;

[TestFixture]
public class ChoiceIdTests
{
    #region CreateUnique() Method Tests

    [Test]
    public void CreateUnique_ShouldGenerateUniqueGuids()
    {
        // Arrange & Act
        var choiceId1 = ChoiceId.CreateUnique();
        var choiceId2 = ChoiceId.CreateUnique();
        var choiceId3 = ChoiceId.CreateUnique();

        // Assert
        choiceId1.Value.Should().NotBe(Guid.Empty);
        choiceId2.Value.Should().NotBe(Guid.Empty);
        choiceId3.Value.Should().NotBe(Guid.Empty);
        choiceId1.Value.Should().NotBe(choiceId2.Value);
        choiceId1.Value.Should().NotBe(choiceId3.Value);
        choiceId2.Value.Should().NotBe(choiceId3.Value);
    }

    #endregion

    #region Create(Guid) Method Tests

    [Test]
    public void Create_WithValidGuid_ShouldReturnChoiceIdWithCorrectValue()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var choiceId = ChoiceId.Create(guid);

        // Assert
        choiceId.Value.Should().Be(guid);
    }

    [Test]
    public void Create_WithEmptyGuid_ShouldReturnChoiceIdWithEmptyGuid()
    {
        // Arrange
        var emptyGuid = Guid.Empty;

        // Act
        var choiceId = ChoiceId.Create(emptyGuid);

        // Assert
        choiceId.Value.Should().Be(emptyGuid);
    }

    #endregion

    #region Create(string) Method Tests

    [Test]
    public void Create_WithValidGuidString_ShouldReturnSuccessWithCorrectValue()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var guidString = guid.ToString();

        // Act
        var result = ChoiceId.Create(guidString);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(guid);
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("invalid-guid")]
    [TestCase("12345")]
    [TestCase("not-a-guid-at-all")]
    public void Create_WithInvalidGuidString_ShouldReturnFailureWithInvalidChoiceIdError(string invalidGuidString)
    {
        // Act
        var result = ChoiceId.Create(invalidGuidString);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CustomizationGroupErrors.InvalidChoiceId);
    }

    [Test]
    public void Create_WithNullString_ShouldReturnFailureWithInvalidChoiceIdError()
    {
        // Act
#pragma warning disable CS8625
        var result = ChoiceId.Create((string)null!);
#pragma warning restore CS8625

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CustomizationGroupErrors.InvalidChoiceId);
    }

    [Test]
    public void Create_WithGuidStringInDifferentFormats_ShouldSucceed()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var formats = new[]
        {
            guid.ToString("D"), // Default format with hyphens
            guid.ToString("N"), // No hyphens
            guid.ToString("B"), // Braces
            guid.ToString("P")  // Parentheses
        };

        // Act & Assert
        foreach (var format in formats)
        {
            var result = ChoiceId.Create(format);
            result.IsSuccess.Should().BeTrue($"Format {format} should be valid");
            result.Value.Value.Should().Be(guid);
        }
    }

    #endregion

    #region Equality Tests

    [Test]
    public void Equals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var choiceId1 = ChoiceId.Create(guid);
        var choiceId2 = ChoiceId.Create(guid);

        // Act & Assert
        choiceId1.Should().Be(choiceId2);
        choiceId1.Equals(choiceId2).Should().BeTrue();
        (choiceId1 == choiceId2).Should().BeTrue();
        (choiceId1 != choiceId2).Should().BeFalse();
    }

    [Test]
    public void Equals_WithDifferentValues_ShouldReturnFalse()
    {
        // Arrange
        var choiceId1 = ChoiceId.CreateUnique();
        var choiceId2 = ChoiceId.CreateUnique();

        // Act & Assert
        choiceId1.Should().NotBe(choiceId2);
        choiceId1.Equals(choiceId2).Should().BeFalse();
        (choiceId1 == choiceId2).Should().BeFalse();
        (choiceId1 != choiceId2).Should().BeTrue();
    }

    [Test]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var choiceId = ChoiceId.CreateUnique();

        // Act & Assert
        choiceId.Equals(null).Should().BeFalse();
    }

    [Test]
    public void GetHashCode_WithSameValues_ShouldReturnSameHashCode()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var choiceId1 = ChoiceId.Create(guid);
        var choiceId2 = ChoiceId.Create(guid);

        // Act
        var hashCode1 = choiceId1.GetHashCode();
        var hashCode2 = choiceId2.GetHashCode();

        // Assert
        hashCode1.Should().Be(hashCode2);
    }

    [Test]
    public void GetHashCode_WithDifferentValues_ShouldReturnDifferentHashCodes()
    {
        // Arrange
        var choiceId1 = ChoiceId.CreateUnique();
        var choiceId2 = ChoiceId.CreateUnique();

        // Act
        var hashCode1 = choiceId1.GetHashCode();
        var hashCode2 = choiceId2.GetHashCode();

        // Assert
        hashCode1.Should().NotBe(hashCode2);
    }

    #endregion
}
