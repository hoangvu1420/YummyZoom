using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Domain.Common.Models;
using YummyZoom.Domain.SupportTicketAggregate.ValueObjects;
using YummyZoom.Domain.UnitTests;

namespace YummyZoom.Domain.UnitTests.SupportTicketAggregate.ValueObjects;

/// <summary>
/// Tests for TicketNumber value object validation and format generation.
/// </summary>
[TestFixture]
public class TicketNumberTests
{
    #region Test Data Helpers

    private static string CreateStringOfLength(int length, char character = 'A')
    {
        return length <= 0 ? string.Empty : new string(character, length);
    }

    #endregion

    #region Factory Methods - Create

    [TestCase("TKT-2024-000001")]
    [TestCase("TICKET-123")]
    [TestCase("T-1")]
    [TestCase("SHORT")]
    [TestCase("Valid Ticket Number")]
    public void Create_WithValidTicketNumber_ShouldReturnSuccessResult(string ticketNumberValue)
    {
        // Act
        var result = TicketNumber.Create(ticketNumberValue);

        // Assert
        result.ShouldBeSuccessful();
        result.Value.Value.Should().Be(ticketNumberValue);
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("\t\n")]
    public void Create_WithInvalidTicketNumber_ShouldReturnFailureResult(string ticketNumberValue)
    {
        // Act
        var result = TicketNumber.Create(ticketNumberValue);

        // Assert
        result.ShouldBeFailure();
        result.Error.Code.Should().Be("SupportTicket.InvalidTicketNumber");
        result.Error.Description.Should().Contain("Ticket number cannot be empty");
    }

    [Test]
    public void Create_WithNullTicketNumber_ShouldReturnFailureResult()
    {
        // Act
        var result = TicketNumber.Create(null!);

        // Assert
        result.ShouldBeFailure();
        result.Error.Code.Should().Be("SupportTicket.InvalidTicketNumber");
        result.Error.Description.Should().Contain("Ticket number cannot be empty");
    }

    [Test]
    public void Create_WithTicketNumberExceeding50Characters_ShouldReturnFailureResult()
    {
        // Arrange
        var longTicketNumber = CreateStringOfLength(51);

        // Act
        var result = TicketNumber.Create(longTicketNumber);

        // Assert
        result.ShouldBeFailure();
        result.Error.Code.Should().Be("SupportTicket.InvalidTicketNumber");
        result.Error.Description.Should().Contain("Ticket number cannot exceed 50 characters");
    }

    [Test]
    public void Create_WithExactly50Characters_ShouldReturnSuccessResult()
    {
        // Arrange
        var ticketNumber = CreateStringOfLength(50);

        // Act
        var result = TicketNumber.Create(ticketNumber);

        // Assert
        result.ShouldBeSuccessful();
        result.Value.Value.Should().HaveLength(50);
    }

    [TestCase(1)]
    [TestCase(10)]
    [TestCase(25)]
    [TestCase(49)]
    public void Create_WithVariousValidLengths_ShouldReturnSuccessResult(int length)
    {
        // Arrange
        var ticketNumber = CreateStringOfLength(length);

        // Act
        var result = TicketNumber.Create(ticketNumber);

        // Assert
        result.ShouldBeSuccessful();
        result.Value.Value.Should().HaveLength(length);
    }

    [Test]
    public void Create_WithSpecialCharacters_ShouldReturnSuccessResult()
    {
        // Arrange
        var ticketNumber = "TKT-2024-001@#$%";

        // Act
        var result = TicketNumber.Create(ticketNumber);

        // Assert
        result.ShouldBeSuccessful();
        result.Value.Value.Should().Be(ticketNumber);
    }

    [Test]
    public void Create_WithUnicodeCharacters_ShouldReturnSuccessResult()
    {
        // Arrange
        var ticketNumber = "TKT-2024-001-émojis🎫";

        // Act
        var result = TicketNumber.Create(ticketNumber);

        // Assert
        result.ShouldBeSuccessful();
        result.Value.Value.Should().Be(ticketNumber);
    }

    #endregion

    #region Factory Methods - CreateFromSequence

