using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.TagAggregate.ValueObjects;
using YummyZoom.Domain.TagAggregate.Errors;

namespace YummyZoom.Domain.UnitTests.TagAggregate;

[TestFixture]
public class TagIdTests
{
    #region CreateUnique() Method Tests

    [Test]
    public void CreateUnique_ShouldGenerateUniqueGuids()
    {
        // Arrange & Act
        var tagId1 = TagId.CreateUnique();
        var tagId2 = TagId.CreateUnique();
        var tagId3 = TagId.CreateUnique();

        // Assert
        tagId1.Value.Should().NotBe(Guid.Empty);
        tagId2.Value.Should().NotBe(Guid.Empty);
        tagId3.Value.Should().NotBe(Guid.Empty);
        tagId1.Value.Should().NotBe(tagId2.Value);
        tagId1.Value.Should().NotBe(tagId3.Value);
        tagId2.Value.Should().NotBe(tagId3.Value);
    }

    #endregion

    #region Create(Guid) Method Tests

    [Test]
    public void Create_WithValidGuid_ShouldReturnTagIdWithCorrectValue()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var tagId = TagId.Create(guid);

        // Assert
        tagId.Value.Should().Be(guid);
    }

    [Test]
    public void Create_WithEmptyGuid_ShouldReturnTagIdWithEmptyGuid()
    {
        // Arrange
        var guid = Guid.Empty;

        // Act
        var tagId = TagId.Create(guid);

        // Assert
        tagId.Value.Should().Be(Guid.Empty);
    }

    #endregion

    #region Create(string) Method Tests

    [Test]
    public void Create_WithValidGuidString_ShouldSucceedAndReturnCorrectTagId()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var guidString = guid.ToString();

        // Act
        var result = TagId.Create(guidString);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(guid);
    }

    [TestCase("not-a-guid")]
    [TestCase("12345")]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("invalid-guid-format")]
    public void Create_WithInvalidGuidString_ShouldFailWithInvalidTagIdError(string invalidGuidString)
    {
        // Act
        var result = TagId.Create(invalidGuidString);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TagErrors.InvalidTagId);
    }

    [Test]
    public void Create_WithNullGuidString_ShouldFailWithInvalidTagIdError()
    {
        // Act
#pragma warning disable CS8625
        var result = TagId.Create(null);
#pragma warning restore CS8625

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TagErrors.InvalidTagId);
    }

    #endregion

    #region Equality Tests

    [Test]
    public void Equality_WithSameGuidValues_ShouldBeEqual()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var tagId1 = TagId.Create(guid);
        var tagId2 = TagId.Create(guid);

        // Assert
        tagId1.Should().Be(tagId2);
        (tagId1 == tagId2).Should().BeTrue();
        (tagId1 != tagId2).Should().BeFalse();
        tagId1.Equals(tagId2).Should().BeTrue();
        tagId1.GetHashCode().Should().Be(tagId2.GetHashCode());
    }

    [Test]
    public void Equality_WithDifferentGuidValues_ShouldNotBeEqual()
    {
        // Arrange
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();
        var tagId1 = TagId.Create(guid1);
        var tagId2 = TagId.Create(guid2);

        // Assert
        tagId1.Should().NotBe(tagId2);
        (tagId1 == tagId2).Should().BeFalse();
        (tagId1 != tagId2).Should().BeTrue();
        tagId1.Equals(tagId2).Should().BeFalse();
        tagId1.GetHashCode().Should().NotBe(tagId2.GetHashCode());
    }

    [Test]
    public void Equality_WithNull_ShouldNotBeEqual()
    {
        // Arrange
        var tagId = TagId.CreateUnique();

        // Assert
        tagId.Equals(null).Should().BeFalse();
#pragma warning disable CS8625, CS8604
        (tagId == null).Should().BeFalse();
        (tagId != null).Should().BeTrue();
#pragma warning restore CS8625, CS8604
    }

    #endregion
} 
