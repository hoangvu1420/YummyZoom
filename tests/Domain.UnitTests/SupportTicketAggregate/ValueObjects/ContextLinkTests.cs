using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.Models;
using YummyZoom.Domain.SupportTicketAggregate.Enums;
using YummyZoom.Domain.SupportTicketAggregate.ValueObjects;
using YummyZoom.Domain.UnitTests;

namespace YummyZoom.Domain.UnitTests.SupportTicketAggregate.ValueObjects;

/// <summary>
/// Tests for ContextLink value object validation and behavior.
/// </summary>
[TestFixture]
public class ContextLinkTests
{
    #region Test Data Helpers

    private static Guid CreateValidEntityId() => Guid.NewGuid();

    #endregion

    #region Factory Methods

    [TestCase(ContextEntityType.Order)]
    [TestCase(ContextEntityType.User)]
    [TestCase(ContextEntityType.Restaurant)]
    [TestCase(ContextEntityType.Review)]
    public void Create_WithValidInputs_ShouldReturnSuccessResult(ContextEntityType entityType)
    {
        // Arrange
        var entityId = CreateValidEntityId();

        // Act
        var result = ContextLink.Create(entityType, entityId);

        // Assert
        result.ShouldBeSuccessful();
        result.Value.EntityType.Should().Be(entityType);
        result.Value.EntityID.Should().Be(entityId);
    }

    [Test]
    public void Create_WithEmptyEntityId_ShouldReturnFailureResult()
    {
        // Arrange
        var entityType = ContextEntityType.Order;
        var entityId = Guid.Empty;

        // Act
        var result = ContextLink.Create(entityType, entityId);

        // Assert
        result.ShouldBeFailure();
        result.Error.Code.Should().Be("SupportTicket.InvalidContextEntityId");
        result.Error.Description.Should().Contain("Entity ID cannot be empty");
    }

    [Test]
    public void Create_WithStringGuid_ShouldReturnSuccessResult()
    {
        // Arrange
        var entityType = ContextEntityType.Review;
        var entityId = Guid.NewGuid();
        var entityIdString = entityId.ToString();

        // Act
        var result = ContextLink.Create(entityType, entityIdString);

        // Assert
        result.ShouldBeSuccessful();
        result.Value.EntityType.Should().Be(entityType);
        result.Value.EntityID.Should().Be(entityId);
    }

    [TestCase("invalid-guid")]
    [TestCase("")]
    [TestCase("123-456-789")]
    [TestCase("not-a-guid-at-all")]
    [TestCase("12345678-1234-1234-1234-12345678901")]  // Too short
    [TestCase("12345678-1234-1234-1234-1234567890123")] // Too long
    public void Create_WithInvalidStringGuid_ShouldReturnFailureResult(string invalidGuidString)
    {
        // Arrange
        var entityType = ContextEntityType.User;

        // Act
        var result = ContextLink.Create(entityType, invalidGuidString);

        // Assert
        result.ShouldBeFailure();
        result.Error.Code.Should().Be("SupportTicket.InvalidContextEntityId");
        result.Error.Description.Should().Contain("is not a valid GUID");
    }

    [Test]
    public void Create_WithNullStringGuid_ShouldReturnFailureResult()
    {
        // Arrange
        var entityType = ContextEntityType.Restaurant;

        // Act
        var result = ContextLink.Create(entityType, (string)null!);

        // Assert
        result.ShouldBeFailure();
        result.Error.Code.Should().Be("SupportTicket.InvalidContextEntityId");
    }

    [Test]
    public void Create_WithWhitespaceStringGuid_ShouldReturnFailureResult()
    {
        // Arrange
        var entityType = ContextEntityType.Order;

        // Act
        var result = ContextLink.Create(entityType, "   ");

        // Assert
        result.ShouldBeFailure();
        result.Error.Code.Should().Be("SupportTicket.InvalidContextEntityId");
    }

    [Test]
    public void Create_WithValidStringGuidWithDifferentFormats_ShouldReturnSuccessResult()
    {
        // Arrange
        var entityType = ContextEntityType.Review;
        var entityId = Guid.NewGuid();
        var formats = new[]
        {
            entityId.ToString(),           // Default format
            entityId.ToString("D"),        // Digits with hyphens
            entityId.ToString("B"),        // Braces
            entityId.ToString("P"),        // Parentheses
            entityId.ToString("N")         // No hyphens
        };

        foreach (var format in formats)
        {
            // Act
            var result = ContextLink.Create(entityType, format);

            // Assert
            result.ShouldBeSuccessful();
            result.Value.EntityID.Should().Be(entityId);
        }
    }

    #endregion

    #region Value Object Equality

    [Test]
    public void ContextLink_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var entityType = ContextEntityType.Order;
        var entityId = CreateValidEntityId();

        // Act
        var contextLink1 = ContextLink.Create(entityType, entityId).Value;
        var contextLink2 = ContextLink.Create(entityType, entityId).Value;