    [TestCase(1, "TKT-{0}-000001")]
    [TestCase(42, "TKT-{0}-000042")]
    [TestCase(123, "TKT-{0}-000123")]
    [TestCase(1000, "TKT-{0}-001000")]
    [TestCase(999999, "TKT-{0}-999999")]
    public void CreateFromSequence_WithValidSequenceNumber_ShouldReturnCorrectFormat(int sequenceNumber, string expectedFormat)
    {
        // Arrange
        var currentYear = DateTime.UtcNow.Year;
        var expectedTicketNumber = string.Format(expectedFormat, currentYear);

        // Act
        var result = TicketNumber.CreateFromSequence(sequenceNumber);

        // Assert
        result.Value.Should().Be(expectedTicketNumber);
    }

    [Test]
    public void CreateFromSequence_WithZeroSequenceNumber_ShouldReturnCorrectFormat()
    {
        // Arrange
        var currentYear = DateTime.UtcNow.Year;
        var expectedTicketNumber = $"TKT-{currentYear}-000000";

        // Act
        var result = TicketNumber.CreateFromSequence(0);

        // Assert
        result.Value.Should().Be(expectedTicketNumber);
    }

    [Test]
    public void CreateFromSequence_WithNegativeSequenceNumber_ShouldReturnFormattedResult()
    {
        // Arrange
        var currentYear = DateTime.UtcNow.Year;
        var sequenceNumber = -1;

        // Act
        var result = TicketNumber.CreateFromSequence(sequenceNumber);

        // Assert
        result.Value.Should().StartWith($"TKT-{currentYear}-");
        // Note: Negative numbers will be formatted with leading zeros as "-000001"
    }

    [Test]
    public void CreateFromSequence_WithLargeSequenceNumber_ShouldReturnFormattedResult()
    {
        // Arrange
        var currentYear = DateTime.UtcNow.Year;
        var sequenceNumber = 1234567; // More than 6 digits

        // Act
        var result = TicketNumber.CreateFromSequence(sequenceNumber);

        // Assert
        result.Value.Should().Be($"TKT-{currentYear}-1234567");
    }

    [Test]
    public void CreateFromSequence_ShouldUseCurrentYear()
    {
        // Arrange
        var currentYear = DateTime.UtcNow.Year;
        var sequenceNumber = 1;

        // Act
        var result = TicketNumber.CreateFromSequence(sequenceNumber);

        // Assert
        result.Value.Should().Contain(currentYear.ToString());
        result.Value.Should().StartWith($"TKT-{currentYear}-");
    }

    [Test]
    public void CreateFromSequence_WithMaxIntValue_ShouldReturnFormattedResult()
    {
        // Arrange
        var currentYear = DateTime.UtcNow.Year;
        var sequenceNumber = int.MaxValue;

        // Act
        var result = TicketNumber.CreateFromSequence(sequenceNumber);

        // Assert
        result.Value.Should().Be($"TKT-{currentYear}-{int.MaxValue}");
    }

    #endregion

    #region Value Object Equality

    [Test]
    public void TicketNumber_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var ticketNumberValue = "TKT-2024-000001";

        // Act
        var ticketNumber1 = TicketNumber.Create(ticketNumberValue).Value;
        var ticketNumber2 = TicketNumber.Create(ticketNumberValue).Value;

        // Assert
        ticketNumber1.Should().Be(ticketNumber2);
        ticketNumber1.Equals(ticketNumber2).Should().BeTrue();
        (ticketNumber1 == ticketNumber2).Should().BeTrue();
        (ticketNumber1 != ticketNumber2).Should().BeFalse();
    }

    [Test]
    public void TicketNumber_WithDifferentValues_ShouldNotBeEqual()
    {
        // Act
        var ticketNumber1 = TicketNumber.Create("TKT-2024-000001").Value;
        var ticketNumber2 = TicketNumber.Create("TKT-2024-000002").Value;

        // Assert
        ticketNumber1.Should().NotBe(ticketNumber2);
        ticketNumber1.Equals(ticketNumber2).Should().BeFalse();
        (ticketNumber1 == ticketNumber2).Should().BeFalse();
        (ticketNumber1 != ticketNumber2).Should().BeTrue();
    }

    [Test]
    public void TicketNumber_GetHashCode_ShouldBeConsistent()
    {
        // Arrange
        var ticketNumberValue = "TKT-2024-000001";

        // Act
        var ticketNumber1 = TicketNumber.Create(ticketNumberValue).Value;
        var ticketNumber2 = TicketNumber.Create(ticketNumberValue).Value;

        // Assert
        ticketNumber1.GetHashCode().Should().Be(ticketNumber2.GetHashCode());
    }

    [Test]
    public void TicketNumber_GetHashCode_WithDifferentValues_ShouldBeDifferent()
    {
        // Act
        var ticketNumber1 = TicketNumber.Create("TKT-2024-000001").Value;
        var ticketNumber2 = TicketNumber.Create("TKT-2024-000002").Value;

        // Assert
        ticketNumber1.GetHashCode().Should().NotBe(ticketNumber2.GetHashCode());
    }

    #endregion

    #region Format Validation

    [Test]
    public void CreateFromSequence_ShouldAlwaysReturn15CharacterLength()
    {
        // Arrange
        var testCases = new[] { 1, 10, 100, 1000, 10000, 100000 };

        foreach (var sequenceNumber in testCases)
        {
            // Act
            var result = TicketNumber.CreateFromSequence(sequenceNumber);

            // Assert
            result.Value.Should().HaveLength(15, $"Sequence number {sequenceNumber} should produce 15-character ticket number");
            result.Value.Should().MatchRegex(@"^TKT-\d{4}-\d{6}$", $"Sequence number {sequenceNumber} should match expected format");
        }
    }

    [Test]
    public void CreateFromSequence_ShouldPadSequenceNumberWithZeros()
    {
        // Act
        var result = TicketNumber.CreateFromSequence(42);

        // Assert
        result.Value.Should().EndWith("000042");
    }

    [Test]
    public void CreateFromSequence_FormatConsistency_ShouldBeConsistent()
    {
        // Arrange
        var sequenceNumbers = Enumerable.Range(1, 100).ToArray();

        foreach (var sequenceNumber in sequenceNumbers)
        {
            // Act
            var result = TicketNumber.CreateFromSequence(sequenceNumber);

            // Assert
            result.Value.Should().StartWith("TKT-");
            result.Value.Should().Contain($"-{DateTime.UtcNow.Year}-");
        }
    }

    #endregion

    #region Type Safety and Immutability

    [Test]
    public void TicketNumber_ShouldBeValueObject()
    {
        // Arrange & Act
        var ticketNumber = TicketNumber.Create("TKT-2024-000001").Value;

        // Assert
        ticketNumber.Should().BeAssignableTo<ValueObject>();
    }

    [Test]
    public void TicketNumber_Value_ShouldBeReadOnly()
    {
        // Arrange & Act
        var property = typeof(TicketNumber).GetProperty(nameof(TicketNumber.Value));

        // Assert
        property.Should().NotBeNull();
        // Properties should have either no setter or private setter (externally read-only)
        var setMethod = property!.GetSetMethod();
        setMethod.Should().BeNull("Value property should not have a public setter");
    }

    [Test]
    public void TicketNumber_ShouldPreventDirectInstantiation()
    {
        // Arrange & Act
        var constructors = typeof(TicketNumber).GetConstructors();
        var publicConstructors = constructors.Where(c => c.IsPublic).ToArray();

        // Assert
        publicConstructors.Should().BeEmpty("TicketNumber should not have public constructors");
    }

    [Test]
    public void TicketNumber_ShouldNotHaveUpdateMethods()
    {
        // Arrange & Act
        var updateMethods = typeof(TicketNumber)
            .GetMethods()
            .Where(m => m.Name.StartsWith("Update") || m.Name.StartsWith("Change") || m.Name.StartsWith("Modify"))
            .ToList();

        // Assert
        updateMethods.Should().BeEmpty("TicketNumber should be immutable with no update methods");
    }

    #endregion

    #region Edge Cases

    [Test]
    public void Create_WithSingleCharacter_ShouldReturnSuccessResult()
    {
        // Act
        var result = TicketNumber.Create("T");

        // Assert
        result.ShouldBeSuccessful();
        result.Value.Value.Should().Be("T");
    }

    [Test]
    public void Create_WithNumericString_ShouldReturnSuccessResult()
    {
        // Act
        var result = TicketNumber.Create("123456");

        // Assert
        result.ShouldBeSuccessful();
        result.Value.Value.Should().Be("123456");
    }

    [Test]
    public void CreateFromSequence_WithCurrentYear_ShouldHandleYearChanges()
    {
        // This test documents the behavior when year changes
        // In a real system, you might want to mock DateTime.UtcNow for testing

        // Arrange
        var currentYear = DateTime.UtcNow.Year;

        // Act
        var result = TicketNumber.CreateFromSequence(1);

        // Assert
        result.Value.Should().Contain(currentYear.ToString());
    }

    #endregion
}