        // Assert
        contextLink1.Should().Be(contextLink2);
        contextLink1.Equals(contextLink2).Should().BeTrue();
        (contextLink1 == contextLink2).Should().BeTrue();
        (contextLink1 != contextLink2).Should().BeFalse();
    }

    [Test]
    public void ContextLink_WithDifferentEntityTypes_ShouldNotBeEqual()
    {
        // Arrange
        var entityId = CreateValidEntityId();

        // Act
        var contextLink1 = ContextLink.Create(ContextEntityType.Order, entityId).Value;
        var contextLink2 = ContextLink.Create(ContextEntityType.Review, entityId).Value;

        // Assert
        contextLink1.Should().NotBe(contextLink2);
        contextLink1.Equals(contextLink2).Should().BeFalse();
        (contextLink1 == contextLink2).Should().BeFalse();
        (contextLink1 != contextLink2).Should().BeTrue();
    }

    [Test]
    public void ContextLink_WithDifferentEntityIds_ShouldNotBeEqual()
    {
        // Arrange
        var entityType = ContextEntityType.User;

        // Act
        var contextLink1 = ContextLink.Create(entityType, CreateValidEntityId()).Value;
        var contextLink2 = ContextLink.Create(entityType, CreateValidEntityId()).Value;

        // Assert
        contextLink1.Should().NotBe(contextLink2);
        contextLink1.Equals(contextLink2).Should().BeFalse();
    }

    [Test]
    public void ContextLink_WithCompletelyDifferentValues_ShouldNotBeEqual()
    {
        // Arrange & Act
        var contextLink1 = ContextLink.Create(ContextEntityType.Order, CreateValidEntityId()).Value;
        var contextLink2 = ContextLink.Create(ContextEntityType.Restaurant, CreateValidEntityId()).Value;

        // Assert
        contextLink1.Should().NotBe(contextLink2);
    }

    [Test]
    public void ContextLink_GetHashCode_ShouldBeConsistent()
    {
        // Arrange
        var entityType = ContextEntityType.Review;
        var entityId = CreateValidEntityId();

        // Act
        var contextLink1 = ContextLink.Create(entityType, entityId).Value;
        var contextLink2 = ContextLink.Create(entityType, entityId).Value;

        // Assert
        contextLink1.GetHashCode().Should().Be(contextLink2.GetHashCode());
    }

    [Test]
    public void ContextLink_GetHashCode_WithDifferentValues_ShouldBeDifferent()
    {
        // Arrange
        var entityId = CreateValidEntityId();

        // Act
        var contextLink1 = ContextLink.Create(ContextEntityType.Order, entityId).Value;
        var contextLink2 = ContextLink.Create(ContextEntityType.Review, entityId).Value;

        // Assert
        contextLink1.GetHashCode().Should().NotBe(contextLink2.GetHashCode());
    }

    #endregion

    #region Edge Cases and Boundary Values

    [Test]
    public void Create_WithAllContextEntityTypes_ShouldReturnSuccessResult()
    {
        // Arrange
        var entityId = CreateValidEntityId();
        var allEntityTypes = Enum.GetValues<ContextEntityType>();

        foreach (var entityType in allEntityTypes)
        {
            // Act
            var result = ContextLink.Create(entityType, entityId);

            // Assert
            result.ShouldBeSuccessful();
            result.Value.EntityType.Should().Be(entityType);
            result.Value.EntityID.Should().Be(entityId);
        }
    }

    [Test]
    public void ContextLink_ShouldBeValueObject()
    {
        // Arrange & Act
        var contextLink = ContextLink.Create(ContextEntityType.Order, CreateValidEntityId()).Value;

        // Assert
        contextLink.Should().BeAssignableTo<ValueObject>();
    }

    [Test]
    public void ContextLink_Properties_ShouldBeReadOnly()
    {
        // Arrange & Act
        var properties = typeof(ContextLink).GetProperties();

        // Assert
        foreach (var property in properties)
        {
            // Properties should have either no setter or private setter (externally read-only)
            var setMethod = property.GetSetMethod();
            setMethod.Should().BeNull($"Property {property.Name} should not have a public setter");
        }
    }

    [Test]
    public void Create_WithGuidMinValue_ShouldReturnFailureResult()
    {
        // Arrange
        var entityType = ContextEntityType.Order;
        var entityId = Guid.Empty;

        // Act
        var result = ContextLink.Create(entityType, entityId);

        // Assert
        result.ShouldBeFailure();
        result.Error.Code.Should().Be("SupportTicket.InvalidContextEntityId");
    }

    [Test]
    public void Create_WithGuidMaxValue_ShouldReturnSuccessResult()
    {
        // Arrange
        var entityType = ContextEntityType.User;
        var entityId = new Guid("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF");

        // Act
        var result = ContextLink.Create(entityType, entityId);

        // Assert
        result.ShouldBeSuccessful();
        result.Value.EntityID.Should().Be(entityId);
    }

    #endregion

    #region Type Safety

    [Test]
    public void ContextLink_ShouldPreventDirectInstantiation()
    {
        // Arrange & Act
        var constructors = typeof(ContextLink).GetConstructors();
        var publicConstructors = constructors.Where(c => c.IsPublic).ToArray();

        // Assert
        publicConstructors.Should().BeEmpty("ContextLink should not have public constructors");
    }

    #endregion
}
